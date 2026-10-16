using System.Collections.Immutable;

namespace Turbo.Games.SnowWar.Simulation;

public sealed record SnowWarLevelData
{
    public required int FieldType { get; init; }

    public required string Name { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required ImmutableArray<string> HeightMap { get; init; }

    public required ImmutableArray<SnowWarLevelObjectData> Objects { get; init; }

    public ImmutableArray<ImmutableArray<SnowWarTilePosition>> TeamSpawns { get; init; } = [];

    public SnowWarBackdropData? Backdrop { get; init; }
}
