using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Game.Directory;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Game.Directory;

public class Game2LeaveGameMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<Game2LeaveGameMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        Game2LeaveGameMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        await _grainFactory
            .GetSnowWarDirectoryGrain()
            .LeaveLobbyAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);
    }
}
