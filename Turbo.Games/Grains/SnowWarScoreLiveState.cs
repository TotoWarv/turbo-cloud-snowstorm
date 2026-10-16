using System;
using System.Collections.Generic;
using Turbo.Games.SnowWar.Scores;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed class SnowWarScoreLiveState
{
    public List<SnowWarPendingScore> PendingScores { get; } = [];

    public int DroppedScores { get; set; }

    public Dictionary<PlayerId, SnowWarCachedTotals> Totals { get; } = [];

    public Dictionary<(SnowWarLeaderboardKind, DateOnly), SnowWarLeaderboardBoard> Boards { get; } =
    [];

    public SnowWarCachedOldestWeek? OldestWeek { get; set; }

    public Dictionary<
        (PlayerId, SnowWarLeaderboardKind, SnowWarLeaderboardScope),
        DateTimeOffset
    > LastRequests { get; } = [];
}
