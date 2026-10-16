using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed record SnowWarFurniDefinition(
    string Name,
    int XDimension,
    int YDimension,
    int Height,
    SnowWarFurniRole Role
);
