using Turbo.Games.SnowWar.Simulation;

namespace Turbo.Tests.Games.SnowWar.Simulation;

internal sealed record SnowWarScenario(
    int Seed,
    int Turns,
    SnowWarLevelData Level,
    IReadOnlyList<SnowWarHumanEntry> Humans,
    IReadOnlyList<SnowWarScenarioInput> Inputs
);
