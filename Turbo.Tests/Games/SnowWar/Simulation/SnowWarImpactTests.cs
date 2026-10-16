using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarImpactTests
{
    [Fact]
    public void Hit_CostsOneLifeAndGivesOnePointToTheThrowerAndHisTeam()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 6, 5);
        ThrowAtHuman(arena, thrower, victim);
        Turns(arena, 5);

        Assert.Empty(Balls(arena));
        Assert.Equal(4, Human(arena, victim).Hp);
        Assert.Equal(1, Human(arena, thrower).Score);
        Assert.Equal(4, Human(arena, thrower).Balls);
        Assert.Equal(1, arena.GetTeamScore(1));
        Assert.Equal(0, arena.GetTeamScore(2));

        var throwerStats = arena.GetHumanStats(thrower)!;
        Assert.Equal(1, throwerStats.SnowballHits);
        Assert.Equal(1, throwerStats.SnowballsThrown);
        Assert.Equal(1, arena.GetHumanStats(victim)!.SnowballHitsTaken);
    }

    [Fact]
    public void FiveHits_KnockTheVictimDownForTenPoints()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 6, 5);

        KnockDown(arena, thrower, victim);

        var view = Human(arena, victim);
        Assert.Equal((int)SnowWarActivityState.Stunned, view.ActivityState);
        Assert.Equal(0, view.Hp);
        Assert.Equal(6, view.BodyDirection);
        Assert.Equal(10, Human(arena, thrower).Score);
        Assert.Equal(10, arena.GetTeamScore(1));
        Assert.Equal(1, arena.GetHumanStats(thrower)!.Kills);
        Assert.Equal(5, arena.GetHumanStats(thrower)!.SnowballHits);
        Assert.Equal(1, arena.GetHumanStats(victim)!.Deaths);
        Assert.Equal(0, arena.GetHumanStats(thrower)!.FriendlyKills);
    }

    [Fact]
    public void KnockedDownHuman_IsStunnedForOneHundredSubturnsThenInvincibleForFiftyNine()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 6, 5);
        KnockDown(arena, thrower, victim);

        var timer = Human(arena, victim).ActivityTimer;
        Assert.InRange(timer, 97, 100);

        for (var elapsed = 3; elapsed <= 180; elapsed += 3)
        {
            Turns(arena, 1);
            var view = Human(arena, victim);

            if (elapsed < timer)
            {
                Assert.Equal((int)SnowWarActivityState.Stunned, view.ActivityState);
                Assert.Equal(timer - elapsed, view.ActivityTimer);
            }
            else if (elapsed < timer + 59)
            {
                Assert.Equal((int)SnowWarActivityState.Invincible, view.ActivityState);
                Assert.Equal(59 - (elapsed - timer), view.ActivityTimer);
                Assert.Equal(5, view.Hp);
            }
            else
            {
                Assert.Equal((int)SnowWarActivityState.Normal, view.ActivityState);
                Assert.Equal(0, view.ActivityTimer);
            }
        }
    }

    [Fact]
    public void StunnedHuman_IgnoresMoveTargetsAndCannotThrow()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 6, 5);
        KnockDown(arena, thrower, victim);

        ThrowAt(arena, victim, 2, 2);
        MoveTo(arena, victim, 9, 5);
        Turns(arena, 1);

        var events = AllEvents(arena, arena.NextTurn);
        var move = Assert.Single(events);
        Assert.Equal(SnowWarGameEventType.NewMoveTarget, move.Type);

        Turns(arena, 3);

        var view = Human(arena, victim);
        Assert.Equal((6, 5), (view.TileX, view.TileY));
        Assert.Equal((Center(6), Center(5)), (view.TargetX, view.TargetY));
    }

    [Fact]
    public void StunnedAndInvincibleHumans_AreNotTouchable()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var victim = AddHuman(arena, 2, 6, 5);
        var second = AddHuman(arena, 1, 2, 4);
        KnockDown(arena, thrower, victim);

        ThrowAtHuman(arena, second, victim);
        Turns(arena, 6);

        Assert.Equal(4, Human(arena, second).Balls);
        Assert.Equal(0, Human(arena, second).Score);
        Assert.Equal(5, arena.GetHumanStats(victim)!.SnowballHitsTaken);
    }

    [Fact]
    public void FriendlyHit_DoesNoDamageAndOnlyCountsAFriendlyHit()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        var mate = AddHuman(arena, 1, 6, 5);
        ThrowAt(arena, thrower, 6, 5);
        Turns(arena, 5);

        Assert.Empty(Balls(arena));
        Assert.Equal(5, Human(arena, mate).Hp);
        Assert.Equal(0, Human(arena, thrower).Score);
        Assert.Equal(0, arena.GetTeamScore(1));
        Assert.Equal(1, arena.GetHumanStats(thrower)!.FriendlyHits);
        Assert.Equal(0, arena.GetHumanStats(mate)!.SnowballHitsTaken);
    }

    [Fact]
    public void Thrower_IsNeverHitByHisOwnSnowball()
    {
        var arena = new SnowWarArena(Open(20, 10), 1);
        var thrower = AddHuman(arena, 1, 2, 5);
        Send(arena, thrower, SnowWarInput.ThrowAtPosition, [Center(2), Center(5), 0]);
        Turns(arena, 10);

        Assert.Equal(5, Human(arena, thrower).Hp);
        Assert.Empty(Balls(arena));
    }

    [Fact]
    public void Tree_StopsThreeSnowballsThenLetsThemThrough()
    {
        var arena = new SnowWarArena(Open(30, 10, Furni("snst_tree1", 6, 5)), 1);
        var tree = 1;
        var thrower = AddHuman(arena, 1, 2, 5);

        for (var hit = 1; hit <= 3; hit++)
        {
            ThrowAt(arena, thrower, 20, 5);
            Turns(arena, 5);
            Assert.Empty(Balls(arena));
            Assert.Equal(hit, Object(arena, tree).Variables[6]);
        }

        ThrowAt(arena, thrower, 20, 5);
        Turns(arena, 5);

        Assert.Single(Balls(arena));
        Assert.Equal(3, Object(arena, tree).Variables[6]);
        Assert.Equal(0, Human(arena, thrower).Score);
    }

    [Fact]
    public void FelledTree_StillBlocksWalking()
    {
        var arena = new SnowWarArena(Open(30, 10, Furni("snst_tree1", 6, 5)), 1);
        var thrower = AddHuman(arena, 1, 2, 5);

        for (var hit = 0; hit < 3; hit++)
        {
            ThrowAt(arena, thrower, 20, 5);
            Turns(arena, 5);
        }

        MoveTo(arena, thrower, 6, 5);
        Turns(arena, 16);

        Assert.Equal((5, 5), (Human(arena, thrower).TileX, Human(arena, thrower).TileY));
    }

    [Fact]
    public void FullPile_StopsOnlyLowSnowballs()
    {
        var world = new SnowWarWorld(Open(10, 10, Furni("snst_ballpile", 5, 5)));
        var pile = (SnowWarPile)world.Objects[0];

        Assert.True(pile.StopsSnowball(Ball(16000, 16000 - 1500, 1199)));
        Assert.False(pile.StopsSnowball(Ball(16000, 16000 - 1500, 1200)));
        Assert.False(pile.StopsSnowball(Ball(16000, 16000 - 1600, 100)));
    }

    [Fact]
    public void Machine_StopsSnowballsUnderItsHeight()
    {
        var world = new SnowWarWorld(Open(10, 10, Furni("s_snowball_machine", 5, 5)));
        var machine = (SnowWarMachine)world.Objects[0];

        Assert.True(machine.StopsSnowball(Ball(16000 + 1599, 16000, 1199)));
        Assert.False(machine.StopsSnowball(Ball(16000 + 1600, 16000, 1199)));
        Assert.False(machine.StopsSnowball(Ball(16000, 16000, 1200)));
    }

    [Fact]
    public void Human_StopsSnowballsUnderFiveThousandWithinTwoThousand()
    {
        var world = new SnowWarWorld(Open(10, 10));
        var human = world.AddHuman(Entry(1, 1), 5, 5);

        Assert.True(human.StopsSnowball(Ball(16000 + 1999, 16000, 4999)));
        Assert.False(human.StopsSnowball(Ball(16000 + 2000, 16000, 4999)));
        Assert.False(human.StopsSnowball(Ball(16000, 16000, 5000)));
        Assert.False(human.StopsSnowball(Ball(16000, 16000, 100, human.Id)));
    }

    [Fact]
    public void Human_IsNotTouchableWhileStunnedOrInvincible()
    {
        var world = new SnowWarWorld(Open(10, 10));
        var human = world.AddHuman(Entry(1, 1), 5, 5);
        human.KnockDown(90);

        Assert.False(human.StopsSnowball(Ball(16000, 16000, 100)));

        for (var i = 0; i < 100; i++)
        {
            human.Update(world);
        }

        Assert.Equal(SnowWarActivityState.Invincible, human.ActivityState);
        Assert.False(human.StopsSnowball(Ball(16000, 16000, 100)));
    }

    [Fact]
    public void SnowballWithoutThrower_DisappearsOnAHumanWithoutEffect()
    {
        var world = new SnowWarWorld(Open(10, 10));
        var human = world.AddHuman(Entry(1, 1), 5, 5);
        var ball = Ball(16000, 16000, 100, 0);

        Assert.True(human.StopsSnowball(ball));
        human.OnSnowballImpact(world, ball);

        Assert.Equal(5, human.Hp);
        Assert.Equal(0, world.GetTeamScore(1));
        Assert.Equal(0, world.GetTeamScore(2));
    }

    private static SnowWarSnowball Ball(int x, int y, int z, int throwerId = 999) =>
        new(500, x, y, z, 90, SnowWarTrajectory.Quick, 0, throwerId, 5, 2000);

    internal static void KnockDown(SnowWarArena arena, int thrower, int victim)
    {
        for (var i = 0; i < 5; i++)
        {
            ThrowAtHuman(arena, thrower, victim);
            Turns(arena, 4);
        }

        Assert.Equal((int)SnowWarActivityState.Stunned, Human(arena, victim).ActivityState);
    }
}
