using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarGrid
{
    private const char MISSING_TILE = 'x';

    private readonly SnowWarTile?[] _tiles;

    public SnowWarGrid(int width, int height, ImmutableArray<string> heightMap)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        _tiles = new SnowWarTile?[width * height];
        var existing = new List<SnowWarTile>();

        for (var y = 0; y < height; y++)
        {
            var row = y < heightMap.Length ? heightMap[y] : string.Empty;

            for (var x = 0; x < width; x++)
            {
                if (x < row.Length && row[x] == MISSING_TILE)
                {
                    continue;
                }

                var tile = new SnowWarTile(x, y);
                _tiles[(y * width) + x] = tile;
                existing.Add(tile);
            }
        }

        Tiles = existing;
    }

    public int Width { get; }

    public int Height { get; }

    public IReadOnlyList<SnowWarTile> Tiles { get; }

    public SnowWarTile? Get(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height ? _tiles[(y * Width) + x] : null;

    public SnowWarTile? AtWorld(int x, int y) =>
        Get(SnowWarMath.WorldToTile(x), SnowWarMath.WorldToTile(y));

    public SnowWarTile? Neighbor(SnowWarTile tile, int direction) =>
        Get(
            tile.X + SnowWarMath.Dir8OffsetX(direction),
            tile.Y + SnowWarMath.Dir8OffsetY(direction)
        );

    public int ClampX(int x) => Math.Clamp(x, 0, SnowWarMath.TileToWorld(Width - 1));

    public int ClampY(int y) => Math.Clamp(y, 0, SnowWarMath.TileToWorld(Height - 1));
}
