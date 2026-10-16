using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Game.Score;

public record Game2GetWeeklyFriendsLeaderboardMessage : IMessageEvent
{
    public required int GameTypeId { get; init; }
    public required int WeekOffset { get; init; }
    public required int StartRank { get; init; }
    public required int Direction { get; init; }
    public required int ViewSize { get; init; }
    public required int WindowSize { get; init; }
}
