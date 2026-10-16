using Turbo.Primitives.Messages.Incoming.Game.Ingame;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Parsers.Game.Ingame;

internal class Game2SetUserMoveTargetMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new Game2SetUserMoveTargetMessage
        {
            X = packet.PopInt(),
            Y = packet.PopInt(),
            Turn = packet.PopInt(),
            Subturn = packet.PopInt(),
        };
}
