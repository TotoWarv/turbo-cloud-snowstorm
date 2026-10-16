using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarPlayerSnapshot
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required string Figure { get; init; }

    [Id(3)]
    public required string Gender { get; init; }

    [Id(4)]
    public required int TeamId { get; init; }

    [Id(5)]
    public required int SkillLevel { get; init; }

    [Id(6)]
    public required int TotalScore { get; init; }

    [Id(7)]
    public required int ScoreToNextLevel { get; init; }
}
