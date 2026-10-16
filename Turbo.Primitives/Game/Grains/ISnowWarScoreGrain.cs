using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Game.Grains;

public interface ISnowWarScoreGrain : IGrainWithIntegerKey
{
    public Task AddAsync(ImmutableArray<SnowWarScoreEntrySnapshot> scores, CancellationToken ct);

    public Task<SnowWarPlayerTotalsSnapshot> GetPlayerTotalsAsync(
        PlayerId playerId,
        CancellationToken ct
    );

    public Task<SnowWarLeaderboardPageSnapshot?> GetTotalAsync(
        SnowWarLeaderboardRequestSnapshot request,
        CancellationToken ct
    );

    public Task<SnowWarWeeklyLeaderboardPageSnapshot?> GetWeeklyAsync(
        SnowWarLeaderboardRequestSnapshot request,
        int weekOffset,
        CancellationToken ct
    );
}
