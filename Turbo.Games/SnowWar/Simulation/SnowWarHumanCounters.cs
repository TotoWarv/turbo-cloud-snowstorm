using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed class SnowWarHumanCounters
{
    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int Hits { get; set; }

    public int HitsTaken { get; set; }

    public int SnowballsThrown { get; set; }

    public int SnowballsMade { get; set; }

    public int SnowballsPicked { get; set; }

    public int FriendlyHits { get; set; }

    public SnowWarPlayerStatsSnapshot ToSnapshot(int score) =>
        new()
        {
            Score = score,
            Kills = Kills,
            Deaths = Deaths,
            SnowballHits = Hits,
            SnowballHitsTaken = HitsTaken,
            SnowballsThrown = SnowballsThrown,
            SnowballsCreated = SnowballsMade,
            SnowballsFromMachine = SnowballsPicked,
            FriendlyHits = FriendlyHits,
            FriendlyKills = 0,
        };
}
