using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarMakeSnowballTests
{
    [Fact]
    public void MakeSnowball_WithFullHandsIsRejected()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);
        Send(arena, human, SnowWarInput.MakeSnowball, []);
        Turns(arena, 1);

        Assert.Empty(AllEvents(arena, arena.NextTurn));
    }

    [Fact]
    public void MakeSnowball_TakesTwentySubturnsAndKeepsTheHumanStill()
    {
        var arena = ArenaWithOneThrownBall(out var human);
        Send(arena, human, SnowWarInput.MakeSnowball, []);
        Turns(arena, 1);

        var start = Assert.Single(AllEvents(arena, arena.NextTurn));
        Assert.Equal(SnowWarGameEventType.HumanStartsToMakeASnowball, start.Type);
        Assert.Equal([human], start.Fields);

        Turns(arena, 1);
        var making = Human(arena, human);
        Assert.Equal((int)SnowWarActivityState.MakingSnowball, making.ActivityState);
        Assert.Equal(17, making.ActivityTimer);
        Assert.Equal(4, making.Balls);

        Turns(arena, 5);
        Assert.Equal(2, Human(arena, human).ActivityTimer);
        Assert.Equal(4, Human(arena, human).Balls);

        Turns(arena, 1);
        var done = Human(arena, human);
        Assert.Equal((int)SnowWarActivityState.Normal, done.ActivityState);
        Assert.Equal(0, done.ActivityTimer);
        Assert.Equal(5, done.Balls);
        Assert.Equal(1, arena.GetHumanStats(human)!.SnowballsCreated);
    }

    [Fact]
    public void MoveTarget_CancelsTheSnowballBeingMade()
    {
        var arena = ArenaWithOneThrownBall(out var human);
        Send(arena, human, SnowWarInput.MakeSnowball, []);
        Turns(arena, 2);
        MoveTo(arena, human, 4, 5);
        Turns(arena, 2);

        var view = Human(arena, human);
        Assert.Equal((int)SnowWarActivityState.Normal, view.ActivityState);
        Assert.Equal(0, view.ActivityTimer);
        Assert.Equal(4, view.Balls);
        Assert.Equal((3, 5), (view.NextX, view.NextY));

        Turns(arena, 8);
        Assert.Equal(4, Human(arena, human).Balls);
    }

    [Fact]
    public void MakeSnowball_StopsAWalkingHumanOnTheTileHeWasEntering()
    {
        var arena = ArenaWithOneThrownBall(out var human);
        MoveTo(arena, human, 6, 5);
        Turns(arena, 1);
        Send(arena, human, SnowWarInput.MakeSnowball, []);
        Turns(arena, 1);
        Assert.Equal(8002, Human(arena, human).X);

        Turns(arena, 1);
        var view = Human(arena, human);
        Assert.Equal((Center(3), Center(5)), (view.X, view.Y));
        Assert.Equal((3, 5), (view.TileX, view.TileY));
        Assert.Equal((Center(3), Center(5)), (view.TargetX, view.TargetY));
        Assert.Equal((int)SnowWarActivityState.MakingSnowball, view.ActivityState);

        Turns(arena, 9);
        Assert.Equal((3, 5), (Human(arena, human).TileX, Human(arena, human).TileY));
    }

    [Fact]
    public void FiftyMakeSnowballsInARow_ProduceASingleEvent()
    {
        var arena = ArenaWithOneThrownBall(out var human);

        for (var i = 0; i < 50; i++)
        {
            Send(arena, human, SnowWarInput.MakeSnowball, []);
        }

        Turns(arena, 1);

        var start = Assert.Single(AllEvents(arena, arena.NextTurn));
        Assert.Equal(SnowWarGameEventType.HumanStartsToMakeASnowball, start.Type);

        for (var i = 0; i < 50; i++)
        {
            Send(arena, human, SnowWarInput.MakeSnowball, []);
        }

        Turns(arena, 1);
        Assert.Empty(AllEvents(arena, arena.NextTurn));

        Turns(arena, 10);
        Assert.Equal(5, Human(arena, human).Balls);
    }

    [Fact]
    public void MakeSnowball_DuringInvincibilityEndsIt()
    {
        var world = new SnowWarWorld(Open(10, 10));
        var human = world.AddHuman(Entry(1, 1), 5, 5);
        human.ThrowAt(0, 0);
        human.KnockDown(90);

        for (var i = 0; i < 100; i++)
        {
            human.Update(world);
        }

        Assert.Equal(SnowWarActivityState.Invincible, human.ActivityState);

        human.StartMakingSnowball();
        Assert.Equal(SnowWarActivityState.MakingSnowball, human.ActivityState);

        for (var i = 0; i < 20; i++)
        {
            human.Update(world);
        }

        Assert.Equal(SnowWarActivityState.Normal, human.ActivityState);
        Assert.Equal(5, human.Balls);
    }

    private static SnowWarArena ArenaWithOneThrownBall(out int human)
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        human = AddHuman(arena, 1, 2, 5);
        Send(arena, human, SnowWarInput.ThrowAtPosition, [Center(2), Center(0), 0]);
        Turns(arena, 3);
        Assert.Equal(4, Human(arena, human).Balls);
        return arena;
    }
}
