using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Directory;

internal static class SnowWarLobbyTeams
{
    public const int FIRST_TEAM = 1;
    public const int SECOND_TEAM = 2;
    public const int TEAM_COUNT = 2;

    public static int Smallest(ImmutableArray<SnowWarPlayerSnapshot> players) =>
        Count(players, SECOND_TEAM) < Count(players, FIRST_TEAM) ? SECOND_TEAM : FIRST_TEAM;

    public static ImmutableArray<SnowWarPlayerSnapshot> Rebalance(
        ImmutableArray<SnowWarPlayerSnapshot> players
    )
    {
        var balanced = players;

        while (Gap(balanced) > 1)
            balanced = MoveLastOfLargest(balanced);

        return balanced;
    }

    private static ImmutableArray<SnowWarPlayerSnapshot> MoveLastOfLargest(
        ImmutableArray<SnowWarPlayerSnapshot> players
    )
    {
        var largest =
            Count(players, FIRST_TEAM) > Count(players, SECOND_TEAM) ? FIRST_TEAM : SECOND_TEAM;
        var smallest = largest == FIRST_TEAM ? SECOND_TEAM : FIRST_TEAM;
        var index = players.LastIndexOf(players.Last(p => p.TeamId == largest));

        return players.SetItem(index, players[index] with { TeamId = smallest });
    }

    private static int Gap(ImmutableArray<SnowWarPlayerSnapshot> players) =>
        System.Math.Abs(Count(players, FIRST_TEAM) - Count(players, SECOND_TEAM));

    private static int Count(ImmutableArray<SnowWarPlayerSnapshot> players, int teamId) =>
        players.Count(p => p.TeamId == teamId);
}
