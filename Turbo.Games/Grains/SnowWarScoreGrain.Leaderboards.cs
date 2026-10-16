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

        return await PageOfAsync(SnowWarLeaderboardKind.Total, DateOnly.MinValue, request, ct);
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
        var oldestWeek = await OldestWeekAsync(ct);
        var maxOffset = oldestWeek is { } oldest ? SnowWarWeeks.Between(oldest, currentWeek) : 0;
        var offset = Math.Clamp(weekOffset, 0, maxOffset);
        var week = SnowWarWeeks.Back(currentWeek, offset);
        var page =
            request.Scope == SnowWarLeaderboardScope.Groups
                ? EmptyPage
                : await PageOfAsync(SnowWarLeaderboardKind.Weekly, week, request, ct);

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

    private async Task<DateOnly?> OldestWeekAsync(CancellationToken ct)
    {
        if (
            _state.OldestWeek is { } cached
            && Now - cached.LoadedAt < TimeSpan.FromSeconds(_config.LeaderboardCacheSeconds)
        )
            return cached.Week;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var oldest = await dbCtx
            .SnowWarScores.AsNoTracking()
            .Select(s => (DateOnly?)s.WeekStart)
            .MinAsync(ct);

        _state.OldestWeek = new SnowWarCachedOldestWeek(oldest, Now);

        return oldest;
    }

    private async Task<SnowWarLeaderboardPageSnapshot> PageOfAsync(
        SnowWarLeaderboardKind kind,
        DateOnly week,
        SnowWarLeaderboardRequestSnapshot request,
        CancellationToken ct
    )
    {
        var board =
            request.Scope == SnowWarLeaderboardScope.Friends
                ? await ScanAsync(kind, week, await MembersOfAsync(request, ct), ct)
                : await SharedBoardAsync(kind, week, ct);
        var slice = SnowWarLeaderboardPages.Slice(
            board.Ranked,
            request,
            _config.LeaderboardMaxRows
        );

        await LoadNamesAsync(board, slice, ct);

        return new SnowWarLeaderboardPageSnapshot
        {
            Entries = [.. slice.Select(r => Entry(r, board.Names.GetValueOrDefault(r.PlayerId)))],
            TotalListSize = board.Ranked.Length,
        };
    }

    private async Task<SnowWarLeaderboardBoard> SharedBoardAsync(
        SnowWarLeaderboardKind kind,
        DateOnly week,
        CancellationToken ct
    )
    {
        if (
            _state.Boards.TryGetValue((kind, week), out var cached)
            && Now - cached.LoadedAt < TimeSpan.FromSeconds(_config.LeaderboardCacheSeconds)
        )
            return cached;

        var board = await ScanAsync(kind, week, null, ct);

        _state.Boards[(kind, week)] = board;

        return board;
    }

    private async Task<SnowWarLeaderboardBoard> ScanAsync(
        SnowWarLeaderboardKind kind,
        DateOnly week,
        List<int>? members,
        CancellationToken ct
    )
    {
        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var scores =
            kind == SnowWarLeaderboardKind.Weekly
                ? await Among(dbCtx, members)
                    .Where(s => s.WeekStart == week)
                    .Select(s => new { s.PlayerId, s.Score })
                    .ToListAsync(ct)
                : await Among(dbCtx, members)
                    .GroupBy(s => s.PlayerId)
                    .Select(g => new { PlayerId = g.Key, Score = g.Sum(s => s.Score) })
                    .ToListAsync(ct);

        return new SnowWarLeaderboardBoard(
            SnowWarLeaderboardPages.Rank(scores.Select(s => (s.PlayerId, s.Score))),
            Now
        );
    }

    private async Task LoadNamesAsync(
        SnowWarLeaderboardBoard board,
        ImmutableArray<SnowWarRankedScore> slice,
        CancellationToken ct
    )
    {
        var missing = slice
            .Select(r => r.PlayerId)
            .Where(id => !board.Names.ContainsKey(id))
            .ToList();

        if (missing.Count == 0)
            return;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        var players = await dbCtx
            .Players.AsNoTracking()
            .Where(p => missing.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Figure,
                p.Gender,
            })
            .ToListAsync(ct);

        foreach (var player in players)
            board.Names[player.Id] = new SnowWarLeaderboardName(
                player.Name,
                player.Figure,
                player.Gender.ToLegacyString()
            );
    }

    private static SnowWarLeaderboardEntrySnapshot Entry(
        SnowWarRankedScore ranked,
        SnowWarLeaderboardName? name
    ) =>
        new()
        {
            UserId = ranked.PlayerId,
            Score = (int)Math.Min(int.MaxValue, ranked.Score),
            Rank = ranked.Rank,
            Name = name?.Name ?? string.Empty,
            Figure = name?.Figure ?? string.Empty,
            Gender = name?.Gender ?? AvatarGenderType.Male.ToLegacyString(),
        };

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
