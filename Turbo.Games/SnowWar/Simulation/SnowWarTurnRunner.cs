using System.Collections.Generic;

namespace Turbo.Games.SnowWar.Simulation;

internal static class SnowWarTurnRunner
{
    public static int Run(SnowWarWorld world, int turn, IReadOnlyList<SnowWarScheduledEvent> events)
    {
        var resolved = SnowWarEventResolver.Resolve(world, events);

        for (var subturn = 0; subturn < SnowWarConstants.SUBTURNS_PER_TURN; subturn++)
        {
            foreach (var resolvedEvent in resolved)
            {
                if (resolvedEvent.Event.Subturn == subturn)
                {
                    SnowWarEventApplier.Apply(world, resolvedEvent);
                }
            }

            world.UpdateObjects();
            world.RemoveInactive();
        }

        return world.ComputeChecksum(turn);
    }
}
