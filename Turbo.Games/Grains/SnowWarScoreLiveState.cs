using System;
using System.Collections.Generic;
using Turbo.Games.SnowWar.Scores;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed class SnowWarScoreLiveState
{
    public List<SnowWarPendingScore> PendingScores { get; } = [];

    public Dictionary<
        (PlayerId, SnowWarLeaderboardKind, SnowWarLeaderboardScope),
        DateTimeOffset
    > LastRequests { get; } = [];
}
