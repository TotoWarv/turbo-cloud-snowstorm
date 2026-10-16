using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Room.Engine.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarLevelSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, SnowWarLevelSnapshot level)
    {
        packet
            .WriteInteger(level.Width)
            .WriteInteger(level.Height)
            .WriteString(level.HeightMap)
            .WriteInteger(level.FuseObjects.Length);

        foreach (var fuseObject in level.FuseObjects)
        {
            packet
                .WriteString(fuseObject.Name)
                .WriteInteger(fuseObject.Id)
                .WriteInteger(fuseObject.X)
                .WriteInteger(fuseObject.Y)
                .WriteInteger(fuseObject.XDimension)
                .WriteInteger(fuseObject.YDimension)
                .WriteInteger(fuseObject.Height)
                .WriteInteger(fuseObject.Direction)
                .WriteInteger(fuseObject.Altitude)
                .WriteBoolean(fuseObject.CanStandOn);

            StuffDataSnapshotSerializer.Serialize(packet, fuseObject.StuffData);
        }
    }
}
