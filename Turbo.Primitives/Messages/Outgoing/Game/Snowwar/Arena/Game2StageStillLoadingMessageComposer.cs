using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2StageStillLoadingMessageComposer : IComposer
{
    [Id(0)]
    public required int Percentage { get; init; }

    [Id(1)]
    public required ImmutableArray<int> FinishedUserIds { get; init; }
}
