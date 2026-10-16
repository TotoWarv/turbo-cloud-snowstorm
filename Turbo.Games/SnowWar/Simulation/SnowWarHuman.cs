using System;
using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarHuman : SnowWarGameObject
{
    private readonly SnowWarHumanEntry _entry;

    public SnowWarHuman(int id, SnowWarHumanEntry entry, SnowWarTile tile, int bodyDirection)
        : base(id)
    {
        _entry = entry;
        Tile = tile;
        X = tile.CenterX;
        Y = tile.CenterY;
        TargetX = X;
        TargetY = Y;
        BodyDirection = bodyDirection;
        Hp = SnowWarConstants.MAX_HEALTH;
        Balls = SnowWarConstants.MAX_SNOWBALLS;
        tile.SetOccupant(this);
    }

    public override SnowWarGameObjectType Type => SnowWarGameObjectType.Human;

    public override IReadOnlyList<string> Strings =>
        [_entry.Name, _entry.Mission, _entry.Figure, _entry.Gender];

    public int UserId => _entry.UserId;

    public int Team => _entry.Team;

    public int X { get; private set; }

    public int Y { get; private set; }

    public SnowWarTile Tile { get; private set; }

    public SnowWarTile? NextTile { get; private set; }

    public int TargetX { get; private set; }

    public int TargetY { get; private set; }

    public int BodyDirection { get; private set; }

    public int Hp { get; private set; }

    public int Balls { get; private set; }

    public int ActivityTimer { get; private set; }

    public SnowWarActivityState ActivityState { get; private set; }

    public int ThrowTimer { get; private set; }

    public int Score { get; private set; }

    public bool IsWalking { get; private set; }

    public int PickupTimer { get; set; }

    public SnowWarRayGun? ConsumedRayGun { get; set; }

    public SnowWarHumanCounters Counters { get; } = new();

    public SnowWarPlayerStatsSnapshot Stats => Counters.ToSnapshot(Score);

    public bool CanWalk =>
        ActivityState is SnowWarActivityState.Normal or SnowWarActivityState.Invincible;

    public bool CanThrow => CanWalk && Balls >= 1 && ThrowTimer < 1;

    public bool CanMakeSnowball => CanWalk && Balls < SnowWarConstants.MAX_SNOWBALLS;

    private bool IsTouchable =>
        ActivityState is SnowWarActivityState.Normal or SnowWarActivityState.MakingSnowball;

    public bool HasArrivedOn(SnowWarTile tile) =>
        NextTile is null
        && ReferenceEquals(Tile, tile)
        && X == tile.CenterX
        && Y == tile.CenterY
        && SnowWarMath.IsInTile(TargetX, TargetY, tile.CenterX, tile.CenterY);

    public override void AppendVariables(List<int> variables)
    {
        var next = NextTile ?? Tile;
        variables.Add(X);
        variables.Add(Y);
        variables.Add(Tile.X);
        variables.Add(Tile.Y);
        variables.Add(BodyDirection);
        variables.Add(Hp);
        variables.Add(Balls);
        variables.Add(0);
        variables.Add(ActivityTimer);
        variables.Add((int)ActivityState);
        variables.Add(next.X);
        variables.Add(next.Y);
        variables.Add(TargetX);
        variables.Add(TargetY);
        variables.Add(Score);
        variables.Add(Team);
        variables.Add(UserId);
    }

    public override void Update(SnowWarWorld world)
    {
        if (ActivityTimer > 0)
        {
            if (ActivityTimer == 1)
            {
                EndActivity();
            }

            ActivityTimer--;
        }

        if (ThrowTimer > 0)
        {
            ThrowTimer--;
        }

        if (!CanWalk)
        {
            IsWalking = false;
            return;
        }

        if (NextTile is not null)
        {
            Step();
            return;
        }

        if (SnowWarMath.IsInTile(TargetX, TargetY, Tile.CenterX, Tile.CenterY))
        {
            IsWalking = false;
            return;
        }

        ChooseNextTile(world.Grid);
    }

    public override bool StopsSnowball(SnowWarSnowball snowball) =>
        IsTouchable
        && snowball.ThrowerId != Id
        && snowball.Z < SnowWarConstants.HUMAN_HIT_HEIGHT
        && SnowWarMath.IsWithinDistance(
            X,
            Y,
            snowball.X,
            snowball.Y,
            SnowWarConstants.HUMAN_RADIUS + SnowWarConstants.SNOWBALL_RADIUS
        );

    public override void OnSnowballImpact(SnowWarWorld world, SnowWarSnowball snowball)
    {
        var thrower = world.FindActive<SnowWarHuman>(snowball.ThrowerId);

        if (thrower is null)
        {
            return;
        }

        if (thrower.Team == Team)
        {
            thrower.Counters.FriendlyHits++;
            return;
        }

        if (Hp == 0)
        {
            return;
        }

        if (Hp == 1)
        {
            KnockDown(snowball.Direction);
            world.AwardPoints(thrower, SnowWarConstants.KNOCKDOWN_POINTS);
            thrower.Counters.Kills++;
            Counters.Deaths++;
        }

        Hp--;
        thrower.Counters.Hits++;
        Counters.HitsTaken++;
        world.AwardPoints(thrower, SnowWarConstants.HIT_POINTS);
    }

    public void AddScore(int points) => Score += points;

    public void KnockDown(int snowballDirection)
    {
        ActivityState = SnowWarActivityState.Stunned;
        ActivityTimer = SnowWarConstants.STUN_SUBTURNS;
        BodyDirection = SnowWarMath.RotateDir8(SnowWarMath.Dir360ToDir8(snowballDirection), 4);
        Stop();
    }

    public void SetMoveTarget(int x, int y)
    {
        if (ActivityState == SnowWarActivityState.MakingSnowball)
        {
            ActivityState = SnowWarActivityState.Normal;
            ActivityTimer = 0;
        }

        if (CanWalk)
        {
            TargetX = x;
            TargetY = y;
        }
    }

    public void ThrowAt(int x, int y)
    {
        if (Balls >= 1)
        {
            Stop();
            BodyDirection = SnowWarMath.Dir360ToDir8(SnowWarMath.Angle(x - X, y - Y));
            Balls--;
            Counters.SnowballsThrown++;
        }

        ThrowTimer = SnowWarConstants.THROW_INTERVAL;
    }

    public void StartMakingSnowball()
    {
        if (!CanMakeSnowball)
        {
            return;
        }

        ActivityState = SnowWarActivityState.MakingSnowball;
        ActivityTimer = SnowWarConstants.MAKE_SNOWBALL_SUBTURNS;
        Stop();
    }

    public void ReceiveSnowball(ISnowWarSnowballSource source)
    {
        if (Balls >= SnowWarConstants.MAX_SNOWBALLS || !source.TryTakeSnowball())
        {
            return;
        }

        Balls++;
        Counters.SnowballsPicked++;
    }

    public void FireRayGun(SnowWarWorld world, SnowWarRayGun gun, int firstSnowballId)
    {
        BodyDirection = gun.Direction;
        ThrowTimer = SnowWarConstants.THROW_INTERVAL;

        for (var i = 0; i < SnowWarConstants.RAY_GUN_SNOWBALLS; i++)
        {
            var id = firstSnowballId + i;

            if (world.Contains(id))
            {
                continue;
            }

            world.Add(
                SnowWarSnowball.Create(
                    id,
                    Id,
                    X,
                    Y,
                    SnowWarMath.TileToWorld(gun.TargetTileX(i)),
                    SnowWarMath.TileToWorld(gun.TargetTileY(i)),
                    SnowWarTrajectory.Default
                )
            );
        }
    }

    public void Leave(SnowWarWorld world)
    {
        Tile.ClearOccupant(this);
        NextTile?.ClearOccupant(this);
        MarkRemoved();
        world.ForgetThrower(Id);
    }

    private void EndActivity()
    {
        switch (ActivityState)
        {
            case SnowWarActivityState.Stunned:
                Hp = SnowWarConstants.MAX_HEALTH;
                ActivityState = SnowWarActivityState.Invincible;
                ActivityTimer = SnowWarConstants.INVINCIBILITY_SUBTURNS;
                break;
            case SnowWarActivityState.MakingSnowball:
                Balls++;
                Counters.SnowballsMade++;
                ActivityState = SnowWarActivityState.Normal;
                break;
            case SnowWarActivityState.Invincible:
                ActivityState = SnowWarActivityState.Normal;
                break;
            case SnowWarActivityState.Normal:
                break;
        }
    }

    private void Stop()
    {
        if (NextTile is not null)
        {
            Tile = NextTile;
            NextTile = null;
        }

        X = Tile.CenterX;
        Y = Tile.CenterY;
        TargetX = X;
        TargetY = Y;
        IsWalking = false;
    }

    private void ChooseNextTile(SnowWarGrid grid)
    {
        var heading = SnowWarMath.Dir360ToDir8(
            SnowWarMath.Angle(TargetX - Tile.CenterX, TargetY - Tile.CenterY)
        );
        var straight = grid.Neighbor(Tile, heading);

        if (straight is not null && straight.IsWalkable)
        {
            MoveTo(straight, heading);
            return;
        }

        if (straight is not null && TargetX == straight.CenterX && TargetY == straight.CenterY)
        {
            Stop();
            return;
        }

        foreach (var turn in (ReadOnlySpan<int>)[-1, 1])
        {
            var direction = SnowWarMath.RotateDir8(heading, turn);
            var candidate = grid.Neighbor(Tile, direction);

            if (candidate is not null && candidate.IsWalkable)
            {
                MoveTo(candidate, direction);
                return;
            }
        }

        IsWalking = false;
    }

    private void MoveTo(SnowWarTile next, int direction)
    {
        Tile.ClearOccupant(this);
        next.SetOccupant(this);
        NextTile = next;
        BodyDirection = direction;
        Step();
    }

    private void Step()
    {
        var next = NextTile!;
        X = StepAxis(X, next.CenterX);
        Y = StepAxis(Y, next.CenterY);

        if (
            Math.Abs(X - next.CenterX) + Math.Abs(Y - next.CenterY)
            < SnowWarConstants.ARRIVAL_THRESHOLD
        )
        {
            Tile = next;
            NextTile = null;
        }

        IsWalking = true;
    }

    private static int StepAxis(int position, int center)
    {
        var gap = position - center;

        if (gap == 0)
        {
            return position;
        }

        if (Math.Abs(gap) < SnowWarConstants.WALK_STEP)
        {
            return center;
        }

        return gap > 0
            ? position - SnowWarConstants.WALK_STEP
            : position + SnowWarConstants.WALK_STEP;
    }
}
