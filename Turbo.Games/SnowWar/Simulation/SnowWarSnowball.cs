using System;
using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarSnowball(
    int id,
    int x,
    int y,
    int z,
    int direction,
    SnowWarTrajectory trajectory,
    int life,
    int throwerId,
    int parabolaOffset,
    int speed
) : SnowWarGameObject(id)
{
    public override SnowWarGameObjectType Type => SnowWarGameObjectType.Snowball;

    public int X { get; private set; } = x;

    public int Y { get; private set; } = y;

    public int Z { get; private set; } = z;

    public int Direction { get; } = direction;

    public SnowWarTrajectory Trajectory { get; } = trajectory;

    public int Life { get; private set; } = life;

    public int ThrowerId { get; private set; } = throwerId;

    public int ParabolaOffset { get; } = parabolaOffset;

    public int Speed { get; } = speed;

    private int HeightFactor =>
        Trajectory switch
        {
            SnowWarTrajectory.Quick => SnowWarConstants.QUICK_HEIGHT_FACTOR,
            SnowWarTrajectory.ShortLob => SnowWarConstants.SHORT_LOB_HEIGHT_FACTOR,
            _ => SnowWarConstants.LONG_LOB_HEIGHT_FACTOR,
        };

    public static SnowWarSnowball Create(
        int id,
        int throwerId,
        int x,
        int y,
        int targetX,
        int targetY,
        SnowWarTrajectory requested
    )
    {
        var dx = (targetX - x) / SnowWarConstants.THROW_DISTANCE_STEP;
        var dy = (targetY - y) / SnowWarConstants.THROW_DISTANCE_STEP;
        var direction = SnowWarMath.NormalizeDirection(SnowWarMath.Angle(dx, dy));
        var distance =
            SnowWarMath.FastSqrt((dx * dx) + (dy * dy)) * SnowWarConstants.THROW_DISTANCE_STEP;
        var trajectory = requested == SnowWarTrajectory.Default ? Resolve(distance) : requested;
        var (life, speed) = trajectory switch
        {
            SnowWarTrajectory.Quick => (SnowWarConstants.QUICK_LIFE, SnowWarConstants.QUICK_SPEED),
            SnowWarTrajectory.ShortLob => Lob(
                Math.Min(distance, SnowWarConstants.SHORT_LOB_RANGE),
                SnowWarConstants.SHORT_LOB_TIME_PER_UNIT
            ),
            SnowWarTrajectory.LongLob => Lob(
                Math.Min(distance, SnowWarConstants.LONG_LOB_RANGE),
                SnowWarConstants.LONG_LOB_TIME_PER_UNIT
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(requested)),
        };

        return new(
            id,
            x,
            y,
            SnowWarConstants.SNOWBALL_START_HEIGHT,
            direction,
            trajectory,
            life,
            throwerId,
            life / 2,
            speed
        );
    }

    public void ForgetThrower() => ThrowerId = 0;

    public override void AppendVariables(List<int> variables)
    {
        variables.Add(X);
        variables.Add(Y);
        variables.Add(Z);
        variables.Add(Direction);
        variables.Add((int)Trajectory);
        variables.Add(Life);
        variables.Add(ThrowerId);
        variables.Add(ParabolaOffset);
        variables.Add(Speed);
    }

    public override void Update(SnowWarWorld world)
    {
        Life--;
        X += SnowWarMath.BaseVectorX(Direction) * Speed / SnowWarConstants.BASE_VECTOR_DIVISOR;
        Y += SnowWarMath.BaseVectorY(Direction) * Speed / SnowWarConstants.BASE_VECTOR_DIVISOR;

        var fromVertex = Life - ParabolaOffset;
        Z =
            (((ParabolaOffset * ParabolaOffset) - (fromVertex * fromVertex)) * HeightFactor)
            + SnowWarConstants.SNOWBALL_START_HEIGHT;

        if (Trajectory == SnowWarTrajectory.Quick)
        {
            Z = Math.Min(Z, SnowWarConstants.SNOWBALL_START_HEIGHT);
        }

        var tile = world.Grid.AtWorld(X, Y);

        if (tile is not null && HitsAnObjectAround(world, tile))
        {
            MarkRemoved();
            return;
        }

        if (Z < 1 || (tile is not null && Z < tile.Height))
        {
            MarkRemoved();
        }
    }

    private bool HitsAnObjectAround(SnowWarWorld world, SnowWarTile tile)
    {
        var heading = SnowWarMath.Dir360ToDir8(Direction);

        return TryHit(world, tile)
            || TryHit(world, world.Grid.Neighbor(tile, heading))
            || TryHit(world, world.Grid.Neighbor(tile, SnowWarMath.RotateDir8(heading, -1)))
            || TryHit(world, world.Grid.Neighbor(tile, SnowWarMath.RotateDir8(heading, 1)));
    }

    private bool TryHit(SnowWarWorld world, SnowWarTile? tile)
    {
        var occupant = tile?.Occupant;

        if (occupant is null || !occupant.IsActive || !occupant.StopsSnowball(this))
        {
            return false;
        }

        occupant.OnSnowballImpact(world, this);
        return true;
    }

    private static SnowWarTrajectory Resolve(int distance) =>
        distance <= SnowWarConstants.DEFAULT_QUICK_RANGE ? SnowWarTrajectory.Quick
        : distance <= SnowWarConstants.DEFAULT_SHORT_LOB_RANGE ? SnowWarTrajectory.ShortLob
        : SnowWarTrajectory.LongLob;

    private static (int Life, int Speed) Lob(int distance, double timePerUnit)
    {
        var life = (int)(distance * timePerUnit);
        return (life, life == 0 ? 0 : distance / life);
    }
}
