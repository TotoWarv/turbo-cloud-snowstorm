using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarArenaPlayerSnapshot
{
    [Id(0)]
    public required int ReferenceId { get; init; }

    [Id(1)]
    public required string UserName { get; init; }

    [Id(2)]
    public required string Figure { get; init; }

    [Id(3)]
    public required string Gender { get; init; }

    [Id(4)]
    public required int TeamId { get; init; }
}
