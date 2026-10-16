using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2StageEndingMessageComposer : IComposer
{
    [Id(0)]
    public required int TimeToNextState { get; init; }
}
