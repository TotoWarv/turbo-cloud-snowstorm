using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game;
using Turbo.Primitives.Messages.Incoming.Game.Directory;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Game.Directory;

public class Game2GetAccountGameStatusMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2GetAccountGameStatusMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2GetAccountGameStatusMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        var status = await _grainFactory
            .GetSnowWarDirectoryGrain()
            .GetAccountStatusAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
        var gamesPlayed =
            message.GameTypeId == SnowWarGameTypes.SNOWWAR
                ? (
                    await _grainFactory
                        .GetSnowWarScoreGrain()
                        .GetPlayerTotalsAsync(ctx.PlayerId, ct)
                        .ConfigureAwait(false)
                ).GamesPlayed
                : 0;

        await ctx.SendComposerAsync(
                new Game2AccountGameStatusMessageMessageComposer
                {
                    GameTypeId = message.GameTypeId,
                    FreeGamesLeft = status.FreeGamesLeft,
                    GamesPlayedTotal = gamesPlayed,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
