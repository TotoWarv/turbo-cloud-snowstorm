using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Directory;

internal class Game2UserJoinedGameMessageComposerSerializer(int header)
    : AbstractSerializer<Game2UserJoinedGameMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2UserJoinedGameMessageComposer message
    )
    {
        SnowWarPlayerSnapshotSerializer.Serialize(packet, message.Player);
        packet.WriteBoolean(message.WasTeamSwitched);
    }
}
