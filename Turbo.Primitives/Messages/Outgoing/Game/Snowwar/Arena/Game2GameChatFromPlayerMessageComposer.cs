using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2GameChatFromPlayerMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required string Message { get; init; }
}
