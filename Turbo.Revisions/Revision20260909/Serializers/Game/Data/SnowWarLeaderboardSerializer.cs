using System.Collections.Immutable;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Data;

internal static class SnowWarLeaderboardSerializer
{
    public static void SerializeWeek(
        IServerPacket packet,
        int year,
        int week,
        int maxOffset,
        int currentOffset,
        int minutesUntilReset
    )
    {
        packet
            .WriteInteger(year)
            .WriteInteger(week)
            .WriteInteger(maxOffset)
            .WriteInteger(currentOffset)
            .WriteInteger(minutesUntilReset);
    }

    public static void SerializeEntries(
        IServerPacket packet,
        ImmutableArray<SnowWarLeaderboardEntrySnapshot> entries,
        int totalListSize,
        int gameTypeId
    )
    {
        packet.WriteInteger(entries.Length);

        foreach (var entry in entries)
        {
            packet
                .WriteInteger(entry.UserId)
                .WriteInteger(entry.Score)
                .WriteInteger(entry.Rank)
                .WriteString(entry.Name)
                .WriteString(entry.Figure)
                .WriteString(entry.Gender);
        }

        packet.WriteInteger(totalListSize).WriteInteger(gameTypeId);
    }
}
