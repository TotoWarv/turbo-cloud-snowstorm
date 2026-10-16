using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

namespace Turbo.Games.SnowWar.Game;

internal static class SnowWarGameResults
{
    private const int NO_WINNER = 0;
    private const int NOBODY = 0;

    public static Game2GameEndingMessageComposer Build(
        int rematchSeconds,
        IReadOnlyList<int> teamScores,
        IReadOnlyList<SnowWarResultEntry> players
    )
    {
        var bestScore = teamScores.Max();
        var leaders = teamScores.Count(score => score == bestScore);
        var isTie = leaders > 1;

        return new Game2GameEndingMessageComposer
        {
            TimeToNextState = rematchSeconds,
            IsDeathMatch = false,
            ResultType = isTie ? SnowWarGameResultType.Tie : SnowWarGameResultType.Win,
            WinnerId = isTie ? NO_WINNER : IndexOfTeam(teamScores, bestScore),
            Teams = [.. teamScores.Select((score, index) => Team(index + 1, score, players))],
            PlayerWithMostKills = Best(players, stats => stats.Kills),
            PlayerWithMostHits = Best(players, stats => stats.SnowballHits),
        };
    }

    private static int IndexOfTeam(IReadOnlyList<int> teamScores, int score)
    {
        for (var index = 0; index < teamScores.Count; index++)
        {
            if (teamScores[index] == score)
                return index + 1;
        }

        return NO_WINNER;
    }

    private static SnowWarTeamScoreSnapshot Team(
        int teamId,
        int score,
        IReadOnlyList<SnowWarResultEntry> players
    ) =>
        new()
        {
            TeamId = teamId,
            Score = score,
            Players =
            [
                .. players
                    .Where(entry => entry.Player.TeamId == teamId)
                    .Select(entry => new SnowWarTeamPlayerSnapshot
                    {
                        UserName = entry.Player.Name,
                        UserId = entry.Player.UserId,
                        Figure = entry.Player.Figure,
                        Gender = entry.Player.Gender,
                        Score = entry.Stats.Score,
                        Stats = entry.Stats,
                    }),
            ],
        };

    private static int Best(
        IReadOnlyList<SnowWarResultEntry> players,
        Func<SnowWarPlayerStatsSnapshot, int> value
    )
    {
        var best = NOBODY;
        var bestValue = 0;

        foreach (var entry in players)
        {
            var current = value(entry.Stats);

            if (current > bestValue)
            {
                best = entry.Player.UserId;
                bestValue = current;
            }
        }

        return best;
    }
}
