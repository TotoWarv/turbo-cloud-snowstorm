using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Score;

[GenerateSerializer, Immutable]
public sealed record Game2TotalLeaderboardMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SnowWarLeaderboardEntrySnapshot> Entries { get; init; }

    [Id(1)]
    public required int TotalListSize { get; init; }

    [Id(2)]
    public required int GameTypeId { get; init; }
}
