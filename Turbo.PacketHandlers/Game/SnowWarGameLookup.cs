using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.PacketHandlers.Game;

internal static class SnowWarGameLookup
{
    public static async Task<ISnowWarGameGrain?> GameOfAsync(
        this IGrainFactory grainFactory,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        var gameId = await grainFactory
            .GetSnowWarDirectoryGrain()
            .GetGameIdOfPlayerAsync(playerId, ct)
            .ConfigureAwait(false);

        return gameId is { } id ? grainFactory.GetSnowWarGameGrain(id) : null;
    }
}
