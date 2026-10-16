using System.Collections.Immutable;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed record SnowWarPendingInput(
    int HumanId,
    SnowWarInput Input,
    ImmutableArray<int> Values,
    int Turn,
    int Subturn
);
