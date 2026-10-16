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

public class Game2GetWeeklyLeaderboardMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2GetWeeklyLeaderboardMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2GetWeeklyLeaderboardMessage message,
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
                    SnowWarLeaderboardScope.Everyone,
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
                new Game2WeeklyLeaderboardMessageComposer
                {
                    Year = result.Year,
                    Week = result.Week,
                    MaxOffset = result.MaxOffset,
                    CurrentOffset = result.CurrentOffset,
                    MinutesUntilReset = result.MinutesUntilReset,
                    Entries = result.Page.Entries,
                    TotalListSize = result.Page.TotalListSize,
                    GameTypeId = message.GameTypeId,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
