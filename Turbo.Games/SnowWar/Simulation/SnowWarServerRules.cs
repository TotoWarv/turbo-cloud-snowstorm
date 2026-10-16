using System;
using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

public static class SnowWarServerRules
{
    public static bool IsWellFormed(SnowWarInput input, IReadOnlyList<int> values, int subturn)
    {
        ArgumentNullException.ThrowIfNull(values);

        return subturn is >= 0 and < SnowWarConstants.SUBTURNS_PER_TURN
            && values.Count == ValueCount(input);
    }

    public static bool Accepts(
        SnowWarArena arena,
        int humanId,
        SnowWarInput input,
        IReadOnlyList<int> values
    )
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count != ValueCount(input))
        {
            return false;
        }

        var human = arena.World.FindActive<SnowWarHuman>(humanId);

        if (human is null || arena.IsLeaving(humanId))
        {
            return false;
        }

        var grid = arena.World.Grid;

        return input switch
        {
            SnowWarInput.MoveTarget => grid.AtWorld(grid.ClampX(values[0]), grid.ClampY(values[1]))
                is not null,
            SnowWarInput.MakeSnowball => human.CanMakeSnowball
                && !arena.HasPendingEvent(
                    SnowWarGameEventType.HumanStartsToMakeASnowball,
                    0,
                    humanId
                ),
            SnowWarInput.ThrowAtPosition => CanThrow(arena, human, values[2]),
            SnowWarInput.ThrowAtHuman => CanThrow(arena, human, values[1])
                && arena.World.FindActive<SnowWarHuman>(values[0]) is { } target
                && target.Team != human.Team,
            _ => false,
        };
    }

    private static bool CanThrow(SnowWarArena arena, SnowWarHuman human, int trajectory) =>
        human.CanThrow
        && trajectory is >= (int)SnowWarTrajectory.Quick and <= (int)SnowWarTrajectory.Default
        && !arena.HasPendingEvent(SnowWarGameEventType.CreateSnowball, 1, human.Id);

    private static int ValueCount(SnowWarInput input) =>
        input switch
        {
            SnowWarInput.MoveTarget => 2,
            SnowWarInput.MakeSnowball => 0,
            SnowWarInput.ThrowAtPosition => 3,
            SnowWarInput.ThrowAtHuman => 2,
            _ => -1,
        };
}
