using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Score;

[GenerateSerializer, Immutable]
public sealed record Game2WeeklyLeaderboardMessageComposer : IComposer
{
    [Id(0)]
    public required int Year { get; init; }

    [Id(1)]
    public required int Week { get; init; }

    [Id(2)]
    public required int MaxOffset { get; init; }

    [Id(3)]
    public required int CurrentOffset { get; init; }

    [Id(4)]
    public required int MinutesUntilReset { get; init; }

    [Id(5)]
    public required ImmutableArray<SnowWarLeaderboardEntrySnapshot> Entries { get; init; }

    [Id(6)]
    public required int TotalListSize { get; init; }

    [Id(7)]
    public required int GameTypeId { get; init; }
}
