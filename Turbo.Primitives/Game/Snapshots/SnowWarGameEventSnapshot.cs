using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarGameEventSnapshot
{
    [Id(0)]
    public required SnowWarGameEventType Type { get; init; }

    [Id(1)]
    public required ImmutableArray<int> Fields { get; init; }
}
