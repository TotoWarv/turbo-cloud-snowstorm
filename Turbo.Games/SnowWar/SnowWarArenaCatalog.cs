using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar.Simulation;

namespace Turbo.Games.SnowWar;

public sealed class SnowWarArenaCatalog
{
    private const string RESOURCE_PREFIX = "Turbo.Games.SnowWar.Arenas.";

    private readonly ImmutableDictionary<int, SnowWarLevelData> _levels;

    public SnowWarArenaCatalog(IOptions<SnowWarConfig> config)
    {
        var settings = config.Value;

        _levels = LoadEmbeddedLevels(settings.Backdrops);
        Arenas = [.. settings.Arenas.Distinct().Where(_levels.ContainsKey)];
    }

    public ImmutableArray<int> Arenas { get; }

    public SnowWarLevelData Get(int fieldType) =>
        _levels.TryGetValue(fieldType, out var level)
            ? level
            : throw new ArgumentOutOfRangeException(
                nameof(fieldType),
                fieldType,
                "No SnowWar arena has this field type."
            );

    public int Next(int previousFieldType)
    {
        if (Arenas.IsEmpty)
            throw new InvalidOperationException("No configured SnowWar arena can be played.");

        var previousIndex = Arenas.IndexOf(previousFieldType);

        return Arenas[(previousIndex + 1) % Arenas.Length];
    }

    private static ImmutableDictionary<int, SnowWarLevelData> LoadEmbeddedLevels(
        IReadOnlyDictionary<int, string> backdrops
    )
    {
        var assembly = typeof(SnowWarArenaCatalog).Assembly;

        return assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.StartsWith(RESOURCE_PREFIX, StringComparison.Ordinal)
                && name.EndsWith(".json", StringComparison.Ordinal)
            )
            .Order(StringComparer.Ordinal)
            .Select(name =>
            {
                using var stream =
                    assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidDataException($"Missing SnowWar arena resource {name}.");
                using var document = JsonDocument.Parse(stream);

                return ParseLevel(document.RootElement, backdrops);
            })
            .ToImmutableDictionary(level => level.FieldType);
    }

    private static SnowWarLevelData ParseLevel(
        JsonElement root,
        IReadOnlyDictionary<int, string> backdrops
    )
    {
        var fieldType = root.GetProperty("fieldType").GetInt32();
        ImmutableArray<string> heightMap =
        [
            .. root.GetProperty("heightmap").EnumerateArray().Select(row => row.GetString() ?? ""),
        ];

        return new SnowWarLevelData
        {
            FieldType = fieldType,
            Name = root.GetProperty("name").GetString() ?? "",
            Width = heightMap.Max(row => row.Length),
            Height = heightMap.Length,
            HeightMap = heightMap,
            Objects =
            [
                .. root.GetProperty("items")
                    .EnumerateArray()
                    .Select(item => ParseObjectLine(item.GetString() ?? "")),
            ],
            TeamSpawns = ParseTeamSpawns(root),
            Backdrop = ParseBackdrop(root, backdrops.GetValueOrDefault(fieldType)),
        };
    }

    private static SnowWarLevelObjectData ParseObjectLine(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is < 4 or > 5)
            throw new InvalidDataException($"Malformed SnowWar arena object '{line}'.");

        return new SnowWarLevelObjectData
        {
            Name = parts[0],
            X = ParseInt(parts[1], line),
            Y = ParseInt(parts[2], line),
            Direction = ParseInt(parts[3], line),
            Altitude = parts.Length == 5 ? ParseInt(parts[4], line) : 0,
        };
    }

    private static int ParseInt(string value, string line) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidDataException($"Malformed SnowWar arena object '{line}'.");

    private static ImmutableArray<ImmutableArray<SnowWarTilePosition>> ParseTeamSpawns(
        JsonElement root
    )
    {
        if (!root.TryGetProperty("spawns", out var spawns))
            return [];

        return
        [
            .. spawns
                .EnumerateObject()
                .OrderBy(team => int.Parse(team.Name, CultureInfo.InvariantCulture))
                .Select(team =>
                    team.Value.EnumerateArray()
                        .Select(tile => new SnowWarTilePosition(
                            tile[0].GetInt32(),
                            tile[1].GetInt32()
                        ))
                        .ToImmutableArray()
                ),
        ];
    }

    private static SnowWarBackdropData? ParseBackdrop(JsonElement root, string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl) || !root.TryGetProperty("backdrop", out var backdrop))
            return null;

        return new SnowWarBackdropData
        {
            ImageUrl = imageUrl,
            X = backdrop.GetProperty("x").GetInt32(),
            Y = backdrop.GetProperty("y").GetInt32(),
            OffsetX = backdrop.GetProperty("offsetX").GetInt32(),
            OffsetY = backdrop.GetProperty("offsetY").GetInt32(),
            OffsetZ = backdrop.GetProperty("offsetZ").GetInt32(),
        };
    }
}
