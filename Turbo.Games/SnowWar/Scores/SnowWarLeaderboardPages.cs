using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Scores;

internal static class SnowWarLeaderboardPages
{
    public const int AROUND_REQUESTER = -1;
    private const int FIRST_RANK = 1;

    public static ImmutableArray<SnowWarRankedScore> Rank(
        IEnumerable<(int PlayerId, long Score)> scores
    )
    {
        var ordered = scores
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.PlayerId)
            .ToList();
        var ranked = ImmutableArray.CreateBuilder<SnowWarRankedScore>(ordered.Count);

        for (var index = 0; index < ordered.Count; index++)
        {
            var rank =
                index > 0 && ordered[index].Score == ordered[index - 1].Score
                    ? ranked[index - 1].Rank
                    : index + 1;

            ranked.Add(new SnowWarRankedScore(ordered[index].PlayerId, ordered[index].Score, rank));
        }

        return ranked.MoveToImmutable();
    }

    public static ImmutableArray<SnowWarRankedScore> Slice(
        ImmutableArray<SnowWarRankedScore> ranked,
        SnowWarLeaderboardRequestSnapshot request,
        int maxRows
    )
    {
        var viewSize = Math.Max(0, request.ViewSize);
        var rows = Math.Min(maxRows, Math.Max(1, Math.Max(viewSize, request.WindowSize)));
        var start = StartOf(ranked, request, viewSize);

        return [.. ranked.Skip(start - FIRST_RANK).Take(rows)];
    }

    private static int StartOf(
        ImmutableArray<SnowWarRankedScore> ranked,
        SnowWarLeaderboardRequestSnapshot request,
        int viewSize
    )
    {
        if (request.StartRank != AROUND_REQUESTER)
            return Math.Max(FIRST_RANK, request.StartRank);

        var mine = ranked.FirstOrDefault(r => r.PlayerId == request.PlayerId.Value);

        return mine is null ? FIRST_RANK : Math.Max(FIRST_RANK, mine.Rank - viewSize / 2);
    }
}
