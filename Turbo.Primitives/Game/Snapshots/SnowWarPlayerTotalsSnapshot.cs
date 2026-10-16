using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarPlayerTotalsSnapshot
{
    [Id(0)]
    public required long TotalScore { get; init; }

    [Id(1)]
    public required int GamesPlayed { get; init; }

    [Id(2)]
    public required int SkillLevel { get; init; }

    [Id(3)]
    public required int ScoreToNextLevel { get; init; }
}
