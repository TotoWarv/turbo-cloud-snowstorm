namespace Turbo.Games.SnowWar.Simulation;

public sealed record SnowWarBackdropData
{
    public required string ImageUrl { get; init; }

    public required int X { get; init; }

    public required int Y { get; init; }

    public required int OffsetX { get; init; }

    public required int OffsetY { get; init; }

    public required int OffsetZ { get; init; }
}
