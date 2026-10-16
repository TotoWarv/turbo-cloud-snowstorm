using System.Collections.Immutable;
using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarTeamScoreSnapshot
{
    [Id(0)]
    public required int TeamId { get; init; }

    [Id(1)]
    public required int Score { get; init; }

    [Id(2)]
    public required ImmutableArray<SnowWarTeamPlayerSnapshot> Players { get; init; }
}
