using Orleans;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2StartingGameFailedMessageMessageComposer : IComposer
{
    [Id(0)]
    public required SnowWarJoinFailureReason Reason { get; init; }
}
