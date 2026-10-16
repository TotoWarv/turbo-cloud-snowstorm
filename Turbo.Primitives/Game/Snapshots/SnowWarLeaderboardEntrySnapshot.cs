using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarLeaderboardEntrySnapshot
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required int Score { get; init; }

    [Id(2)]
    public required int Rank { get; init; }

    [Id(3)]
    public required string Name { get; init; }

    [Id(4)]
    public required string Figure { get; init; }

    [Id(5)]
    public required string Gender { get; init; }
}
