using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarPlayerSnapshotSerializer
{
    public static void Serialize(IServerPacket packet, SnowWarPlayerSnapshot player)
    {
        packet
            .WriteInteger(player.UserId)
            .WriteString(player.Name)
            .WriteString(player.Figure)
            .WriteString(player.Gender)
            .WriteInteger(player.TeamId)
            .WriteInteger(player.SkillLevel)
            .WriteInteger(player.TotalScore)
            .WriteInteger(player.ScoreToNextLevel);
    }
}
