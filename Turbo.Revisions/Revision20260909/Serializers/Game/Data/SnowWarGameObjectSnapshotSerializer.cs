using System.Collections.Immutable;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarGameObjectSnapshotSerializer
{
    public static void Serialize(
        IServerPacket packet,
        ImmutableArray<SnowWarGameObjectSnapshot> objects
    )
    {
        packet.WriteInteger(objects.Length);

        foreach (var gameObject in objects)
        {
            packet.WriteInteger((int)gameObject.Type).WriteInteger(gameObject.Id);

            foreach (var variable in gameObject.Variables)
            {
                packet.WriteInteger(variable);
            }

            foreach (var text in gameObject.Strings)
            {
                packet.WriteString(text);
            }
        }
    }
}
