namespace Turbo.Tests.Games.SnowWar.Simulation;

internal sealed record SnowWarScenarioRecording(
    int Seed,
    IReadOnlyList<SnowWarRecordedObject> InitialObjects,
    IReadOnlyList<SnowWarRecordedTurn> Turns
);
