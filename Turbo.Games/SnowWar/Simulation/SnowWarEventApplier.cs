using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal static class SnowWarEventApplier
{
    public static void Apply(SnowWarWorld world, SnowWarResolvedEvent resolved)
    {
        var fields = resolved.Event.Fields;
        var human = resolved.Human!;

        switch (resolved.Event.Type)
        {
            case SnowWarGameEventType.HumanLeftGame:
                human.Leave(world);
                break;
            case SnowWarGameEventType.NewMoveTarget:
                human.SetMoveTarget(fields[1], fields[2]);
                break;
            case SnowWarGameEventType.HumanThrowsSnowballAtHuman:
                var target = (SnowWarHuman)resolved.Other!;
                human.ThrowAt(target.X, target.Y);
                break;
            case SnowWarGameEventType.HumanThrowsSnowballAtPosition:
                human.ThrowAt(fields[1], fields[2]);
                break;
            case SnowWarGameEventType.HumanStartsToMakeASnowball:
                human.StartMakingSnowball();
                break;
            case SnowWarGameEventType.CreateSnowball:
                CreateSnowball(world, human, fields[0], fields[2], fields[3], fields[4]);
                break;
            case SnowWarGameEventType.MachineCreatesSnowball:
                ((SnowWarMachine)resolved.Other!).AddSnowball();
                break;
            case SnowWarGameEventType.HumanGetsSnowballsFromMachine:
                human.ReceiveSnowball((ISnowWarSnowballSource)resolved.Other!);
                break;
            case SnowWarGameEventType.RayGunBurst:
                human.FireRayGun(world, resolved.RayGun!, fields[2]);
                break;
        }
    }

    private static void CreateSnowball(
        SnowWarWorld world,
        SnowWarHuman thrower,
        int id,
        int targetX,
        int targetY,
        int trajectory
    )
    {
        if (world.Contains(id))
        {
            return;
        }

        world.Add(
            SnowWarSnowball.Create(
                id,
                thrower.Id,
                thrower.X,
                thrower.Y,
                targetX,
                targetY,
                (SnowWarTrajectory)trajectory
            )
        );
    }
}
