using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Game.Grains;

public interface ISnowWarDirectoryGrain : IGrainWithIntegerKey
{
    public Task<SnowWarAccountStatusSnapshot> GetAccountStatusAsync(
        PlayerId playerId,
        CancellationToken ct
    );

    public Task QuickJoinAsync(
        PlayerId playerId,
        SnowWarPlayerSnapshot player,
        CancellationToken ct
    );

    public Task LeaveLobbyAsync(PlayerId playerId, CancellationToken ct);

    public Task OnPlayerLeftGameAsync(
        int gameId,
        PlayerId playerId,
        bool duringPlay,
        CancellationToken ct
    );

    public Task OnGameFinishedAsync(int gameId, CancellationToken ct);

    public Task RematchAsync(
        int gameId,
        ImmutableArray<SnowWarPlayerSnapshot> players,
        CancellationToken ct
    );

    public Task<int?> GetGameIdOfPlayerAsync(PlayerId playerId, CancellationToken ct);
}
