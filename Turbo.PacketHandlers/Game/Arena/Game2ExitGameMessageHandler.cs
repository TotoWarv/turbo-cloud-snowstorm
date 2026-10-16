using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Game.Arena;

namespace Turbo.PacketHandlers.Game.Arena;

public class Game2ExitGameMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2ExitGameMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2ExitGameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        if (await _grainFactory.GameOfAsync(ctx.PlayerId, ct).ConfigureAwait(false) is not { } game)
            return;

        await game.ExitAsync(ctx.PlayerId, ct).ConfigureAwait(false);
    }
}
