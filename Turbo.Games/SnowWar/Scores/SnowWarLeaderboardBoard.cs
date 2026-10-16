using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Turbo.Games.SnowWar.Scores;

internal sealed class SnowWarLeaderboardBoard(
    ImmutableArray<SnowWarRankedScore> ranked,
    DateTimeOffset loadedAt
)
{
    public ImmutableArray<SnowWarRankedScore> Ranked { get; } = ranked;

    public DateTimeOffset LoadedAt { get; } = loadedAt;

    public Dictionary<int, SnowWarLeaderboardName> Names { get; } = [];
}
