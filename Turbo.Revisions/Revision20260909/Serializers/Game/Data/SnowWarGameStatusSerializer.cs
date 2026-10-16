using System.Collections.Immutable;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarGameStatusSerializer
{
    public static void Serialize(
        IServerPacket packet,
        int turn,
        int checksum,
        ImmutableArray<ImmutableArray<SnowWarGameEventSnapshot>> subturns
    )
    {
        packet.WriteInteger(turn).WriteInteger(checksum).WriteInteger(subturns.Length);

        foreach (var events in subturns)
        {
            packet.WriteInteger(events.Length);

            foreach (var gameEvent in events)
            {
                packet.WriteInteger((int)gameEvent.Type);

                foreach (var field in gameEvent.Fields)
                {
                    packet.WriteInteger(field);
                }
            }
        }
    }
}
