using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Primitives.Game.Grains;

public interface ISnowWarInputGrain : IGrainWithIntegerKey
{
    public Task QueueInputAsync(
        SnowWarInput input,
        int[] values,
        int turn,
        int subturn,
        CancellationToken ct
    );

    public Task RequestFullStatusAsync(CancellationToken ct);
}
