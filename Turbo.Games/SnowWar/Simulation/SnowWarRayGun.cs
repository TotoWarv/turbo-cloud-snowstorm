namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarRayGun
{
    private static readonly int[] SPREAD_X = [0, 0, 1, -1, -1, 1, 1];
    private static readonly int[] SPREAD_Y = [0, 1, 0, 1, -1, -1, 1];

    private SnowWarRayGun(SnowWarFuseObject fuse, int forwardX, int forwardY)
    {
        Fuse = fuse;
        ForwardX = forwardX;
        ForwardY = forwardY;
        UseTileX = fuse.X + (forwardX < 0 ? fuse.FootprintWidth : -forwardX);
        UseTileY = fuse.Y + (forwardY < 0 ? fuse.FootprintDepth : -forwardY);
    }

    public SnowWarFuseObject Fuse { get; }

    public int FuseId => Fuse.Id;

    public int Direction => Fuse.Direction;

    public int ForwardX { get; }

    public int ForwardY { get; }

    public int UseTileX { get; }

    public int UseTileY { get; }

    public int? LastBurstTurn { get; set; }

    public static SnowWarRayGun? TryCreate(SnowWarFuseObject fuse) =>
        fuse.Direction switch
        {
            0 => new(fuse, 0, -1),
            2 => new(fuse, 1, 0),
            4 => new(fuse, 0, 1),
            6 => new(fuse, -1, 0),
            _ => null,
        };

    public int TargetTileX(int index) =>
        Fuse.X + (SnowWarConstants.RAY_GUN_RANGE * ForwardX) + SPREAD_X[index];

    public int TargetTileY(int index) =>
        Fuse.Y + (SnowWarConstants.RAY_GUN_RANGE * ForwardY) + SPREAD_Y[index];

    public bool IsReadyFor(int turn) =>
        LastBurstTurn is not int last
        || (turn * SnowWarConstants.SUBTURNS_PER_TURN) - (last * SnowWarConstants.SUBTURNS_PER_TURN)
            >= SnowWarConstants.RAY_GUN_RECHARGE_SUBTURNS;
}
