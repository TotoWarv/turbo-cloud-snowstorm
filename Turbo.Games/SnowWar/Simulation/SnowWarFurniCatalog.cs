using System;
using System.Collections.Generic;
using System.Linq;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

internal static class SnowWarFurniCatalog
{
    public static readonly SnowWarFurniDefinition BACKDROP = new(
        "ads_background",
        1,
        1,
        0,
        SnowWarFurniRole.Backdrop
    );

    private static readonly Dictionary<string, SnowWarFurniDefinition> DEFINITIONS =
        new SnowWarFurniDefinition[]
        {
            new("snst_block1", 1, 1, 1440, SnowWarFurniRole.Obstacle),
            new("snst_iceblock", 1, 1, 1760, SnowWarFurniRole.Obstacle),
            new("snst_tree1", 1, 1, 3200, SnowWarFurniRole.Tree),
            new("snst_tree1_d", 1, 1, 3200, SnowWarFurniRole.Tree),
            new("snst_fence", 1, 2, 960, SnowWarFurniRole.Obstacle),
            new("snst_ballpile", 1, 1, 320, SnowWarFurniRole.Pile),
            new("s_snowball_machine", 1, 1, 1600, SnowWarFurniRole.Machine),
            new("snowball_machine", 1, 1, 1600, SnowWarFurniRole.Machine),
            new("ads_igorraygun", 1, 2, 1600, SnowWarFurniRole.RayGun),
            new("xm09_man_a", 1, 1, 1280, SnowWarFurniRole.Obstacle),
            new("xm09_man_b", 1, 1, 1200, SnowWarFurniRole.Obstacle),
            new("xm09_man_c", 1, 1, 800, SnowWarFurniRole.Obstacle),
            BACKDROP,
        }.ToDictionary(definition => definition.Name, StringComparer.Ordinal);

    public static SnowWarFurniDefinition Get(string name) =>
        DEFINITIONS.TryGetValue(name, out var definition)
            ? definition
            : throw new ArgumentException($"Unknown Snowstorm furni '{name}'.", nameof(name));
}
