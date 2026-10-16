using System;
using Turbo.Primitives.Players;

namespace Turbo.Games.SnowWar.Scores;

internal sealed record SnowWarPendingScore(
    PlayerId PlayerId,
    DateOnly WeekStart,
    long Score,
    int Matches
);
