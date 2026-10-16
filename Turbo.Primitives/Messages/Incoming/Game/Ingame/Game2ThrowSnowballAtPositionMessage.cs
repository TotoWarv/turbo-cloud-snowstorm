using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Game.Ingame;

public record Game2ThrowSnowballAtPositionMessage : IMessageEvent
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Trajectory { get; init; }
    public required int Turn { get; init; }
    public required int Subturn { get; init; }
}
