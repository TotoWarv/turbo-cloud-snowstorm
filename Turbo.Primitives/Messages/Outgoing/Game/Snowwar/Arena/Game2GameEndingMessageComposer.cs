using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2GameEndingMessageComposer : IComposer
{
    [Id(0)]
    public required int TimeToNextState { get; init; }

    [Id(1)]
    public required bool IsDeathMatch { get; init; }

    [Id(2)]
    public required SnowWarGameResultType ResultType { get; init; }

    [Id(3)]
    public required int WinnerId { get; init; }

    [Id(4)]
    public required ImmutableArray<SnowWarTeamScoreSnapshot> Teams { get; init; }

    [Id(5)]
    public required int PlayerWithMostKills { get; init; }

    [Id(6)]
    public required int PlayerWithMostHits { get; init; }
}
