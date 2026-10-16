using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Game.Ingame;

namespace Turbo.PacketHandlers.Game.Ingame;

public class Game2RequestFullStatusUpdateMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2RequestFullStatusUpdateMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2RequestFullStatusUpdateMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        if (await _grainFactory.GameOfAsync(ctx.PlayerId, ct).ConfigureAwait(false) is not { } game)
            return;

        await game.RequestFullStatusAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
