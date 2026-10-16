using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarPlayerStatsSnapshot
{
    [Id(0)]
    public required int Score { get; init; }

    [Id(1)]
    public required int Kills { get; init; }

    [Id(2)]
    public required int Deaths { get; init; }

    [Id(3)]
    public required int SnowballHits { get; init; }

    [Id(4)]
    public required int SnowballHitsTaken { get; init; }

    [Id(5)]
    public required int SnowballsThrown { get; init; }

    [Id(6)]
    public required int SnowballsCreated { get; init; }

    [Id(7)]
    public required int SnowballsFromMachine { get; init; }

    [Id(8)]
    public required int FriendlyHits { get; init; }

    [Id(9)]
    public required int FriendlyKills { get; init; }
}
