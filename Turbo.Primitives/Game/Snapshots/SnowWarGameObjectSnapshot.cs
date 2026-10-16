using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarGameObjectSnapshot
{
    [Id(0)]
    public required SnowWarGameObjectType Type { get; init; }

    [Id(1)]
    public required int Id { get; init; }

    [Id(2)]
    public required ImmutableArray<int> Variables { get; init; }

    [Id(3)]
    public required ImmutableArray<string> Strings { get; init; }
}
