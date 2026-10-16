using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Arena;

internal class Game2ArenaEnteredMessageComposerSerializer(int header)
    : AbstractSerializer<Game2ArenaEnteredMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2ArenaEnteredMessageComposer message
    )
    {
        SnowWarArenaPlayerSnapshotSerializer.Serialize(packet, message.Player);
    }
}
