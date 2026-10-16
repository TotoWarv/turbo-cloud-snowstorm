using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2GameCreatedMessageComposer : IComposer
{
    [Id(0)]
    public required SnowWarGameLobbySnapshot Lobby { get; init; }
}
