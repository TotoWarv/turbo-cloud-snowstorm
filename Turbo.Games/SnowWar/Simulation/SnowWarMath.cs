using System;

namespace Turbo.Games.SnowWar.Simulation;

internal static class SnowWarMath
{
    private static readonly int[] DIR8_OFFSET_X = [0, 1, 1, 1, 0, -1, -1, -1];
    private static readonly int[] DIR8_OFFSET_Y = [-1, -1, 0, 1, 1, 1, 0, -1];

    public static int NormalizeDirection(int value)
    {
        if (value > 359)
        {
            return value % 360;
        }

        if (value < 0)
        {
            return 360 + (value % 360);
        }

        return value;
    }

    public static int BaseVectorX(int direction) =>
        SnowWarTables.BASE_VECTOR_X[NormalizeDirection(direction) % 360];

    public static int BaseVectorY(int direction) =>
        SnowWarTables.BASE_VECTOR_Y[NormalizeDirection(direction) % 360];

    public static int Dir360ToDir8(int direction) =>
        ((NormalizeDirection(direction - 22) / 45) + 1) & 7;

    public static int Dir8ToDir360(int direction) =>
        direction is >= 0 and <= 7 ? direction * 45 : -1;

    public static int RotateDir8(int direction, int steps) => (direction + steps) & 7;

    public static int Dir8OffsetX(int direction) => DIR8_OFFSET_X[direction & 7];

    public static int Dir8OffsetY(int direction) => DIR8_OFFSET_Y[direction & 7];

    public static int Angle(int x, int y)
    {
        if (Math.Abs(x) <= Math.Abs(y))
        {
            if (y == 0)
            {
                y = 1;
            }

            var angle = SnowWarTables.ARC_TANGENT[Math.Min(255, Math.Abs(256 * x / y))];

            if (y < 0)
            {
                return x > 0 ? angle : 360 - angle;
            }

            return x > 0 ? 180 - angle : 180 + angle;
        }

        var flat = SnowWarTables.ARC_TANGENT[Math.Min(255, Math.Abs(256 * y / x))];

        if (y < 0)
        {
            return x > 0 ? 90 - flat : 270 + flat;
        }

        return x > 0 ? 90 + flat : 270 - flat;
    }

    public static int FastSqrt(int value)
    {
        if (value < 0)
        {
            return -1;
        }

        var shift = 0;

        while ((value >> (2 * shift)) >= 256)
        {
            shift++;
        }

        var root = SnowWarTables.SQUARE_ROOT[value >> (2 * shift)];

        return shift >= 4 ? root << (shift - 4) : root >> (4 - shift);
    }

    public static bool IsWithinDistance(int ax, int ay, int bx, int by, int radius)
    {
        var dx = ax - bx;
        var dy = ay - by;

        if (Math.Abs(dx) > radius || Math.Abs(dy) > radius)
        {
            return false;
        }

        return (dx * dx) + (dy * dy) < radius * radius;
    }

    public static int WorldToTile(int world) =>
        (world + SnowWarConstants.HALF_TILE) / SnowWarConstants.TILE_SIZE;

    public static int TileToWorld(int tile) => tile * SnowWarConstants.TILE_SIZE;

    public static bool IsInTile(int x, int y, int centerX, int centerY) =>
        Math.Abs(x - centerX) < SnowWarConstants.HALF_TILE
        && Math.Abs(y - centerY) < SnowWarConstants.HALF_TILE;
}
