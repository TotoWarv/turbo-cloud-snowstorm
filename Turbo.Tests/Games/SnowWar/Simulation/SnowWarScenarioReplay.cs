using System.Text.Json;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Xunit;

namespace Turbo.Tests.Games.SnowWar.Simulation;

internal static class SnowWarScenarioReplay
{
    public static readonly JsonSerializerOptions JSON = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string ReadFixture(string name)
    {
        using var stream = typeof(SnowWarScenarioReplay).Assembly.GetManifestResourceStream(
            $"Games.SnowWar.Fixtures.{name}"
        );
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static SnowWarScenario LoadScenario(string name) =>
        JsonSerializer.Deserialize<SnowWarScenario>(ReadFixture(name), JSON)!;

    public static SnowWarScenarioRecording Run(SnowWarScenario scenario)
    {
        var (arena, ids) = Start(scenario);
        var initial = arena.GetFullStatus().Select(Record).ToList();
        var turns = new List<SnowWarRecordedTurn>();

        for (var turn = 0; turn < scenario.Turns; turn++)
            turns.Add(Step(scenario, arena, ids, turn));

        return new(scenario.Seed, initial, turns);
    }

    public static SnowWarRecordedFullStatus FullStatusAfter(SnowWarScenario scenario, int lastTurn)
    {
        var (arena, ids) = Start(scenario);
        SnowWarRecordedTurn? recorded = null;

        for (var turn = 0; turn <= lastTurn; turn++)
            recorded = Step(scenario, arena, ids, turn);

        Assert.NotNull(recorded);

        return new(
            recorded.Turn,
            recorded.Checksum,
            arena.GetFullStatus().Select(Record).ToList(),
            recorded.Events
        );
    }

    private static (SnowWarArena Arena, List<int> Ids) Start(SnowWarScenario scenario)
    {
        var arena = new SnowWarArena(scenario.Level, scenario.Seed);
        var ids = new List<int>();

        foreach (var human in scenario.Humans)
        {
            Assert.True(arena.TryAddHuman(human, out var id));
            ids.Add(id);
        }

        return (arena, ids);
    }

    private static SnowWarRecordedTurn Step(
        SnowWarScenario scenario,
        SnowWarArena arena,
        List<int> ids,
        int turn
    )
    {
        foreach (var input in scenario.Inputs.Where(input => input.AtTurn == turn))
        {
            Submit(arena, ids, input);
        }

        var result = arena.RunTurn();
        var events = arena
            .GetScheduledEvents(result.Turn + 1)
            .Select(subturn => subturn.Select(e => (int[])[(int)e.Type, .. e.Fields]).ToArray())
            .ToArray();

        return new(result.Turn, result.Checksum, events);
    }

    private static void Submit(SnowWarArena arena, List<int> ids, SnowWarScenarioInput input)
    {
        var humanId = ids[input.Human];

        if (input.Input == "Leave")
        {
            arena.RemoveHuman(humanId);
            return;
        }

        var kind = Enum.Parse<SnowWarInput>(input.Input);
        int[] values = input.Target is int target ? [ids[target], .. input.Values] : input.Values;

        for (var i = 0; i < input.Count; i++)
        {
            arena.ScheduleInput(humanId, kind, values, input.Turn, input.Subturn);
        }
    }

    private static SnowWarRecordedObject Record(SnowWarGameObjectSnapshot snapshot) =>
        new((int)snapshot.Type, snapshot.Id, [.. snapshot.Variables], [.. snapshot.Strings]);
}
