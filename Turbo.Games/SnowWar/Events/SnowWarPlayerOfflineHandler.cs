using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Events.Registry;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Events;

namespace Turbo.Games.SnowWar.Events;

public sealed class SnowWarPlayerOfflineHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerOnlineChangedEvent>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        PlayerOnlineChangedEvent env,
        EventContext ctx,
        CancellationToken ct
    )
    {
        if (env.Online)
            return;

        var directory = _grainFactory.GetSnowWarDirectoryGrain();

        await directory.LeaveLobbyAsync(env.PlayerId, ct).ConfigureAwait(false);

        var gameId = await directory.GetGameIdOfPlayerAsync(env.PlayerId, ct).ConfigureAwait(false);

        if (gameId is { } id)
            await _grainFactory
                .GetSnowWarGameGrain(id)
                .ExitAsync(env.PlayerId, ct)
                .ConfigureAwait(false);
    }
}
