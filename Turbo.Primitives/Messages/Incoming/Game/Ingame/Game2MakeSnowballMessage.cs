using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Game.Ingame;

public record Game2MakeSnowballMessage : IMessageEvent
{
    public required int Turn { get; init; }
    public required int Subturn { get; init; }
}
