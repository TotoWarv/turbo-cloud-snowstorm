using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarTeamPlayerSnapshot
{
    [Id(0)]
    public required string UserName { get; init; }

    [Id(1)]
    public required int UserId { get; init; }

    [Id(2)]
    public required string Figure { get; init; }

    [Id(3)]
    public required string Gender { get; init; }

    [Id(4)]
    public required int Score { get; init; }

    [Id(5)]
    public required SnowWarPlayerStatsSnapshot Stats { get; init; }
}
