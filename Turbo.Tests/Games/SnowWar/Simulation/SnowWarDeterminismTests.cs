using System.Text.Json;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
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

    [Fact]
    public void Scenario1_FullStatusCaptures_MatchTheServerSimulation()
    {
        var scenario = SnowWarScenarioReplay.LoadScenario("scenario-1.json");
        var captures = FullStatusCaptures();

        Assert.NotEmpty(captures);

        foreach (var capture in captures)
            Assert.Equal(
                JsonSerializer.Serialize(
                    SnowWarScenarioReplay.FullStatusAfter(scenario, capture.Turn),
                    SnowWarScenarioReplay.JSON
                ),
                JsonSerializer.Serialize(capture, SnowWarScenarioReplay.JSON)
            );
    }

    [Fact]
    public void Scenario1_FullStatusCaptures_CatchHumansBetweenTilesAndBallsInFlight()
    {
        foreach (var capture in FullStatusCaptures())
        {
            Assert.Contains(capture.Objects, o => o.Type == (int)SnowWarGameObjectType.Snowball);
            Assert.Contains(
                capture.Objects.Where(o => o.Type == (int)SnowWarGameObjectType.Human),
                human => IsBetweenTiles(SnowWarHumanView.From(Snapshot(human)))
            );
        }
    }

    private static List<SnowWarRecordedFullStatus> FullStatusCaptures() =>
        JsonSerializer.Deserialize<List<SnowWarRecordedFullStatus>>(
            SnowWarScenarioReplay.ReadFixture("scenario-1.full-status.json"),
            SnowWarScenarioReplay.JSON
        )!;

    private static bool IsBetweenTiles(SnowWarHumanView human) =>
        (human.NextX, human.NextY) != (human.TileX, human.TileY)
        && (human.X, human.Y)
            != (SnowWarMath.TileToWorld(human.TileX), SnowWarMath.TileToWorld(human.TileY));

    private static SnowWarGameObjectSnapshot Snapshot(SnowWarRecordedObject recorded) =>
        new()
        {
            Type = (SnowWarGameObjectType)recorded.Type,
            Id = recorded.Id,
            Variables = [.. recorded.Variables],
            Strings = [.. recorded.Strings],
        };
}
