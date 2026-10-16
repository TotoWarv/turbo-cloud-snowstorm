using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarArenaPlayerSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, SnowWarArenaPlayerSnapshot player)
    {
        packet
            .WriteInteger(player.ReferenceId)
            .WriteString(player.UserName)
            .WriteString(player.Figure)
            .WriteString(player.Gender)
            .WriteInteger(player.TeamId);
    }
}
