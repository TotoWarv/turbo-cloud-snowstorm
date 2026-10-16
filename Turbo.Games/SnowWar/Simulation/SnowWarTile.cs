using System;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarTile(int x, int y)
{
    public int X { get; } = x;

    public int Y { get; } = y;

    public int CenterX => SnowWarMath.TileToWorld(X);

    public int CenterY => SnowWarMath.TileToWorld(Y);

    public int Height { get; private set; }

    public int FuseObjectCount { get; private set; }

    public bool IsBlocked { get; private set; }

    public SnowWarGameObject? Occupant { get; private set; }

    private bool _firstFuseObjectStandable;

    public bool IsWalkable => Occupant is null && !IsBlocked && !HasBlockingFuseObjects;

    private bool HasBlockingFuseObjects =>
        FuseObjectCount > 1 || (FuseObjectCount == 1 && !_firstFuseObjectStandable);

    public void RegisterAnchor(SnowWarFuseObject fuse)
    {
        if (FuseObjectCount == 0)
        {
            _firstFuseObjectStandable = fuse.CanStandOn;
        }

        FuseObjectCount++;
        AddHeight(fuse.Height);
    }

    public void RegisterFootprint(SnowWarFuseObject fuse)
    {
        AddHeight(fuse.Height);

        if (!fuse.CanStandOn)
        {
            IsBlocked = true;
        }
    }

    public void AddHeight(int height) => Height = Math.Max(0, Height + height);

    public void SetOccupant(SnowWarGameObject occupant) => Occupant ??= occupant;

    public void ClearOccupant(SnowWarGameObject occupant)
    {
        if (ReferenceEquals(Occupant, occupant))
        {
            Occupant = null;
        }
    }
}
