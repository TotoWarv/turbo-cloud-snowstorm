using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Directory;

internal class Game2GameLongDataMessageComposerSerializer(int header)
    : AbstractSerializer<Game2GameLongDataMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2GameLongDataMessageComposer message
    )
    {
        SnowWarGameLobbySnapshotSerializer.Serialize(packet, message.Lobby);
    }
}
