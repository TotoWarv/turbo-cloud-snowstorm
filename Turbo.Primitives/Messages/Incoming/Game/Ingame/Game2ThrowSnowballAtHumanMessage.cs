using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Game.Ingame;

public record Game2ThrowSnowballAtHumanMessage : IMessageEvent
{
    public required int TargetHumanId { get; init; }
    public required int Trajectory { get; init; }
    public required int Turn { get; init; }
    public required int Subturn { get; init; }
}
