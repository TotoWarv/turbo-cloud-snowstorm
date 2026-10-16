using Orleans;
using Turbo.Primitives.Furniture.Snapshots.StuffData;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarFuseObjectSnapshot
{
    [Id(0)]
    public required string Name { get; init; }

    [Id(1)]
    public required int Id { get; init; }

    [Id(2)]
    public required int X { get; init; }

    [Id(3)]
    public required int Y { get; init; }

    [Id(4)]
    public required int XDimension { get; init; }

    [Id(5)]
    public required int YDimension { get; init; }

    [Id(6)]
    public required int Height { get; init; }

    [Id(7)]
    public required int Direction { get; init; }

    [Id(8)]
    public required int Altitude { get; init; }

    [Id(9)]
    public required bool CanStandOn { get; init; }

    [Id(10)]
    public required StuffDataSnapshot StuffData { get; init; }
}
