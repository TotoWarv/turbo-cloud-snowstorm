using System;

namespace Turbo.Games.SnowWar.Scores;

internal sealed record SnowWarCachedOldestWeek(DateOnly? Week, DateTimeOffset LoadedAt);
