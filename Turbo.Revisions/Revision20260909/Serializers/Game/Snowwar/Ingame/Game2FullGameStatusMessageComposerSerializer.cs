using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Ingame;

internal class Game2FullGameStatusMessageComposerSerializer(int header)
    : AbstractSerializer<Game2FullGameStatusMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2FullGameStatusMessageComposer message
    )
    {
        packet
            .WriteInteger(0)
            .WriteInteger(message.RemainingSeconds)
            .WriteInteger(message.DurationSeconds);

        SnowWarGameObjectSnapshotSerializer.Serialize(packet, message.Objects);

        packet.WriteInteger(0).WriteInteger(message.NumberOfTeams);

        SnowWarGameStatusSerializer.Serialize(
            packet,
            message.Turn,
            message.Checksum,
            message.Subturns
        );
    }
}
