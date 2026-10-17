using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Messages.Incoming.Game.Ingame;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Game.Ingame;

public class Game2SetUserMoveTargetMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2SetUserMoveTargetMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2SetUserMoveTargetMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        await _grainFactory
            .GetSnowWarInputGrain(ctx.PlayerId)
            .QueueInputAsync(
                SnowWarInput.MoveTarget,
                [message.X, message.Y],
                message.Turn,
                message.Subturn,
                ct
            )
            .ConfigureAwait(false);
    }
}
