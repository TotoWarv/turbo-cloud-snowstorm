using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarRayGunTests
{
    [Theory]
    [InlineData(2, 9, 10)]
    [InlineData(6, 12, 10)]
    [InlineData(4, 10, 9)]
    [InlineData(0, 10, 12)]
    public void UseTile_IsBehindTheGun(int direction, int x, int y)
    {
        var world = new SnowWarWorld(Open(20, 20, Furni("ads_igorraygun", 10, 10, direction)));

        var gun = Assert.Single(world.RayGuns);
        Assert.Equal((x, y), (gun.UseTileX, gun.UseTileY));
    }

    [Fact]
    public void DiagonalGun_IsOnlyAnObstacle() =>
        Assert.Empty(new SnowWarWorld(Open(20, 20, Furni("ads_igorraygun", 10, 10, 1))).RayGuns);

    [Fact]
    public void ArrivingOnTheUseTile_FiresSevenFreeSnowballsOnce()
    {
        var arena = new SnowWarArena(Open(40, 20, Furni("ads_igorraygun", 10, 10, 2)), 1);
        var human = AddHuman(arena, 1, 7, 10);
        MoveTo(arena, human, 9, 10);

        var bursts = RunAndCollectBursts(arena, 6);
        var burst = Assert.Single(bursts);
        Assert.Equal(0, burst.Subturn);
        Assert.Equal(human, burst.Fields[0]);
        Assert.Equal(1, burst.Fields[1]);

        Turns(arena, 1);

        var balls = Balls(arena);
        Assert.Equal(7, balls.Count);
        Assert.Equal(Enumerable.Range(burst.Fields[2], 7), balls.Select(ball => ball.Id));
        Assert.All(balls, ball => Assert.Equal(human, ball.ThrowerId));
        Assert.Equal(90, balls[0].Direction);
        Assert.Equal(5, Human(arena, human).Balls);
        Assert.Equal(2, Human(arena, human).BodyDirection);
        Assert.Equal(0, arena.GetHumanStats(human)!.SnowballsThrown);

        Assert.Empty(RunAndCollectBursts(arena, 30));
    }

    [Fact]
    public void ComingBackDuringTheRecharge_ConsumesTheArrival()
    {
        var arena = new SnowWarArena(Open(40, 20, Furni("ads_igorraygun", 10, 10, 2)), 1);
        var human = AddHuman(arena, 1, 9, 10);
        Assert.Single(RunAndCollectBursts(arena, 2));

        MoveTo(arena, human, 8, 10);
        Turns(arena, 3);
        MoveTo(arena, human, 9, 10);
        Assert.Empty(RunAndCollectBursts(arena, 30));

        MoveTo(arena, human, 8, 10);
        Turns(arena, 3);
        MoveTo(arena, human, 9, 10);
        Assert.Single(RunAndCollectBursts(arena, 6));
    }

    [Fact]
    public void WalkingThroughTheUseTile_FiresNothing()
    {
        var arena = new SnowWarArena(Open(40, 20, Furni("ads_igorraygun", 10, 10, 2)), 1);
        var human = AddHuman(arena, 1, 9, 13);
        MoveTo(arena, human, 9, 7);

        Assert.Empty(RunAndCollectBursts(arena, 16));
        Assert.Equal(7, Human(arena, human).TileY);
    }

    [Fact]
    public void MakingASnowballOnTheUseTile_DelaysTheBurstUntilItIsMade()
    {
        var arena = new SnowWarArena(Open(40, 20, Furni("ads_igorraygun", 10, 10, 2)), 1);
        var human = AddHuman(arena, 1, 8, 10);
        ThrowAt(arena, human, 8, 19);
        Turns(arena, 3);

        MoveTo(arena, human, 9, 10);
        arena.ScheduleInput(human, SnowWarInput.MakeSnowball, [], arena.NextTurn + 2, 2);

        var firstMakingTurns = RunAndCollectBursts(arena, 7);
        Assert.Empty(firstMakingTurns);
        Assert.Equal((int)SnowWarActivityState.MakingSnowball, Human(arena, human).ActivityState);

        Assert.Single(RunAndCollectBursts(arena, 3));
        Assert.Equal(5, Human(arena, human).Balls);
    }

    [Fact]
    public void DisabledRayGuns_NeverFire()
    {
        var arena = new SnowWarArena(
            Open(40, 20, Furni("ads_igorraygun", 10, 10, 2)),
            1,
            rayGunsEnabled: false
        );
        AddHuman(arena, 1, 9, 10);

        Assert.Empty(RunAndCollectBursts(arena, 10));
    }

    [Fact]
    public void HumanAboutToLeave_DoesNotFire()
    {
        var arena = new SnowWarArena(Open(40, 20, Furni("ads_igorraygun", 10, 10, 2)), 1);
        var human = AddHuman(arena, 1, 9, 10);
        arena.RemoveHuman(human);

        Assert.Empty(RunAndCollectBursts(arena, 4));
    }

    private static List<SnowWarBurst> RunAndCollectBursts(SnowWarArena arena, int turns)
    {
        var bursts = new List<SnowWarBurst>();

        for (var i = 0; i < turns; i++)
        {
            var result = arena.RunTurn();
            var subturns = arena.GetScheduledEvents(result.Turn + 1);

            for (var subturn = 0; subturn < 3; subturn++)
            {
                bursts.AddRange(
                    subturns[subturn]
                        .Where(scheduled => scheduled.Type == SnowWarGameEventType.RayGunBurst)
                        .Select(scheduled => new SnowWarBurst(subturn, [.. scheduled.Fields]))
                );
            }
        }

        return bursts;
    }
}
