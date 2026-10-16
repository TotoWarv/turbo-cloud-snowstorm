namespace Turbo.Tests.Games.SnowWar.Simulation;

internal sealed record SnowWarScenarioInput(
    int AtTurn,
    int Human,
    string Input,
    int[] Values,
    int Turn,
    int Subturn,
    int Count = 1,
    int? Target = null
);
