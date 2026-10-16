using Turbo.Primitives.Messages.Outgoing.Game.Score;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Score;

internal class Game2WeeklyLeaderboardMessageComposerSerializer(int header)
    : AbstractSerializer<Game2WeeklyLeaderboardMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2WeeklyLeaderboardMessageComposer message
    )
    {
        SnowWarLeaderboardSerializer.SerializeWeek(
            packet,
            message.Year,
            message.Week,
            message.MaxOffset,
            message.CurrentOffset,
            message.MinutesUntilReset
        );

        SnowWarLeaderboardSerializer.SerializeEntries(
            packet,
            message.Entries,
            message.TotalListSize,
            message.GameTypeId
        );
    }
}
