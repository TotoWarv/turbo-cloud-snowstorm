using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260909.Serializers.Game.Data;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Arena;

internal class Game2EnterArenaMessageComposerSerializer(int header)
    : AbstractSerializer<Game2EnterArenaMessageComposer>(header)
{
    protected override void Serialize(IServerPacket packet, Game2EnterArenaMessageComposer message)
    {
        packet
            .WriteInteger(message.GameType)
            .WriteInteger(message.FieldType)
            .WriteInteger(message.NumberOfTeams)
            .WriteInteger(message.Players.Length);

        foreach (var player in message.Players)
        {
            SnowWarArenaPlayerSnapshotSerializer.Serialize(packet, player);
        }

        SnowWarLevelSnapshotSerializer.Serialize(packet, message.Level);
    }
}
