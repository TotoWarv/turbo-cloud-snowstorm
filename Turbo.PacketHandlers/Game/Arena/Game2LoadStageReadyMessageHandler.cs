using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game;
using Turbo.Primitives.Messages.Incoming.Game.Arena;

namespace Turbo.PacketHandlers.Game.Arena;

public class Game2LoadStageReadyMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2LoadStageReadyMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2LoadStageReadyMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        if (
            message.Percentage is < SnowWarLoadingPercentage.NONE or > SnowWarLoadingPercentage.FULL
        )
            return;

        if (await _grainFactory.GameOfAsync(ctx.PlayerId, ct).ConfigureAwait(false) is not { } game)
            return;

        await game.StageReadyAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
