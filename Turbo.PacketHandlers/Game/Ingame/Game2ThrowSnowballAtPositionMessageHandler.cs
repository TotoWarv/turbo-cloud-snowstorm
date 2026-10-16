using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Messages.Incoming.Game.Ingame;

namespace Turbo.PacketHandlers.Game.Ingame;

public class Game2ThrowSnowballAtPositionMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2ThrowSnowballAtPositionMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2ThrowSnowballAtPositionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        if (await _grainFactory.GameOfAsync(ctx.PlayerId, ct).ConfigureAwait(false) is not { } game)
            return;

        await game.QueueInputAsync(
                ctx.PlayerId,
                SnowWarInput.ThrowAtPosition,
                [message.X, message.Y, message.Trajectory],
                message.Turn,
                message.Subturn,
                ct
            )
            .ConfigureAwait(false);
    }
}
