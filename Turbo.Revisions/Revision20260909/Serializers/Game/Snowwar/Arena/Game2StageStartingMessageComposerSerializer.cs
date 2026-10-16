using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Arena;

internal class Game2StageStartingMessageComposerSerializer(int header)
    : AbstractSerializer<Game2StageStartingMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2StageStartingMessageComposer message
    )
    {
        packet
            .WriteInteger(message.GameType)
            .WriteString(message.RoomType)
            .WriteInteger(message.CountDown);

        SnowWarGameObjectSnapshotSerializer.Serialize(packet, message.Objects);
    }
}
