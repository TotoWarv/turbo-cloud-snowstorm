using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2StageStartingMessageComposer : IComposer
{
    [Id(0)]
    public required int GameType { get; init; }

    [Id(1)]
    public required string RoomType { get; init; }

    [Id(2)]
    public required int CountDown { get; init; }

    [Id(3)]
    public required ImmutableArray<SnowWarGameObjectSnapshot> Objects { get; init; }
}
