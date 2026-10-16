namespace Turbo.Games.SnowWar.Simulation;

public sealed record SnowWarHumanEntry
{
    public required int UserId { get; init; }

    public required string Name { get; init; }

    public required string Mission { get; init; }

    public required string Figure { get; init; }

    public required string Gender { get; init; }

    public required int Team { get; init; }
}
