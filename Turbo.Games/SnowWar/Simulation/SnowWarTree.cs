using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarTree : SnowWarGameObject
{
    public SnowWarTree(int id, SnowWarFuseObject fuse, SnowWarTile tile)
        : base(id)
    {
        Tile = tile;
        FuseId = fuse.Id;
        Direction = fuse.Direction;
        Height = fuse.Height;
        tile.AddHeight(-fuse.Height);
        tile.SetOccupant(this);
    }

    public override SnowWarGameObjectType Type => SnowWarGameObjectType.Tree;

    public SnowWarTile Tile { get; }

    public int FuseId { get; }

    public int Direction { get; }

    public int Height { get; }

    public int MaxHits => SnowWarConstants.TREE_MAX_HITS;

    public int Hits { get; private set; }

    public bool IsStanding => Hits < MaxHits;

    public override void AppendVariables(List<int> variables)
    {
        variables.Add(Tile.CenterX);
        variables.Add(Tile.CenterY);
        variables.Add(Direction);
        variables.Add(Height);
        variables.Add(FuseId);
        variables.Add(MaxHits);
        variables.Add(Hits);
    }

    public override bool StopsSnowball(SnowWarSnowball snowball) =>
        IsStanding
        && snowball.Z < Height
        && SnowWarMath.IsWithinDistance(
            Tile.CenterX,
            Tile.CenterY,
            snowball.X,
            snowball.Y,
            SnowWarConstants.TREE_RADIUS + SnowWarConstants.SNOWBALL_RADIUS
        );

    public override void OnSnowballImpact(SnowWarWorld world, SnowWarSnowball snowball)
    {
        if (!IsStanding)
        {
            return;
        }

        Hits++;

        if (!IsStanding)
        {
            Tile.ClearOccupant(this);
        }
    }
}
