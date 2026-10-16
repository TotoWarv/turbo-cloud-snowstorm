using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2ArenaEnteredMessageComposer : IComposer
{
    [Id(0)]
    public required SnowWarArenaPlayerSnapshot Player { get; init; }
}
