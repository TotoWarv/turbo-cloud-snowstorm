using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Messages.Incoming.Game.Directory;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Game.Directory;

public class Game2CheckGameDirectoryStatusMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2CheckGameDirectoryStatusMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2CheckGameDirectoryStatusMessage message,
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

        if (status.Status == SnowWarDirectoryStatus.Unavailable)
        {
            await ctx.SendComposerAsync(
                    new Game2GameDirectoryStatusMessageMessageComposer
                    {
                        Status = SnowWarDirectoryStatus.Unavailable,
                        BlockSeconds = 0,
                        GamesPlayed = 0,
                        FreeGamesLeft = 0,
                    },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        var totals = await _grainFactory
            .GetSnowWarScoreGrain()
            .GetPlayerTotalsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new Game2GameDirectoryStatusMessageMessageComposer
                {
                    Status = status.Status,
                    BlockSeconds = status.BlockSeconds,
                    GamesPlayed = totals.GamesPlayed,
                    FreeGamesLeft = status.FreeGamesLeft,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
