using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal static class SnowWarEventResolver
{
    public static List<SnowWarResolvedEvent> Resolve(
        SnowWarWorld world,
        IReadOnlyList<SnowWarScheduledEvent> events
    )
    {
        var leaving = new List<int>();

        foreach (var scheduled in events)
        {
            if (
                scheduled.Type == SnowWarGameEventType.HumanLeftGame
                && HasShape(scheduled)
                && world.FindActive<SnowWarHuman>(scheduled.Fields[0]) is not null
            )
            {
                leaving.Add(scheduled.Fields[0]);
            }
        }

        var resolved = new List<SnowWarResolvedEvent>();

        foreach (var scheduled in events)
        {
            if (HasShape(scheduled) && TryResolve(world, scheduled, leaving) is { } resolvedEvent)
            {
                resolved.Add(resolvedEvent);
            }
        }

        return resolved;
    }

    public static int FieldCount(SnowWarGameEventType type) =>
        type switch
        {
            SnowWarGameEventType.HumanLeftGame => 1,
            SnowWarGameEventType.NewMoveTarget => 3,
            SnowWarGameEventType.HumanThrowsSnowballAtHuman => 3,
            SnowWarGameEventType.HumanThrowsSnowballAtPosition => 4,
            SnowWarGameEventType.HumanStartsToMakeASnowball => 1,
            SnowWarGameEventType.CreateSnowball => 5,
            SnowWarGameEventType.MachineCreatesSnowball => 1,
            SnowWarGameEventType.HumanGetsSnowballsFromMachine => 2,
            SnowWarGameEventType.RayGunBurst => 3,
            _ => -1,
        };

    private static bool HasShape(SnowWarScheduledEvent scheduled) =>
        scheduled.Subturn is >= 0 and < SnowWarConstants.SUBTURNS_PER_TURN
        && scheduled.Fields.Length == FieldCount(scheduled.Type);

    private static SnowWarResolvedEvent? TryResolve(
        SnowWarWorld world,
        SnowWarScheduledEvent scheduled,
        List<int> leaving
    )
    {
        var fields = scheduled.Fields;

        if (scheduled.Type == SnowWarGameEventType.MachineCreatesSnowball)
        {
            return world.FindActive<SnowWarMachine>(fields[0]) is { } machine
                ? new(scheduled, null, machine)
                : null;
        }

        var actorField = scheduled.Type == SnowWarGameEventType.CreateSnowball ? 1 : 0;
        var human = world.FindActive<SnowWarHuman>(fields[actorField]);

        if (human is null)
        {
            return null;
        }

        if (scheduled.Type == SnowWarGameEventType.HumanLeftGame)
        {
            return new(scheduled, human);
        }

        if (leaving.Contains(human.Id))
        {
            return null;
        }

        return scheduled.Type switch
        {
            SnowWarGameEventType.NewMoveTarget => new(scheduled, human),
            SnowWarGameEventType.HumanStartsToMakeASnowball => new(scheduled, human),
            SnowWarGameEventType.HumanThrowsSnowballAtPosition => IsTrajectory(fields[3])
                ? new(scheduled, human)
                : null,
            SnowWarGameEventType.CreateSnowball => IsTrajectory(fields[4])
                ? new(scheduled, human)
                : null,
            SnowWarGameEventType.HumanThrowsSnowballAtHuman => world.FindActive<SnowWarHuman>(
                fields[1]
            )
                is { } target
                ? new(scheduled, human, target)
                : null,
            SnowWarGameEventType.HumanGetsSnowballsFromMachine =>
                world.FindActive<SnowWarGameObject>(fields[1])
                    is ISnowWarSnowballSource
                        and SnowWarGameObject source
                    ? new(scheduled, human, source)
                    : null,
            SnowWarGameEventType.RayGunBurst => world.FindRayGun(fields[1]) is { } gun
                ? new(scheduled, human, RayGun: gun)
                : null,
            _ => null,
        };
    }

    private static bool IsTrajectory(int value) =>
        value is >= (int)SnowWarTrajectory.Quick and <= (int)SnowWarTrajectory.Default;
}
