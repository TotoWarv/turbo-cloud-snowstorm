using Orleans;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2EnterArenaFailedMessageComposer : IComposer
{
    [Id(0)]
    public required SnowWarEnterArenaFailureReason Reason { get; init; }
}
