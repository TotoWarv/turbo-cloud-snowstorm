using Turbo.Primitives.Messages.Outgoing.Game.Score;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Score;

internal class Game2TotalGroupLeaderboardMessageComposerSerializer(int header)
    : AbstractSerializer<Game2TotalGroupLeaderboardMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2TotalGroupLeaderboardMessageComposer message
    )
    {
        SnowWarLeaderboardSerializer.SerializeEntries(
            packet,
            message.Entries,
            message.TotalListSize,
            message.GameTypeId
        );
        packet.WriteInteger(message.FavouriteGroupId);
    }
}
