using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Game;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar.Scores;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarScoreGrain : Grain, ISnowWarScoreGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory;
    private readonly SnowWarConfig _config;
    private readonly IGrainFactory _grainFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<ISnowWarScoreGrain> _logger;

    private readonly SnowWarScoreLiveState _state = new();

    private IDisposable? _flushTimer;

    public SnowWarScoreGrain(
        IDbContextFactory<TurboDbContext> dbCtxFactory,
        IOptions<SnowWarConfig> config,
        IGrainFactory grainFactory,
        TimeProvider time,
        ILogger<ISnowWarScoreGrain> logger
    )
    {
        _dbCtxFactory = dbCtxFactory;
        _config = config.Value;
        _grainFactory = grainFactory;
        _time = time;
        _logger = logger;
    }

    private DateTimeOffset Now => _time.GetUtcNow();

    public override Task OnActivateAsync(CancellationToken ct)
    {
        var period = TimeSpan.FromSeconds(_config.ScoreFlushIntervalSeconds);

        _flushTimer = this.RegisterGrainTimer<object?>(
            static async (self, ct) => await ((SnowWarScoreGrain)self!).OnFlushTimerAsync(ct),
            this,
            period,
            period
        );

        return Task.CompletedTask;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _flushTimer?.Dispose();
        _flushTimer = null;

        await FlushOnDeactivationAsync();
    }

    public Task AddAsync(ImmutableArray<SnowWarScoreEntrySnapshot> scores, CancellationToken ct)
    {
        if (scores.IsDefaultOrEmpty)
            return Task.CompletedTask;

        var week = SnowWarWeeks.StartOf(Now);

        foreach (var entry in scores)
        {
            if (entry is null || entry.PlayerId.Value <= 0)
                continue;

            Queue(new SnowWarPendingScore(entry.PlayerId, week, Math.Max(0, entry.Score), 1));
        }

        return Task.CompletedTask;
    }

    public async Task<SnowWarPlayerTotalsSnapshot> GetPlayerTotalsAsync(
        PlayerId playerId,
        CancellationToken ct
    )
    {
        var stored = await StoredTotalsAsync(playerId, ct);
        var pending = _state.PendingScores.Where(p => p.PlayerId == playerId).ToList();
        var total = stored.Score + pending.Sum(p => p.Score);

        return new SnowWarPlayerTotalsSnapshot
        {
            TotalScore = total,
            GamesPlayed = stored.Matches + pending.Sum(p => p.Matches),
            SkillLevel = SnowWarSkill.LevelOf(total),
            ScoreToNextLevel = SnowWarSkill.ScoreToNextLevel(total),
        };
    }

    private async Task<SnowWarCachedTotals> StoredTotalsAsync(
        PlayerId playerId,
        CancellationToken ct
    )
    {
        if (
            _state.Totals.TryGetValue(playerId, out var cached)
            && Now - cached.LoadedAt < TimeSpan.FromSeconds(_config.PlayerTotalsCacheSeconds)
        )
            return cached;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var rows = await dbCtx
            .SnowWarScores.AsNoTracking()
            .Where(s => s.PlayerId == playerId.Value)
            .Select(s => new { s.Score, s.Matches })
            .ToListAsync(ct);
        var loaded = new SnowWarCachedTotals(rows.Sum(r => r.Score), rows.Sum(r => r.Matches), Now);

        _state.Totals[playerId] = loaded;

        return loaded;
    }

    private async Task OnFlushTimerAsync(CancellationToken ct)
    {
        ForgetExpiredRequests();
        ForgetExpiredCaches();

        await FlushAsync(ct);
    }

    private void Queue(SnowWarPendingScore score)
    {
        var index = _state.PendingScores.FindIndex(p =>
            p.PlayerId == score.PlayerId && p.WeekStart == score.WeekStart
        );

        if (index >= 0)
        {
            var current = _state.PendingScores[index];

            _state.PendingScores[index] = current with
            {
                Score = current.Score + score.Score,
                Matches = current.Matches + score.Matches,
            };

            return;
        }

        if (_state.PendingScores.Count >= _config.MaxPendingScores)
        {
            _state.PendingScores.RemoveAt(0);
            _state.DroppedScores++;
        }

        _state.PendingScores.Add(score);
    }

    private async Task<bool> FlushAsync(CancellationToken ct)
    {
        if (_state.PendingScores.Count == 0)
        {
            ReportDroppedScores();

            return true;
        }

        var taken = _state.PendingScores.ToList();

        _state.PendingScores.Clear();

        try
        {
            await WriteAsync(taken, ct);
            OnWritten(taken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to flush {Count} SnowWar scores; they stay queued",
                taken.Count
            );

            var later = _state.PendingScores.ToList();

            _state.PendingScores.Clear();

            foreach (var score in taken.Concat(later))
                Queue(score);

            return false;
        }
        finally
        {
            ReportDroppedScores();
        }
    }

    private async Task FlushOnDeactivationAsync()
    {
        var taken = _state.PendingScores.ToList();

        _state.PendingScores.Clear();

        if (taken.Count > 0)
        {
            try
            {
                await WriteAsync(taken, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to write {Count} SnowWar scores on deactivation; they are lost",
                    taken.Count
                );
            }
        }

        ReportDroppedScores();
    }

    private void ReportDroppedScores()
    {
        if (_state.DroppedScores == 0)
            return;

        _logger.LogWarning(
            "SnowWar score queue was full ({Max}); dropped {Dropped} scores",
            _config.MaxPendingScores,
            _state.DroppedScores
        );

        _state.DroppedScores = 0;
    }

    private void OnWritten(IReadOnlyList<SnowWarPendingScore> written)
    {
        _state.Boards.Clear();
        _state.OldestWeek = null;

        foreach (var score in written)
        {
            if (_state.Totals.TryGetValue(score.PlayerId, out var cached))
                _state.Totals[score.PlayerId] = cached with
                {
                    Score = cached.Score + score.Score,
                    Matches = cached.Matches + score.Matches,
                };
        }
    }

    private async Task WriteAsync(IReadOnlyList<SnowWarPendingScore> scores, CancellationToken ct)
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var playerIds = scores.Select(s => s.PlayerId.Value).Distinct().ToList();
        var weeks = scores.Select(s => s.WeekStart).Distinct().ToList();
        var existing = await dbCtx
            .SnowWarScores.Where(s => playerIds.Contains(s.PlayerId) && weeks.Contains(s.WeekStart))
            .ToListAsync(ct);

        foreach (var score in scores)
        {
            var row = existing.FirstOrDefault(r =>
                r.PlayerId == score.PlayerId.Value && r.WeekStart == score.WeekStart
            );

            if (row is null)
            {
                row = new SnowWarScoreEntity
                {
                    PlayerId = score.PlayerId.Value,
                    WeekStart = score.WeekStart,
                };

                dbCtx.SnowWarScores.Add(row);
                existing.Add(row);
            }

            row.Score += score.Score;
            row.Matches += score.Matches;
        }

        await dbCtx.SaveChangesAsync(ct);
    }

    private void ForgetExpiredCaches()
    {
        var totalsAge = TimeSpan.FromSeconds(_config.PlayerTotalsCacheSeconds);
        var boardAge = TimeSpan.FromSeconds(_config.LeaderboardCacheSeconds);

        foreach (
            var expired in _state.Totals.Where(t => Now - t.Value.LoadedAt >= totalsAge).ToList()
        )
            _state.Totals.Remove(expired.Key);

        foreach (
            var expired in _state.Boards.Where(b => Now - b.Value.LoadedAt >= boardAge).ToList()
        )
            _state.Boards.Remove(expired.Key);
    }

    private void ForgetExpiredRequests()
    {
        var interval = TimeSpan.FromMilliseconds(_config.LeaderboardRequestIntervalMilliseconds);

        foreach (var expired in _state.LastRequests.Where(r => Now - r.Value >= interval).ToList())
            _state.LastRequests.Remove(expired.Key);
    }
}
