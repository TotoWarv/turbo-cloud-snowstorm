using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Primitives.Game.Grains;

public interface ISnowWarGameGrain : IGrainWithIntegerKey
{
    public Task StartAsync(SnowWarGameLobbySnapshot lobby, CancellationToken ct);
}
