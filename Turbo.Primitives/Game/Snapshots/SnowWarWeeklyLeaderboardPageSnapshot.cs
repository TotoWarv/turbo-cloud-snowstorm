using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarWeeklyLeaderboardPageSnapshot
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
    public required SnowWarLeaderboardPageSnapshot Page { get; init; }
}
