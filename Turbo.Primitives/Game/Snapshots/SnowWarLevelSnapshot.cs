using System.Collections.Immutable;
using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarLevelSnapshot
{
    [Id(0)]
    public required int Width { get; init; }

    [Id(1)]
    public required int Height { get; init; }

    [Id(2)]
    public required string HeightMap { get; init; }

    [Id(3)]
    public required ImmutableArray<SnowWarFuseObjectSnapshot> FuseObjects { get; init; }
}
