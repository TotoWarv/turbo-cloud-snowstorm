using System.Collections.Immutable;
using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarLeaderboardPageSnapshot
{
    [Id(0)]
    public required ImmutableArray<SnowWarLeaderboardEntrySnapshot> Entries { get; init; }

    [Id(1)]
    public required int TotalListSize { get; init; }
}
