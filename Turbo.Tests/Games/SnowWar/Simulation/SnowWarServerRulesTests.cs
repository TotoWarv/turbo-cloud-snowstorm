using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarServerRulesTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void Input_WithASubturnOutOfRangeIsIgnored(int subturn)
    {
        var arena = Duel(out var blue, out _);
        Send(arena, blue, SnowWarInput.MoveTarget, [Center(4), Center(4)], subturn);
        Turns(arena, 1);

        Assert.Empty(AllEvents(arena, arena.NextTurn));
        Assert.Empty(AllEvents(arena, arena.NextTurn + 1));
    }

    [Theory]
    [InlineData(SnowWarInput.MoveTarget, new[] { 3200 })]
    [InlineData(SnowWarInput.MoveTarget, new[] { 3200, 3200, 3200 })]
    [InlineData(SnowWarInput.MakeSnowball, new[] { 1 })]
    [InlineData(SnowWarInput.ThrowAtPosition, new[] { 3200, 3200 })]
    [InlineData(SnowWarInput.ThrowAtHuman, new[] { 2 })]
    [InlineData((SnowWarInput)9, new int[0])]
    public void Input_WithTheWrongNumberOfValuesIsIgnored(SnowWarInput input, int[] values)
    {
        var arena = Duel(out var blue, out _);

        Assert.False(SnowWarServerRules.Accepts(arena, blue, input, values));

        Send(arena, blue, input, values);
        Turns(arena, 1);
        Assert.Empty(AllEvents(arena, arena.NextTurn));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Throw_WithAnUnknownTrajectoryIsIgnored(int trajectory)
    {
        var arena = Duel(out var blue, out var red);

        Assert.False(
            SnowWarServerRules.Accepts(arena, blue, SnowWarInput.ThrowAtHuman, [red, trajectory])
        );
        Assert.False(
            SnowWarServerRules.Accepts(
                arena,
                blue,
                SnowWarInput.ThrowAtPosition,
                [Center(4), Center(4), trajectory]
            )
        );
    }

    [Fact]
    public void ThrowAtHuman_OnlyTargetsAnExistingOpponent()
    {
        var arena = new SnowWarArena(Open(20, 10, Furni("snst_tree1", 9, 9)), 1);
        var blue = AddHuman(arena, 1, 2, 5);
        var mate = AddHuman(arena, 1, 2, 7);
        var red = AddHuman(arena, 2, 8, 5);

        Assert.False(SnowWarServerRules.Accepts(arena, blue, SnowWarInput.ThrowAtHuman, [mate, 0]));
        Assert.False(SnowWarServerRules.Accepts(arena, blue, SnowWarInput.ThrowAtHuman, [blue, 0]));
        Assert.False(SnowWarServerRules.Accepts(arena, blue, SnowWarInput.ThrowAtHuman, [1, 0]));
        Assert.False(SnowWarServerRules.Accepts(arena, blue, SnowWarInput.ThrowAtHuman, [999, 0]));
        Assert.True(SnowWarServerRules.Accepts(arena, blue, SnowWarInput.ThrowAtHuman, [red, 3]));
        Assert.False(SnowWarServerRules.Accepts(arena, 999, SnowWarInput.MakeSnowball, []));
    }

    [Fact]
    public void ThrowAtHuman_AimsAtTheTargetsPositionWhenPlaced()
    {
        var arena = Duel(out var blue, out var red);
        ThrowAtHuman(arena, blue, red, SnowWarTrajectory.Default);
        Turns(arena, 1);

        var events = SubturnEvents(arena, arena.NextTurn, 0);
        Assert.Equal(SnowWarGameEventType.HumanThrowsSnowballAtHuman, events[0].Type);
        Assert.Equal([blue, red, 3], events[0].Fields);
        Assert.Equal(SnowWarGameEventType.CreateSnowball, events[1].Type);
        Assert.Equal([blue, Center(8), Center(5), 3], events[1].Fields.Skip(1));
        Assert.True(events[1].Fields[0] > red);
    }

    [Fact]
    public void Throw_WithoutSnowballsIsIgnored()
    {
        var arena = Duel(out var blue, out _);

        for (var i = 0; i < 5; i++)
        {
            ThrowAt(arena, blue, 2, 0);
            Turns(arena, 3);
        }

        Assert.Equal(0, Human(arena, blue).Balls);
        ThrowAt(arena, blue, 2, 0);
        Turns(arena, 1);

        Assert.Empty(AllEvents(arena, arena.NextTurn));
    }

    [Fact]
    public void Throw_BeforeTheThrowTimerRunsOutIsIgnored()
    {
        var arena = Duel(out var blue, out _);
        ThrowAt(arena, blue, 2, 0);
        Turns(arena, 1);
        ThrowAt(arena, blue, 2, 0);
        Turns(arena, 1);

        Assert.Empty(AllEvents(arena, arena.NextTurn));
        Assert.Equal(4, Human(arena, blue).Balls);
    }

    [Fact]
    public void Throw_WhileAnotherSnowballIsPendingIsIgnored()
    {
        var arena = Duel(out var blue, out var red);
        ThrowAt(arena, blue, 2, 0);
        ThrowAtHuman(arena, blue, red);
        ThrowAt(arena, blue, 9, 9);
        Turns(arena, 1);

        Assert.Equal(
            [
                SnowWarGameEventType.HumanThrowsSnowballAtPosition,
                SnowWarGameEventType.CreateSnowball,
            ],
            AllEvents(arena, arena.NextTurn).Select(e => e.Type)
        );
    }

    [Fact]
    public void Input_IsNeverPlacedInThePastNorTooFarAhead()
    {
        var arena = Duel(out var blue, out _);
        Turns(arena, 10);

        arena.ScheduleInput(blue, SnowWarInput.MoveTarget, [Center(3), Center(5)], 2, 2);
        Turns(arena, 1);
        Assert.Single(SubturnEvents(arena, 11, 0));

        Turns(arena, 1);
        arena.ScheduleInput(blue, SnowWarInput.MoveTarget, [Center(3), Center(5)], 500, 2);
        Turns(arena, 1);
        Assert.Single(SubturnEvents(arena, 14, 0));

        arena.ScheduleInput(blue, SnowWarInput.MoveTarget, [Center(3), Center(5)], 14, 2);
        Turns(arena, 1);
        Assert.Single(SubturnEvents(arena, 14, 2));

        arena.ScheduleInput(blue, SnowWarInput.MoveTarget, [Center(3), Center(5)], 16, 1);
        Turns(arena, 1);
        Assert.Single(SubturnEvents(arena, 16, 1));
    }

    [Fact]
    public void Inputs_BeyondTwelvePendingAreDropped()
    {
        var arena = Duel(out var blue, out _);
        ThrowAt(arena, blue, 2, 0);
        Turns(arena, 3);

        for (var i = 0; i < 12; i++)
        {
            Send(arena, blue, SnowWarInput.MakeSnowball, []);
        }

        MoveTo(arena, blue, 4, 5);
        Turns(arena, 1);

        Assert.Equal(
            [SnowWarGameEventType.HumanStartsToMakeASnowball],
            AllEvents(arena, arena.NextTurn).Select(e => e.Type)
        );
    }

    [Fact]
    public void Inputs_OfAnUnknownHumanAreIgnored()
    {
        var arena = Duel(out _, out _);
        Send(arena, 999, SnowWarInput.MoveTarget, [Center(4), Center(4)]);
        Turns(arena, 1);

        Assert.Empty(AllEvents(arena, arena.NextTurn));
    }

    [Fact]
    public void MakeSnowball_IsRefusedWhileAnotherIsPending()
    {
        var arena = Duel(out var blue, out _);
        ThrowAt(arena, blue, 2, 0);
        Turns(arena, 3);
        Send(arena, blue, SnowWarInput.MakeSnowball, []);
        Turns(arena, 1);

        Assert.False(SnowWarServerRules.Accepts(arena, blue, SnowWarInput.MakeSnowball, []));
    }

    private static SnowWarArena Duel(out int blue, out int red)
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        blue = AddHuman(arena, 1, 2, 5);
        red = AddHuman(arena, 2, 8, 5);
        return arena;
    }
}
