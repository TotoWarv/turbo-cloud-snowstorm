using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Game.Ingame;

public record Game2RequestFullStatusUpdateMessage : IMessageEvent
{
    public required int Reason { get; init; }
}
