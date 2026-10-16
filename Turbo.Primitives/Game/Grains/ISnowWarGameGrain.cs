using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Game.Grains;

public interface ISnowWarGameGrain : IGrainWithIntegerKey
{
    public Task StartAsync(SnowWarGameLobbySnapshot lobby, CancellationToken ct);

    public Task StageReadyAsync(PlayerId playerId, CancellationToken ct);

    public Task QueueInputAsync(
        PlayerId playerId,
        SnowWarInput input,
        int[] values,
        int turn,
        int subturn,
        CancellationToken ct
    );

    public Task RequestFullStatusAsync(PlayerId playerId, CancellationToken ct);

    public Task ChatAsync(PlayerId playerId, string message, CancellationToken ct);

    public Task PlayAgainAsync(PlayerId playerId, CancellationToken ct);

    public Task ExitAsync(PlayerId playerId, CancellationToken ct);
}
