using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Events.Registry;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Events;

namespace Turbo.Games.SnowWar.Events;

public sealed class SnowWarPlayerOfflineHandler(
    IGrainFactory grainFactory,
    ILogger<SnowWarPlayerOfflineHandler> logger
) : IEventHandler<PlayerOnlineChangedEvent>
{
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<SnowWarPlayerOfflineHandler> _logger = logger;

    public async ValueTask HandleAsync(
        PlayerOnlineChangedEvent env,
        EventContext ctx,
        CancellationToken ct
    )
    {
        if (env.Online)
            return;

        await LeaveLobbyAsync(env.PlayerId, ct).ConfigureAwait(false);
        await ExitGameAsync(env.PlayerId, ct).ConfigureAwait(false);
    }

    private async Task LeaveLobbyAsync(PlayerId playerId, CancellationToken ct)
    {
        try
        {
            await _grainFactory
                .GetSnowWarDirectoryGrain()
                .LeaveLobbyAsync(playerId, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to take disconnected player {PlayerId} out of their SnowWar lobby",
                playerId
            );
        }
    }

    private async Task ExitGameAsync(PlayerId playerId, CancellationToken ct)
    {
        try
        {
            var gameId = await _grainFactory
                .GetSnowWarDirectoryGrain()
                .GetGameIdOfPlayerAsync(playerId, ct)
                .ConfigureAwait(false);

            if (gameId is { } id)
                await _grainFactory
                    .GetSnowWarGameGrain(id)
                    .ExitAsync(playerId, ct)
                    .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to take disconnected player {PlayerId} out of their SnowWar game",
                playerId
            );
        }
    }
}
