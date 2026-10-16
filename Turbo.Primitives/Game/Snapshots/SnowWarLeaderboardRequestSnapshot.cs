using Orleans;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarLeaderboardRequestSnapshot
{
    [Id(0)]
    public required PlayerId PlayerId { get; init; }

    [Id(1)]
    public required SnowWarLeaderboardScope Scope { get; init; }

    [Id(2)]
    public required int StartRank { get; init; }

    [Id(3)]
    public required int ViewSize { get; init; }

    [Id(4)]
    public required int WindowSize { get; init; }
}
