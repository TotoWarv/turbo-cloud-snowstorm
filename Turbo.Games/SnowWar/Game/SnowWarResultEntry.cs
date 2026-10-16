using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Game;

internal sealed record SnowWarResultEntry(
    SnowWarPlayerSnapshot Player,
    SnowWarPlayerStatsSnapshot Stats
);
