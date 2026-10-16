using System.Collections.Generic;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarMachine : SnowWarGameObject, ISnowWarSnowballSource
{
    public SnowWarMachine(int id, SnowWarFuseObject fuse, SnowWarTile tile)
        : base(id)
    {
        Tile = tile;
        FuseId = fuse.Id;
        Direction = fuse.Direction;
        tile.SetOccupant(this);
    }

    public override SnowWarGameObjectType Type => SnowWarGameObjectType.Machine;

    public SnowWarTile Tile { get; }

    public int FuseId { get; }

    public int Direction { get; }

    public int Max => SnowWarConstants.MACHINE_MAX;

    public int Stock { get; private set; }

    public int RefillTimer { get; set; } = -1;

    public int WithdrawTileX => Tile.X;

    public int WithdrawTileY => Tile.Y + 1;

    public override void AppendVariables(List<int> variables)
    {
        variables.Add(Tile.CenterX);
        variables.Add(Tile.CenterY);
        variables.Add(Direction);
        variables.Add(Max);
        variables.Add(Stock);
        variables.Add(FuseId);
    }

    public override bool StopsSnowball(SnowWarSnowball snowball) =>
        snowball.Z < SnowWarConstants.MACHINE_RADIUS
        && SnowWarMath.IsWithinDistance(
            Tile.CenterX,
            Tile.CenterY,
            snowball.X,
            snowball.Y,
            SnowWarConstants.MACHINE_RADIUS + SnowWarConstants.SNOWBALL_RADIUS
        );

    public void AddSnowball()
    {
        if (Stock < Max)
        {
            Stock++;
        }
    }

    public bool TryTakeSnowball()
    {
        if (Stock <= 0)
        {
            return false;
        }

        Stock--;
        return true;
    }
}
