using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Game;
using Turbo.Games.SnowWar.Scores;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarScoreGrain
{
    private static readonly SnowWarLeaderboardPageSnapshot EmptyPage = new()
    {
        Entries = [],
        TotalListSize = 0,
    };

    public async Task<SnowWarLeaderboardPageSnapshot?> GetTotalAsync(
        SnowWarLeaderboardRequestSnapshot request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryAdmit(request, SnowWarLeaderboardKind.Total))
            return null;

        if (request.Scope == SnowWarLeaderboardScope.Groups)
            return EmptyPage;

        await FlushAsync(ct);

        var members = await MembersOfAsync(request, ct);

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var scores = await Among(dbCtx, members)
            .GroupBy(s => s.PlayerId)
            .Select(g => new { PlayerId = g.Key, Score = g.Sum(s => s.Score) })
            .ToListAsync(ct);

        return await PageAsync(dbCtx, [.. scores.Select(s => (s.PlayerId, s.Score))], request, ct);
    }

    public async Task<SnowWarWeeklyLeaderboardPageSnapshot?> GetWeeklyAsync(
        SnowWarLeaderboardRequestSnapshot request,
        int weekOffset,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryAdmit(request, SnowWarLeaderboardKind.Weekly))
            return null;

        await FlushAsync(ct);

        var now = Now;
        var currentWeek = SnowWarWeeks.StartOf(now);

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var oldestWeek = await dbCtx
            .SnowWarScores.AsNoTracking()
            .Select(s => (DateOnly?)s.WeekStart)
            .MinAsync(ct);
        var maxOffset = oldestWeek is { } oldest ? SnowWarWeeks.Between(oldest, currentWeek) : 0;
        var offset = Math.Clamp(weekOffset, 0, maxOffset);
        var week = SnowWarWeeks.Back(currentWeek, offset);
        var page =
            request.Scope == SnowWarLeaderboardScope.Groups
                ? EmptyPage
                : await WeekPageAsync(dbCtx, week, request, ct);

        return new SnowWarWeeklyLeaderboardPageSnapshot
        {
            Year = SnowWarWeeks.IsoYear(week),
            Week = SnowWarWeeks.IsoWeek(week),
            MaxOffset = maxOffset,
            CurrentOffset = offset,
            MinutesUntilReset = SnowWarWeeks.MinutesUntilNext(currentWeek, now),
            Page = page,
        };
    }

    private async Task<SnowWarLeaderboardPageSnapshot> WeekPageAsync(
        TurboDbContext dbCtx,
        DateOnly week,
        SnowWarLeaderboardRequestSnapshot request,
        CancellationToken ct
    )
    {
        var members = await MembersOfAsync(request, ct);
        var scores = await Among(dbCtx, members)
            .Where(s => s.WeekStart == week)
            .Select(s => new { s.PlayerId, s.Score })
            .ToListAsync(ct);

        return await PageAsync(dbCtx, [.. scores.Select(s => (s.PlayerId, s.Score))], request, ct);
    }

    private async Task<SnowWarLeaderboardPageSnapshot> PageAsync(
        TurboDbContext dbCtx,
        IReadOnlyList<(int PlayerId, long Score)> scores,
        SnowWarLeaderboardRequestSnapshot request,
        CancellationToken ct
    )
    {
        var ranked = SnowWarLeaderboardPages.Rank(scores);
        var slice = SnowWarLeaderboardPages.Slice(ranked, request, _config.LeaderboardMaxRows);
        var ids = slice.Select(r => r.PlayerId).ToList();
        var players = await dbCtx
            .Players.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Figure,
                p.Gender,
            })
            .ToDictionaryAsync(p => p.Id, ct);

        return new SnowWarLeaderboardPageSnapshot
        {
            Entries =
            [
                .. slice.Select(r =>
                {
                    var player = players.GetValueOrDefault(r.PlayerId);

                    return new SnowWarLeaderboardEntrySnapshot
                    {
                        UserId = r.PlayerId,
                        Score = (int)Math.Min(int.MaxValue, r.Score),
                        Rank = r.Rank,
                        Name = player?.Name ?? string.Empty,
                        Figure = player?.Figure ?? string.Empty,
                        Gender = (player?.Gender ?? AvatarGenderType.Male).ToLegacyString(),
                    };
                }),
            ],
            TotalListSize = ranked.Length,
        };
    }

    private static IQueryable<SnowWarScoreEntity> Among(TurboDbContext dbCtx, List<int>? members)
    {
        var scores = dbCtx.SnowWarScores.AsNoTracking();

        return members is null ? scores : scores.Where(s => members.Contains(s.PlayerId));
    }

    private async Task<List<int>?> MembersOfAsync(
        SnowWarLeaderboardRequestSnapshot request,
        CancellationToken ct
    )
    {
        if (request.Scope != SnowWarLeaderboardScope.Friends)
            return null;

        var friends = await _grainFactory
            .GetPlayerMessengerGrain(request.PlayerId)
            .GetFriendsAsync(ct);

        return
        [
            request.PlayerId.Value,
            .. (friends ?? [])
                .Select(f => f.PlayerId.Value)
                .Where(id => id != request.PlayerId.Value),
        ];
    }

    private bool TryAdmit(SnowWarLeaderboardRequestSnapshot request, SnowWarLeaderboardKind kind)
    {
        var key = (request.PlayerId, kind, request.Scope);
        var interval = TimeSpan.FromMilliseconds(_config.LeaderboardRequestIntervalMilliseconds);

        if (_state.LastRequests.TryGetValue(key, out var last) && Now - last < interval)
            return false;

        _state.LastRequests[key] = Now;

        return true;
    }
}
