using System;
using System.Linq;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

public sealed partial class SnowWarArena
{
    private void ScheduleSourceDecisions(int turn)
    {
        for (var subturn = 0; subturn < SnowWarConstants.SUBTURNS_PER_TURN; subturn++)
        {
            foreach (var machine in World.Objects.OfType<SnowWarMachine>())
            {
                DecideRefill(machine, turn, subturn);
            }

            foreach (var human in World.Objects.OfType<SnowWarHuman>())
            {
                DecidePickup(human, turn, subturn);
            }
        }
    }

    private void DecideRefill(SnowWarMachine machine, int turn, int subturn)
    {
        if (machine.RefillTimer < 0)
        {
            machine.RefillTimer = SnowWarConstants.MACHINE_REFILL_SUBTURNS;
        }

        if (machine.RefillTimer > 0)
        {
            machine.RefillTimer--;
            return;
        }

        machine.RefillTimer = SnowWarConstants.MACHINE_REFILL_SUBTURNS;
        var planned = CountPendingEvents(
            SnowWarGameEventType.MachineCreatesSnowball,
            0,
            machine.Id
        );

        if (machine.Stock + planned < machine.Max)
        {
            Schedule(turn, subturn, SnowWarGameEventType.MachineCreatesSnowball, machine.Id);
        }
    }

    private void DecidePickup(SnowWarHuman human, int turn, int subturn)
    {
        if (!human.IsActive || IsLeaving(human.Id))
        {
            return;
        }

        if (human.PickupTimer > 0)
        {
            human.PickupTimer--;
        }

        if (
            human.PickupTimer != 0
            || human.NextTile is not null
            || human.IsWalking
            || !human.CanWalk
        )
        {
            return;
        }

        var planned = CountPendingEvents(
            SnowWarGameEventType.HumanGetsSnowballsFromMachine,
            0,
            human.Id
        );

        if (human.Balls + planned >= SnowWarConstants.MAX_SNOWBALLS)
        {
            return;
        }

        var source = FindMachineFor(human) ?? FindPileFor(human);

        if (source is null)
        {
            return;
        }

        Schedule(
            turn,
            subturn,
            SnowWarGameEventType.HumanGetsSnowballsFromMachine,
            human.Id,
            source.Id
        );
        human.PickupTimer = SnowWarConstants.PICKUP_INTERVAL;
    }

    private SnowWarGameObject? FindMachineFor(SnowWarHuman human)
    {
        foreach (var machine in World.Objects.OfType<SnowWarMachine>())
        {
            if (
                machine.WithdrawTileX == human.Tile.X
                && machine.WithdrawTileY == human.Tile.Y
                && machine.Stock > PlannedPickupsFrom(machine)
            )
            {
                return machine;
            }
        }

        return null;
    }

    private SnowWarGameObject? FindPileFor(SnowWarHuman human)
    {
        foreach (var pile in World.Objects.OfType<SnowWarPile>())
        {
            var distance =
                Math.Abs(pile.Tile.X - human.Tile.X) + Math.Abs(pile.Tile.Y - human.Tile.Y);

            if (distance == 1 && pile.Remaining > PlannedPickupsFrom(pile))
            {
                return pile;
            }
        }

        return null;
    }

    private int PlannedPickupsFrom(SnowWarGameObject source) =>
        CountPendingEvents(SnowWarGameEventType.HumanGetsSnowballsFromMachine, 1, source.Id);

    private void ScheduleRayGunBursts(int turn)
    {
        foreach (var human in World.Objects.OfType<SnowWarHuman>())
        {
            if (!human.IsActive || IsLeaving(human.Id))
            {
                continue;
            }

            var gun = World.RayGuns.FirstOrDefault(candidate =>
                candidate.UseTileX == human.Tile.X && candidate.UseTileY == human.Tile.Y
            );

            if (gun is null || !human.HasArrivedOn(human.Tile))
            {
                human.ConsumedRayGun = null;
                continue;
            }

            if (!human.CanWalk || ReferenceEquals(human.ConsumedRayGun, gun))
            {
                continue;
            }

            human.ConsumedRayGun = gun;

            if (!gun.IsReadyFor(turn))
            {
                continue;
            }

            gun.LastBurstTurn = turn;
            var firstSnowballId = World.AllocateObjectId();

            for (var i = 1; i < SnowWarConstants.RAY_GUN_SNOWBALLS; i++)
            {
                World.AllocateObjectId();
            }

            Schedule(
                turn,
                0,
                SnowWarGameEventType.RayGunBurst,
                human.Id,
                gun.FuseId,
                firstSnowballId
            );
        }
    }
}
