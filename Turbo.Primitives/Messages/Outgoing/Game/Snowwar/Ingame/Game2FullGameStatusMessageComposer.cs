using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;

[GenerateSerializer, Immutable]
public sealed record Game2FullGameStatusMessageComposer : IComposer
{
    [Id(0)]
    public required int RemainingSeconds { get; init; }

    [Id(1)]
    public required int DurationSeconds { get; init; }

    [Id(2)]
    public required ImmutableArray<SnowWarGameObjectSnapshot> Objects { get; init; }

    [Id(3)]
    public required int NumberOfTeams { get; init; }

    [Id(4)]
    public required int Turn { get; init; }

    [Id(5)]
    public required int Checksum { get; init; }

    [Id(6)]
    public required ImmutableArray<ImmutableArray<SnowWarGameEventSnapshot>> Subturns { get; init; }
}
