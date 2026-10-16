using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Serializers.Game.Snowwar.Arena;

internal class Game2StageStillLoadingMessageComposerSerializer(int header)
    : AbstractSerializer<Game2StageStillLoadingMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        Game2StageStillLoadingMessageComposer message
    )
    {
        packet.WriteInteger(message.Percentage).WriteInteger(message.FinishedUserIds.Length);

        foreach (var userId in message.FinishedUserIds)
        {
            packet.WriteInteger(userId);
        }
    }
}
