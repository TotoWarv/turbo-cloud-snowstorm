using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2PlayerRematchesMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }
}
