using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarFuseObject(
    int id,
    SnowWarFurniDefinition definition,
    int x,
    int y,
    int direction,
    int altitude
)
{
    public int Id { get; } = id;

    public SnowWarFurniDefinition Definition { get; } = definition;

    public int X { get; } = x;

    public int Y { get; } = y;

    public int Direction { get; } = direction;

    public int Altitude { get; } = altitude;

    public int Height => Definition.Height;

    public bool CanStandOn => Definition.Role == SnowWarFurniRole.Backdrop;

    public int FootprintWidth =>
        Direction is 2 or 6 ? Definition.YDimension : Definition.XDimension;

    public int FootprintDepth =>
        Direction is 2 or 6 ? Definition.XDimension : Definition.YDimension;
}
