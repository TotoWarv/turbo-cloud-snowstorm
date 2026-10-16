using System;

namespace Turbo.Games.SnowWar.Scores;

internal sealed record SnowWarCachedTotals(long Score, int Matches, DateTimeOffset LoadedAt);
