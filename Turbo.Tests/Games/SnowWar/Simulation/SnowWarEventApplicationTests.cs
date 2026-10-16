using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarEventApplicationTests
{
    [Fact]
    public void CreateSnowball_WithAnIdAlreadyInUseIsSkipped()
    {
        var world = new SnowWarWorld(Open(20, 10));
        var human = world.AddHuman(Entry(1, 1), 2, 5);

        Run(
            world,
            0,
            Event(0, SnowWarGameEventType.CreateSnowball, 50, human.Id, Center(9), Center(5), 0),
            Event(0, SnowWarGameEventType.CreateSnowball, 50, human.Id, Center(2), Center(9), 0)
        );

        var ball = Assert.Single(world.Objects.OfType<SnowWarSnowball>());
        Assert.Equal(90, ball.Direction);
    }

    [Fact]
    public void EventsNamingMissingOrWrongObjects_AreDropped()
    {
        var world = new SnowWarWorld(Open(20, 10, Furni("snst_tree1", 8, 8)));
        var human = world.AddHuman(Entry(1, 1), 2, 5);
        var other = world.AddHuman(Entry(1, 2), 4, 5);

        Run(
            world,
            0,
            Event(0, SnowWarGameEventType.NewMoveTarget, 999, Center(9), Center(5)),
            Event(0, SnowWarGameEventType.NewMoveTarget, 1, Center(9), Center(5)),
            Event(0, SnowWarGameEventType.HumanThrowsSnowballAtHuman, human.Id, 1, 0),
            Event(0, SnowWarGameEventType.HumanGetsSnowballsFromMachine, human.Id, other.Id),
            Event(0, SnowWarGameEventType.MachineCreatesSnowball, 1),
            Event(0, SnowWarGameEventType.RayGunBurst, human.Id, 1, 70),
            Event(0, SnowWarGameEventType.NewMoveTarget, human.Id, Center(9))
        );

        Assert.Equal(5, human.Balls);
        Assert.Equal(5, other.Balls);
        Assert.Equal((Center(2), Center(5)), (human.TargetX, human.TargetY));
        Assert.Equal(3, world.Objects.Count);
    }

    [Fact]
    public void ThrowAtHuman_TurnsTheThrowerTowardTheTargetsCurrentPosition()
    {
        var world = new SnowWarWorld(Open(20, 10));
        var thrower = world.AddHuman(Entry(1, 1), 2, 2);
        var target = world.AddHuman(Entry(2, 2), 2, 8);

        Run(
            world,
            0,
            Event(0, SnowWarGameEventType.HumanThrowsSnowballAtHuman, thrower.Id, target.Id, 0)
        );

        Assert.Equal(4, thrower.BodyDirection);
        Assert.Equal(4, thrower.Balls);
        Assert.Equal(2, thrower.ThrowTimer);
    }

    [Fact]
    public void ThrowWithoutSnowballs_OnlyArmsTheThrowTimer()
    {
        var world = new SnowWarWorld(Open(20, 10));
        var thrower = world.AddHuman(Entry(1, 1), 2, 2);

        for (var i = 0; i < 5; i++)
        {
            thrower.ThrowAt(0, 0);
        }

        var before = thrower.BodyDirection;
        Run(
            world,
            0,
            Event(
                0,
                SnowWarGameEventType.HumanThrowsSnowballAtPosition,
                thrower.Id,
                Center(9),
                Center(2),
                0
            )
        );

        Assert.Equal(0, thrower.Balls);
        Assert.Equal(before, thrower.BodyDirection);
        Assert.Equal(2, thrower.ThrowTimer);
    }

    [Fact]
    public void MachineAndPickupEvents_RespectStockAndCapacity()
    {
        var world = new SnowWarWorld(
            Open(20, 10, Furni("s_snowball_machine", 5, 2), Furni("snst_ballpile", 9, 2))
        );
        var machine = (SnowWarMachine)world.Objects[0];
        var pile = (SnowWarPile)world.Objects[1];
        var human = world.AddHuman(Entry(1, 1), 5, 3);

        var refills = Enumerable
            .Range(0, 6)
            .Select(_ => Event(0, SnowWarGameEventType.MachineCreatesSnowball, machine.Id))
            .ToArray();
        Run(world, 0, refills);
        Assert.Equal(5, machine.Stock);

        Run(
            world,
            1,
            Event(0, SnowWarGameEventType.HumanGetsSnowballsFromMachine, human.Id, machine.Id)
        );
        Assert.Equal(5, machine.Stock);
        Assert.Equal(5, human.Balls);

        human.ThrowAt(0, 0);
        Run(
            world,
            2,
            Event(0, SnowWarGameEventType.HumanGetsSnowballsFromMachine, human.Id, pile.Id),
            Event(1, SnowWarGameEventType.HumanGetsSnowballsFromMachine, human.Id, machine.Id)
        );
        Assert.Equal(11, pile.Remaining);
        Assert.Equal(5, machine.Stock);
        Assert.Equal(5, human.Balls);
        Assert.Equal(1, human.Stats.SnowballsFromMachine);
    }

    [Fact]
    public void EventsOfOneSubturn_ApplyInListOrder()
    {
        var world = new SnowWarWorld(Open(20, 10));
        var first = world.AddHuman(Entry(1, 1), 2, 2);
        var second = world.AddHuman(Entry(1, 2), 2, 7);
        first.ThrowAt(0, 0);
        second.ThrowAt(0, 0);

        Run(
            world,
            0,
            Event(0, SnowWarGameEventType.NewMoveTarget, first.Id, Center(9), Center(2)),
            Event(0, SnowWarGameEventType.HumanStartsToMakeASnowball, first.Id),
            Event(0, SnowWarGameEventType.HumanStartsToMakeASnowball, second.Id),
            Event(0, SnowWarGameEventType.NewMoveTarget, second.Id, Center(9), Center(7))
        );

        Assert.Equal(SnowWarActivityState.MakingSnowball, first.ActivityState);
        Assert.Equal(Center(2), first.TargetX);
        Assert.Equal(SnowWarActivityState.Normal, second.ActivityState);
        Assert.Equal(Center(9), second.TargetX);
    }

    [Fact]
    public void LeaveEvent_CancelsEveryOtherEventOfTheLeaverInTheSameTurn()
    {
        var world = new SnowWarWorld(Open(20, 10));
        var human = world.AddHuman(Entry(1, 1), 2, 5);

        Run(
            world,
            0,
            Event(
                0,
                SnowWarGameEventType.HumanThrowsSnowballAtPosition,
                human.Id,
                Center(9),
                Center(5),
                0
            ),
            Event(0, SnowWarGameEventType.CreateSnowball, 20, human.Id, Center(9), Center(5), 0),
            Event(2, SnowWarGameEventType.HumanLeftGame, human.Id)
        );

        Assert.Empty(world.Objects);
    }

    [Fact]
    public void LeavingHuman_IsNotUpdatedAgainAndDoesNotHoldAnyTile()
    {
        var world = new SnowWarWorld(Open(20, 10));
        var human = world.AddHuman(Entry(1, 1), 2, 5);
        Run(world, 0, Event(0, SnowWarGameEventType.NewMoveTarget, human.Id, Center(6), Center(5)));
        var x = human.X;

        Run(world, 1, Event(0, SnowWarGameEventType.HumanLeftGame, human.Id));

        Assert.Equal(x, human.X);
        Assert.Null(world.Grid.Get(2, 5)!.Occupant);
        Assert.Null(world.Grid.Get(3, 5)!.Occupant);
    }

    [Fact]
    public void TurnRunner_ReturnsTheChecksumOfTheFinalState()
    {
        var world = new SnowWarWorld(Open(20, 10, Furni("snst_tree1", 8, 8)));
        world.AddHuman(Entry(1, 1), 2, 5);

        var checksum = Run(world, 4);

        Assert.Equal(SnowWarChecksum.Compute(4, world.Snapshot()), checksum);
    }

    private static SnowWarScheduledEvent Event(
        int subturn,
        SnowWarGameEventType type,
        params int[] fields
    ) => new(0, subturn, type, [.. fields]);

    private static int Run(SnowWarWorld world, int turn, params SnowWarScheduledEvent[] events) =>
        SnowWarTurnRunner.Run(world, turn, [.. events.Select(e => e with { Turn = turn })]);
}
