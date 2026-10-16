using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2UserJoinedGameMessageComposer : IComposer
{
    [Id(0)]
    public required SnowWarPlayerSnapshot Player { get; init; }

    [Id(1)]
    public required bool WasTeamSwitched { get; init; }
}
