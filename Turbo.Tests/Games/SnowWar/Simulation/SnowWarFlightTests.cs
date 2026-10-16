using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarFlightTests
{
    [Fact]
    public void QuickThrow_FliesFlatThenFallsAndLandsOnTheTwentyFourthSubturn()
    {
        var arena = new SnowWarArena(Open(40, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        ThrowAt(arena, thrower, 30, 5);
        Turns(arena, 1);

        var heights = new List<int>();
        var xs = new List<int>();

        for (var i = 0; i < 8; i++)
        {
            Turns(arena, 1);
            var balls = Balls(arena);

            if (balls.Count == 0)
            {
                break;
            }

            heights.Add(balls[0].Z);
            xs.Add(balls[0].X);
        }

        Assert.Equal([3000, 3000, 3000, 2760, 2250, 1560, 690], heights);
        Assert.Equal(6400 + (3 * 2007), xs[0]);
        Assert.Equal(6400 + (21 * 2007), xs[6]);
        Assert.Empty(Balls(arena));
    }

    [Fact]
    public void QuickThrow_DecrementsLifeBelowZeroWithoutDisappearing()
    {
        var arena = new SnowWarArena(Open(40, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        ThrowAt(arena, thrower, 30, 5);
        Turns(arena, 1);
        Turns(arena, 5);

        var ball = Assert.Single(Balls(arena));
        Assert.Equal(10 - 15, ball.Life);
        Assert.Equal(thrower, ball.ThrowerId);
        Assert.Equal(90, ball.Direction);
        Assert.Equal(0, ball.Trajectory);
    }

    [Fact]
    public void QuickThrow_IsStoppedWhenItsHeightFallsUnderATallTile()
    {
        var arena = new SnowWarArena(
            Open(
                40,
                10,
                Furni("snst_iceblock", 6, 5),
                Furni("snst_iceblock", 6, 5, altitude: 1760)
            ),
            1
        );
        var thrower = AddHuman(arena, 1, 2, 5);
        ThrowAt(arena, thrower, 30, 5);
        Turns(arena, 2);
        Assert.Single(Balls(arena));

        Turns(arena, 1);
        Assert.Empty(Balls(arena));
    }

    [Fact]
    public void QuickThrow_PassesOverALowBlockWhileStillFlat()
    {
        var arena = new SnowWarArena(Open(40, 10, Furni("snst_block1", 6, 5)), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        ThrowAt(arena, thrower, 30, 5);
        Turns(arena, 4);

        Assert.Single(Balls(arena));
    }

    [Fact]
    public void LongLob_PassesOverAnOpponentStandingOnTheWay()
    {
        var arena = new SnowWarArena(Open(40, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var opponent = AddHuman(arena, 2, 6, 5);
        ThrowAt(arena, thrower, 12, 5, SnowWarTrajectory.LongLob);
        Turns(arena, 12);

        Assert.Empty(Balls(arena));
        Assert.Equal(5, Human(arena, opponent).Hp);
    }

    [Fact]
    public void ThrowOutsideTheMap_IsBoundedToTheArenaRectangle()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        Send(arena, thrower, SnowWarInput.ThrowAtPosition, [-999999, 999999, 0]);
        Turns(arena, 1);

        var events = SubturnEvents(arena, arena.NextTurn, 0);
        Assert.Equal(2, events.Count);
        Assert.Equal(SnowWarGameEventType.HumanThrowsSnowballAtPosition, events[0].Type);
        Assert.Equal([thrower, 0, 9 * 3200, 0], events[0].Fields);
        Assert.Equal(SnowWarGameEventType.CreateSnowball, events[1].Type);
        Assert.Equal([thrower, 0, 9 * 3200, 0], events[1].Fields.Skip(1));
    }

    [Fact]
    public void Snowball_CrashesOutsideTheArenaWithoutTestingObjects()
    {
        var arena = new SnowWarArena(Open(6, 3), 1);
        var thrower = AddHuman(arena, 1, 1, 1);
        Send(arena, thrower, SnowWarInput.ThrowAtPosition, [5 * 3200, 3200, 0]);
        Turns(arena, 4);

        var ball = Assert.Single(Balls(arena));
        Assert.True(ball.X > 6 * 3200);

        Turns(arena, 6);
        Assert.Empty(Balls(arena));
    }
}
