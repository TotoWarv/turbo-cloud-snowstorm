using System.Collections.Immutable;
using Turbo.Games.SnowWar.Simulation;

namespace Turbo.Tests.Games.SnowWar.Simulation;

internal static class SnowWarTestLevels
{
    public static SnowWarLevelData Open(
        int width,
        int height,
        params SnowWarLevelObjectData[] objects
    ) => Rows([.. Enumerable.Repeat(new string('0', width), height)], objects);

    public static SnowWarLevelData Rows(string[] rows, params SnowWarLevelObjectData[] objects) =>
        new()
        {
            FieldType = 0,
            Name = "test",
            Width = rows.Max(row => row.Length),
            Height = rows.Length,
            HeightMap = [.. rows],
            Objects = [.. objects],
        };

    public static SnowWarLevelData WithSpawns(
        SnowWarLevelData level,
        SnowWarTilePosition[] teamOne,
        SnowWarTilePosition[] teamTwo
    ) =>
        level with
        {
            TeamSpawns =
            [
                [.. teamOne],
                [.. teamTwo],
            ],
        };

    public static SnowWarLevelObjectData Furni(
        string name,
        int x,
        int y,
        int direction = 0,
        int altitude = 0
    ) =>
        new()
        {
            Name = name,
            X = x,
            Y = y,
            Direction = direction,
            Altitude = altitude,
        };

    public static ImmutableArray<int> Fields(params int[] values) => [.. values];
}
