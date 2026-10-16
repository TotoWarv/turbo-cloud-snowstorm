using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Messages.Incoming.Game.Score;
using Turbo.Primitives.Messages.Outgoing.Game.Score;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Game.Score;

public class Game2GetWeeklyGroupLeaderboardMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2GetWeeklyGroupLeaderboardMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2GetWeeklyGroupLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GameTypeId != SnowWarGameTypes.SNOWWAR)
            return;

        var result = await _grainFactory
            .GetSnowWarScoreGrain()
            .GetWeeklyAsync(
                SnowWarLeaderboardRequests.Of(
                    ctx.PlayerId,
                    SnowWarLeaderboardScope.Groups,
                    message.StartRank,
                    message.ViewSize,
                    message.WindowSize
                ),
                message.WeekOffset,
                ct
            )
            .ConfigureAwait(false);

        if (result is null)
            return;

        await ctx.SendComposerAsync(
                new Game2WeeklyGroupLeaderboardMessageComposer
                {
                    Year = result.Year,
                    Week = result.Week,
                    MaxOffset = result.MaxOffset,
                    CurrentOffset = result.CurrentOffset,
                    MinutesUntilReset = result.MinutesUntilReset,
                    Entries = result.Page.Entries,
                    TotalListSize = result.Page.TotalListSize,
                    GameTypeId = message.GameTypeId,
                    FavouriteGroupId = SnowWarLeaderboardGroups.NO_FAVOURITE,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
