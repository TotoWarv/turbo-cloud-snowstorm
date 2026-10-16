using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Ingame;

internal class Game2GameStatusMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameStatusMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2GameStatusMessageComposer message)
    {
        SnowWarGameStatusSerializer.Serialize(
            packet,
            message.Turn,
            message.Checksum,
            message.Subturns
        );
    }
}
