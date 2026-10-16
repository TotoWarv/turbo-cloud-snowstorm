using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarPile : SnowWarGameObject, ISnowWarSnowballSource
{
    public SnowWarPile(int id, SnowWarFuseObject fuse, SnowWarTile tile)
        : base(id)
    {
        Tile = tile;
        FuseId = fuse.Id;
        Remaining = Max;
        tile.SetOccupant(this);
    }

    public override SnowWarGameObjectType Type => SnowWarGameObjectType.Pile;

    public SnowWarTile Tile { get; }

    public int FuseId { get; }

    public int Max => SnowWarConstants.PILE_MAX;

    public int Remaining { get; private set; }

    private int Radius => SnowWarConstants.PILE_RADIUS_PER_SNOWBALL * Remaining;

    public override void AppendVariables(List<int> variables)
    {
        variables.Add(Tile.CenterX);
        variables.Add(Tile.CenterY);
        variables.Add(Max);
        variables.Add(Remaining);
        variables.Add(FuseId);
    }

    public override bool StopsSnowball(SnowWarSnowball snowball) =>
        Remaining > 0
        && snowball.Z < Radius
        && SnowWarMath.IsWithinDistance(
            Tile.CenterX,
            Tile.CenterY,
            snowball.X,
            snowball.Y,
            Radius + SnowWarConstants.SNOWBALL_RADIUS
        );

    public bool TryTakeSnowball()
    {
        if (Remaining <= 0)
        {
            return false;
        }

        Remaining--;

        if (Remaining == 0)
        {
            Tile.ClearOccupant(this);
        }

        return true;
    }
}
