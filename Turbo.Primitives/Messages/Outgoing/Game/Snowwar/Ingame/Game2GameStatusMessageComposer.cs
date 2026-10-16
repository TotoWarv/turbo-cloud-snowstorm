using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;

[GenerateSerializer, Immutable]
public sealed record Game2GameStatusMessageComposer : IComposer
{
    [Id(0)]
    public required int Turn { get; init; }

    [Id(1)]
    public required int Checksum { get; init; }

    [Id(2)]
    public required ImmutableArray<ImmutableArray<SnowWarGameEventSnapshot>> Subturns { get; init; }
}
