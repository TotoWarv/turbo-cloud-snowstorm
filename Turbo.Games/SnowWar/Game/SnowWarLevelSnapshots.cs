using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Furniture.Snapshots.StuffData;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Game;

internal static class SnowWarLevelSnapshots
{
    private const string ROW_SEPARATOR = "\r";
    private const string DEFAULT_STATE = "0";
    private const int LEGACY_STUFF_TYPE = 0;
    private const int MAP_STUFF_TYPE = 1;

    public static SnowWarLevelSnapshot Build(SnowWarLevelData level, SnowWarArena arena) =>
        new()
        {
            Width = level.Width,
            Height = level.Height,
            HeightMap = string.Join(ROW_SEPARATOR, level.HeightMap),
            FuseObjects =
            [
                .. arena.World.FuseObjects.Select(fuse => FuseObject(fuse, level.Backdrop)),
            ],
        };

    private static SnowWarFuseObjectSnapshot FuseObject(
        SnowWarFuseObject fuse,
        SnowWarBackdropData? backdrop
    ) =>
        new()
        {
            Name = fuse.Definition.Name,
            Id = fuse.Id,
            X = fuse.X,
            Y = fuse.Y,
            XDimension = fuse.Definition.XDimension,
            YDimension = fuse.Definition.YDimension,
            Height = fuse.Height,
            Direction = fuse.Direction,
            Altitude = fuse.Altitude,
            CanStandOn = fuse.CanStandOn,
            StuffData =
                fuse.Definition.Role == SnowWarFurniRole.Backdrop && backdrop is not null
                    ? BackdropStuff(backdrop)
                    : new LegacyStuffSnapshot
                    {
                        StuffBitmask = LEGACY_STUFF_TYPE,
                        Data = DEFAULT_STATE,
                    },
        };

    private static MapStuffSnapshot BackdropStuff(SnowWarBackdropData backdrop) =>
        new()
        {
            StuffBitmask = MAP_STUFF_TYPE,
            Data = ImmutableDictionary<string, string>
                .Empty.Add("state", DEFAULT_STATE)
                .Add("imageUrl", backdrop.ImageUrl)
                .Add("offsetX", Format(backdrop.OffsetX))
                .Add("offsetY", Format(backdrop.OffsetY))
                .Add("offsetZ", Format(backdrop.OffsetZ)),
        };

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}
