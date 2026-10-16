using Turbo.Primitives.Messages.Outgoing.Game.Score;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Score;

internal class Game2WeeklyFriendsLeaderboardMessageComposerSerializer(int header)
    : AbstractSerializer<Game2WeeklyFriendsLeaderboardMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2WeeklyFriendsLeaderboardMessageComposer message
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
