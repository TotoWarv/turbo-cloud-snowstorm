using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarDepartureTests
{
    [Fact]
    public void LeavingBeforeTheFirstTurn_IsScheduledForTurnOne()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);

        Assert.True(arena.RemoveHuman(human));

        var leave = Assert.Single(SubturnEvents(arena, 1, 0));
        Assert.Equal(SnowWarGameEventType.HumanLeftGame, leave.Type);
        Assert.Equal([human], leave.Fields);
    }

    [Fact]
    public void LeavingLater_IsScheduledForTheFirstTurnNotYetBroadcast()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);
        Turns(arena, 5);

        Assert.True(arena.RemoveHuman(human));
        Assert.False(arena.RemoveHuman(human));

        Assert.Single(SubturnEvents(arena, 6, 0));
        Assert.Empty(AllEvents(arena, 7));
    }

    [Fact]
    public void LeavingHuman_DisappearsAndFreesHisTile()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var leaver = AddHuman(arena, 1, 3, 5);
        var walker = AddHuman(arena, 2, 2, 5);
        arena.RemoveHuman(leaver);
        Turns(arena, 2);

        Assert.False(Exists(arena, leaver));
        Assert.Null(arena.GetHumanStats(leaver));

        MoveTo(arena, walker, 4, 5);
        var tiles = new List<int>();

        for (var i = 0; i < 6; i++)
        {
            Turns(arena, 1);
            tiles.Add(Human(arena, walker).TileY);
        }

        Assert.All(tiles, y => Assert.Equal(5, y));
        Assert.Equal(4, Human(arena, walker).TileX);
    }

    [Fact]
    public void LeavingHuman_InputsAreIgnored()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);
        arena.RemoveHuman(human);
        MoveTo(arena, human, 4, 5);
        Turns(arena, 1);

        Assert.Equal([SnowWarGameEventType.HumanLeftGame], AllEvents(arena, 1).Select(e => e.Type));
    }

    [Fact]
    public void LeavingHuman_LosesEveryOtherActionOfHisLastTurn()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);
        Turns(arena, 1);
        arena.ScheduleInput(
            human,
            SnowWarInput.ThrowAtPosition,
            [Center(8), Center(5), 0],
            arena.NextTurn + 2,
            1
        );
        Turns(arena, 1);
        arena.RemoveHuman(human);

        Assert.Equal(
            [SnowWarGameEventType.HumanLeftGame],
            SubturnEvents(arena, 3, 0).Select(e => e.Type)
        );
        Assert.Equal(
            [
                SnowWarGameEventType.HumanThrowsSnowballAtPosition,
                SnowWarGameEventType.CreateSnowball,
            ],
            SubturnEvents(arena, 3, 1).Select(e => e.Type)
        );

        Turns(arena, 2);

        Assert.Empty(arena.GetFullStatus());
    }

    [Fact]
    public void SnowballOfALeavingThrower_FliesOnButHurtsNobody()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 10, 5);
        ThrowAtHuman(arena, thrower, victim);
        Turns(arena, 2);
        arena.RemoveHuman(thrower);
        Turns(arena, 1);
        Assert.Equal(thrower, Assert.Single(Balls(arena)).ThrowerId);

        Turns(arena, 1);
        Assert.Equal(0, Assert.Single(Balls(arena)).ThrowerId);

        Turns(arena, 1);
        Assert.Empty(Balls(arena));
        Assert.Equal(5, Human(arena, victim).Hp);
        Assert.Equal(0, arena.GetTeamScore(1));
        Assert.Equal(0, arena.GetHumanStats(victim)!.SnowballHitsTaken);
    }

    [Fact]
    public void PointsOfALeavingHuman_StayWithHisTeam()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 6, 5);
        ThrowAtHuman(arena, thrower, victim);
        Turns(arena, 5);
        arena.RemoveHuman(thrower);
        Turns(arena, 3);

        Assert.False(Exists(arena, thrower));
        Assert.Equal(1, arena.GetTeamScore(1));
    }
}
