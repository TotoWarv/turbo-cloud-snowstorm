using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Messages.Incoming.Game.Ingame;

namespace Turbo.PacketHandlers.Game.Ingame;

public class Game2ThrowSnowballAtHumanMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2ThrowSnowballAtHumanMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2ThrowSnowballAtHumanMessage message,
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
                SnowWarInput.ThrowAtHuman,
                [message.TargetHumanId, message.Trajectory],
                message.Turn,
                message.Subturn,
                ct
            )
            .ConfigureAwait(false);
    }
}
