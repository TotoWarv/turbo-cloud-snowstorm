using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.Game.Score;

internal static class SnowWarLeaderboardRequests
{
    public static SnowWarLeaderboardRequestSnapshot Of(
        PlayerId playerId,
        SnowWarLeaderboardScope scope,
        int startRank,
        int viewSize,
        int windowSize
    ) =>
        new()
        {
            PlayerId = playerId,
            Scope = scope,
            StartRank = startRank,
            ViewSize = viewSize,
            WindowSize = windowSize,
        };
}
