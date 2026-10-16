namespace Turbo.Games.SnowWar.Simulation;

public sealed record SnowWarLevelObjectData
{
    public required string Name { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int Direction { get; init; }

    public int Altitude { get; init; }
}
