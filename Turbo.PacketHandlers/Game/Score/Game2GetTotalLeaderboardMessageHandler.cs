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

public class Game2GetTotalLeaderboardMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2GetTotalLeaderboardMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2GetTotalLeaderboardMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GameTypeId != SnowWarGameTypes.SNOWWAR)
            return;

        var result = await _grainFactory
            .GetSnowWarScoreGrain()
            .GetTotalAsync(
                SnowWarLeaderboardRequests.Of(
                    ctx.PlayerId,
                    SnowWarLeaderboardScope.Everyone,
                    message.StartRank,
                    message.ViewSize,
                    message.WindowSize
                ),
                ct
            )
            .ConfigureAwait(false);

        if (result is null)
            return;

        await ctx.SendComposerAsync(
                new Game2TotalLeaderboardMessageComposer
                {
                    Entries = result.Entries,
                    TotalListSize = result.TotalListSize,
                    GameTypeId = message.GameTypeId,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
