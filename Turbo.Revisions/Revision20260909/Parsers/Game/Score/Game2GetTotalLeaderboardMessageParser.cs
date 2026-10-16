using Turbo.Primitives.Messages.Incoming.Game.Score;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;

namespace Turbo.Revisions.Revision20260909.Parsers.Game.Score;

internal class Game2GetTotalLeaderboardMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) =>
        new Game2GetTotalLeaderboardMessage
        {
            GameTypeId = packet.PopInt(),
            StartRank = packet.PopInt(),
            Direction = packet.PopInt(),
            ViewSize = packet.PopInt(),
            WindowSize = packet.PopInt(),
        };
}
