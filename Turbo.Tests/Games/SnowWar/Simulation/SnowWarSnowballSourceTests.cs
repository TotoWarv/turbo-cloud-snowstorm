using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarSnowballSourceTests
{
    [Fact]
    public void Machine_RefillsEveryHundredAndOneSubturnsUntilFull()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("s_snowball_machine", 5, 2)), 1);
        var refills = new List<(int, int)>();

        for (var i = 0; i < 260; i++)
        {
            var result = arena.RunTurn();
            var subturns = arena.GetScheduledEvents(result.Turn + 1);

            for (var subturn = 0; subturn < 3; subturn++)
            {
                foreach (var scheduled in subturns[subturn])
                {
                    Assert.Equal(SnowWarGameEventType.MachineCreatesSnowball, scheduled.Type);
                    Assert.Equal([1], scheduled.Fields);
                    refills.Add((result.Turn + 1, subturn));
                }
            }
        }

        Assert.Equal([(34, 1), (68, 0), (101, 2), (135, 1), (169, 0)], refills);
        Assert.Equal(5, Object(arena, 1).Variables[4]);
    }

    [Fact]
    public void Machine_GivesOneSnowballToAHumanStandingOnItsWithdrawTile()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("s_snowball_machine", 5, 2)), 1);
        var human = AddHuman(arena, 1, 5, 3);
        ThrowAt(arena, human, 5, 9);
        Turns(arena, 3);
        ThrowAt(arena, human, 5, 9);
        Turns(arena, 3);
        Assert.Equal(3, Human(arena, human).Balls);

        var pickups = new List<(int, int)>();

        while (arena.NextTurn < 110)
        {
            var result = arena.RunTurn();
            var subturns = arena.GetScheduledEvents(result.Turn + 1);

            for (var subturn = 0; subturn < 3; subturn++)
            {
                foreach (var scheduled in subturns[subturn])
                {
                    if (scheduled.Type == SnowWarGameEventType.HumanGetsSnowballsFromMachine)
                    {
                        Assert.Equal([human, 1], scheduled.Fields);
                        pickups.Add((result.Turn + 1, subturn));
                    }
                }
            }
        }

        Assert.Equal([(35, 0), (69, 0)], pickups);
        Assert.Equal(5, Human(arena, human).Balls);
        Assert.Equal(1, Object(arena, 1).Variables[4]);
        Assert.Equal(2, arena.GetHumanStats(human)!.SnowballsFromMachine);
    }

    [Fact]
    public void Pile_GivesASnowballEveryTwentySubturnsToAnAdjacentStillHuman()
    {
        var arena = new SnowWarArena(Open(10, 12, Furni("snst_ballpile", 5, 5)), 1);
        var human = AddHuman(arena, 1, 5, 9);

        for (var i = 0; i < 3; i++)
        {
            ThrowAt(arena, human, 5, 11);
            Turns(arena, 3);
        }

        Assert.Equal(2, Human(arena, human).Balls);
        MoveTo(arena, human, 5, 6);

        var pickups = new List<int>();

        for (var i = 0; i < 40; i++)
        {
            var result = arena.RunTurn();
            var subturns = arena.GetScheduledEvents(result.Turn + 1);

            for (var subturn = 0; subturn < 3; subturn++)
            {
                if (
                    subturns[subturn]
                        .Any(scheduled =>
                            scheduled.Type == SnowWarGameEventType.HumanGetsSnowballsFromMachine
                        )
                )
                {
                    pickups.Add(((result.Turn + 1) * 3) + subturn);
                }
            }
        }

        Assert.Equal(3, pickups.Count);
        Assert.Equal(20, pickups[1] - pickups[0]);
        Assert.Equal(20, pickups[2] - pickups[1]);
        Assert.Equal(5, Human(arena, human).Balls);
        Assert.Equal(9, Object(arena, 1).Variables[3]);
    }

    [Fact]
    public void Pile_IsOutOfReachDiagonally()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("snst_ballpile", 5, 5)), 1);
        var human = AddHuman(arena, 1, 6, 6);
        ThrowAt(arena, human, 9, 9);
        Turns(arena, 30);

        Assert.Equal(4, Human(arena, human).Balls);
        Assert.Equal(12, Object(arena, 1).Variables[3]);
    }

    [Fact]
    public void EmptyPile_LeavesItsTileButTheTileStaysBlocked()
    {
        var world = new SnowWarWorld(Open(10, 10, Furni("snst_ballpile", 5, 5)));
        var pile = (SnowWarPile)world.Objects[0];
        var tile = world.Grid.Get(5, 5)!;

        for (var i = 0; i < 12; i++)
        {
            Assert.True(pile.TryTakeSnowball());
        }

        Assert.False(pile.TryTakeSnowball());
        Assert.Null(tile.Occupant);
        Assert.False(tile.IsWalkable);
        Assert.False(
            pile.StopsSnowball(new(9, 16000, 16000, 10, 0, SnowWarTrajectory.Quick, 0, 1, 0, 0))
        );
    }

    [Fact]
    public void Machine_NeverHoldsMoreThanFive()
    {
        var world = new SnowWarWorld(Open(10, 10, Furni("s_snowball_machine", 5, 5)));
        var machine = (SnowWarMachine)world.Objects[0];

        for (var i = 0; i < 7; i++)
        {
            machine.AddSnowball();
        }

        Assert.Equal(5, machine.Stock);
        Assert.Equal((5, 6), (machine.WithdrawTileX, machine.WithdrawTileY));
    }
}
