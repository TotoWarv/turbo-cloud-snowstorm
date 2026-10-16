using System.Text.Json;
using Xunit;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarDeterminismTests
{
    [Fact]
    public void Scenario1_ReplayedTwice_GivesTheSameChecksumsAndEvents()
    {
        var scenario = SnowWarScenarioReplay.LoadScenario("scenario-1.json");

        var first = JsonSerializer.Serialize(
            SnowWarScenarioReplay.Run(scenario),
            SnowWarScenarioReplay.JSON
        );
        var second = JsonSerializer.Serialize(
            SnowWarScenarioReplay.Run(scenario),
            SnowWarScenarioReplay.JSON
        );

        Assert.Equal(first, second);
    }

    [Fact]
    public void Scenario1_MatchesTheRecordedReference()
    {
        var scenario = SnowWarScenarioReplay.LoadScenario("scenario-1.json");
        var recorded = JsonSerializer.Deserialize<SnowWarScenarioRecording>(
            SnowWarScenarioReplay.ReadFixture("scenario-1.checksums.json"),
            SnowWarScenarioReplay.JSON
        )!;

        var replayed = SnowWarScenarioReplay.Run(scenario);

        Assert.Equal(120, replayed.Turns.Count);
        Assert.Equal(
            recorded.Turns.Select(turn => turn.Checksum),
            replayed.Turns.Select(turn => turn.Checksum)
        );
        Assert.Equal(
            JsonSerializer.Serialize(recorded, SnowWarScenarioReplay.JSON),
            JsonSerializer.Serialize(replayed, SnowWarScenarioReplay.JSON)
        );
    }

    [Fact]
    public void Scenario1_ExercisesEveryKindOfEvent()
    {
        var replayed = SnowWarScenarioReplay.Run(
            SnowWarScenarioReplay.LoadScenario("scenario-1.json")
        );
        var types = replayed
            .Turns.SelectMany(turn => turn.Events)
            .SelectMany(subturn => subturn)
            .Select(e => e[0])
            .Distinct()
            .Order();

        Assert.Equal([1, 2, 3, 4, 7, 8, 11, 12, 100], types);
    }
}
