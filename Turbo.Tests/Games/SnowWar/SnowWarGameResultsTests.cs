using FluentAssertions;
using Turbo.Games.SnowWar.Game;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarGameResultsTests
{
    private static SnowWarResultEntry Entry(int id, int team, int score, int kills, int hits) =>
        new(
            new SnowWarPlayerSnapshot
            {
                UserId = id,
                Name = $"player{id}",
                Figure = "hd-180-1",
                Gender = "F",
                TeamId = team,
                SkillLevel = 1,
                TotalScore = 0,
                ScoreToNextLevel = 50,
            },
            new SnowWarPlayerStatsSnapshot
            {
                Score = score,
                Kills = kills,
                Deaths = 0,
                SnowballHits = hits,
                SnowballHitsTaken = 0,
                SnowballsThrown = 0,
                SnowballsCreated = 0,
                SnowballsFromMachine = 0,
                FriendlyHits = 0,
                FriendlyKills = 0,
            }
        );

    [Fact]
    public void the_team_with_the_highest_score_wins_even_with_points_of_players_who_left()
    {
        var ending = SnowWarGameResults.Build(
            30,
            [3, 9],
            [Entry(1, 1, 3, 0, 3), Entry(2, 2, 4, 0, 4)]
        );

        ending.ResultType.Should().Be(SnowWarGameResultType.Win);
        ending.WinnerId.Should().Be(2);
        ending.Teams.Select(t => (t.TeamId, t.Score)).Should().Equal((1, 3), (2, 9));
        ending.Teams[1].Players.Should().ContainSingle().Which.Score.Should().Be(4);
        ending.TimeToNextState.Should().Be(30);
    }

    [Fact]
    public void equal_top_scores_are_a_tie_without_winner()
    {
        var ending = SnowWarGameResults.Build(30, [5, 5], [Entry(1, 1, 5, 0, 5)]);

        ending.ResultType.Should().Be(SnowWarGameResultType.Tie);
        ending.WinnerId.Should().Be(0);
        ending.Teams[1].Players.Should().BeEmpty();
    }

    [Fact]
    public void best_killer_and_hitter_ties_go_to_the_first_participant_and_zero_means_nobody()
    {
        var ending = SnowWarGameResults.Build(
            30,
            [0, 0],
            [Entry(4, 2, 0, 2, 0), Entry(1, 1, 0, 2, 0), Entry(2, 1, 0, 1, 0)]
        );

        ending.PlayerWithMostKills.Should().Be(4);
        ending.PlayerWithMostHits.Should().Be(0);
        ending.Teams[0].Players.Select(p => p.UserId).Should().Equal(1, 2);
    }
}
