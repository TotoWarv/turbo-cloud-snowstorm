using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Entities.Game;
using Turbo.Database.Entities.Players;
using Turbo.Games;
using Turbo.Games.Configuration;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Messenger;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarScoreGrainTests
{
    private const string GRAIN_TYPE = "Turbo.Games.Grains.SnowWarScoreGrain";
    private const string FLUSH_METHOD = "OnFlushTimerAsync";

    private readonly Fakes _fakes = new();
    private readonly InMemoryDb _db = new();
    private readonly ManualTimeProvider _clock = new(
        new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero)
    );

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ISnowWarScoreGrain NewGrain(SnowWarConfig? config = null)
    {
        var grain = GrainHarness.Create(typeof(GamesModule).Assembly, GRAIN_TYPE, _fakes, _db);

        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", config ?? new SnowWarConfig());

        return (ISnowWarScoreGrain)grain;
    }

    private static Task FlushAsync(ISnowWarScoreGrain grain) =>
        (Task)
            grain
                .GetType()
                .GetMethod(FLUSH_METHOD, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grain, [Ct])!;

    private void AddPlayers(int from, int to)
    {
        using var ctx = _db.CreateDbContext();

        for (var id = from; id <= to; id++)
            ctx.Players.Add(
                new PlayerEntity
                {
                    Id = id,
                    Name = $"player{id}",
                    Figure = $"hd-{id}",
                    Gender = id % 2 == 0 ? AvatarGenderType.Female : AvatarGenderType.Male,
                    PlayerStatus = PlayerStatusType.Offline,
                }
            );

        ctx.SaveChanges();
    }

    private static SnowWarScoreEntrySnapshot Score(int playerId, int score) =>
        new() { PlayerId = playerId, Score = score };

    private static SnowWarLeaderboardRequestSnapshot Request(
        int playerId = 1,
        SnowWarLeaderboardScope scope = SnowWarLeaderboardScope.Everyone,
        int startRank = 1,
        int viewSize = 8,
        int windowSize = 50
    ) =>
        new()
        {
            PlayerId = playerId,
            Scope = scope,
            StartRank = startRank,
            ViewSize = viewSize,
            WindowSize = windowSize,
        };

    [Fact]
    public async Task weekly_leaderboard_lists_scores_by_rank_with_shared_ranks_for_ties()
    {
        AddPlayers(1, 4);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 10), Score(2, 30), Score(3, 10), Score(4, 5)], Ct);

        var weekly = await grain.GetWeeklyAsync(Request(), 0, Ct);

        weekly!
            .Page.Entries.Select(e => (e.UserId, e.Score, e.Rank))
            .Should()
            .Equal((2, 30, 1), (1, 10, 2), (3, 10, 2), (4, 5, 4));
        weekly.Page.TotalListSize.Should().Be(4);
        weekly.Page.Entries[0].Name.Should().Be("player2");
        weekly.Page.Entries[0].Figure.Should().Be("hd-2");
        weekly.Page.Entries[0].Gender.Should().Be("F");
        weekly.Year.Should().Be(2026);
        weekly.Week.Should().Be(42);
        weekly.CurrentOffset.Should().Be(0);
        weekly.MaxOffset.Should().Be(0);
    }

    [Fact]
    public async Task scores_of_the_same_week_add_up_and_count_matches()
    {
        AddPlayers(1, 1);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 10)], Ct);
        await grain.AddAsync([Score(1, 7)], Ct);
        await FlushAsync(grain);

        using var ctx = _db.CreateDbContext();
        var row = await ctx.SnowWarScores.SingleAsync(Ct);

        row.PlayerId.Should().Be(1);
        row.Score.Should().Be(17);
        row.Matches.Should().Be(2);
        row.WeekStart.Should().Be(new DateOnly(2026, 10, 12));
    }

    [Fact]
    public async Task a_negative_score_counts_as_zero_and_is_not_ranked()
    {
        AddPlayers(1, 2);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, -4), Score(2, 3)], Ct);

        var weekly = await grain.GetWeeklyAsync(Request(), 0, Ct);
        var totals = await grain.GetPlayerTotalsAsync(1, Ct);

        weekly!.Page.Entries.Select(e => e.UserId).Should().Equal(2);
        totals.TotalScore.Should().Be(0);
        totals.GamesPlayed.Should().Be(1);
    }

    [Fact]
    public async Task the_week_turns_on_monday_at_midnight_utc()
    {
        AddPlayers(1, 2);
        var grain = NewGrain();

        _clock.Set(new DateTime(2026, 10, 18, 23, 59, 0, DateTimeKind.Utc));
        await grain.AddAsync([Score(1, 10)], Ct);

        var sunday = await grain.GetWeeklyAsync(Request(), 0, Ct);
        sunday!.MinutesUntilReset.Should().Be(1);
        sunday.Week.Should().Be(42);

        _clock.Set(new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc));
        await grain.AddAsync([Score(2, 20)], Ct);

        var current = await grain.GetWeeklyAsync(Request(playerId: 2), 0, Ct);
        var previous = await grain.GetWeeklyAsync(Request(playerId: 3), 1, Ct);

        current!.Page.Entries.Select(e => e.UserId).Should().Equal(2);
        current.Week.Should().Be(43);
        current.MaxOffset.Should().Be(1);
        current.MinutesUntilReset.Should().Be(7 * 24 * 60);
        previous!.Page.Entries.Select(e => e.UserId).Should().Equal(1);
        previous.Week.Should().Be(42);
        previous.CurrentOffset.Should().Be(1);
    }

    [Fact]
    public async Task an_aberrant_week_offset_is_clamped_to_the_recorded_weeks()
    {
        AddPlayers(1, 1);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 10)], Ct);

        var far = await grain.GetWeeklyAsync(Request(playerId: 1), 999, Ct);
        var negative = await grain.GetWeeklyAsync(Request(playerId: 2), -5, Ct);

        far!.CurrentOffset.Should().Be(0);
        far.Page.Entries.Should().ContainSingle();
        negative!.CurrentOffset.Should().Be(0);
    }

    [Fact]
    public async Task the_total_leaderboard_sums_every_week()
    {
        AddPlayers(1, 2);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 10), Score(2, 15)], Ct);
        _clock.Advance(TimeSpan.FromDays(7));
        await grain.AddAsync([Score(1, 10)], Ct);

        var total = await grain.GetTotalAsync(Request(), Ct);
        var totals = await grain.GetPlayerTotalsAsync(1, Ct);

        total!.Entries.Select(e => (e.UserId, e.Score)).Should().Equal((1, 20), (2, 15));
        totals.TotalScore.Should().Be(20);
        totals.GamesPlayed.Should().Be(2);
        totals.SkillLevel.Should().Be(1);
        totals.ScoreToNextLevel.Should().Be(30);
    }

    [Fact]
    public async Task the_skill_level_follows_the_total_score()
    {
        AddPlayers(1, 1);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 200)], Ct);

        var totals = await grain.GetPlayerTotalsAsync(1, Ct);

        totals.SkillLevel.Should().Be(3);
        totals.ScoreToNextLevel.Should().Be(250);
    }

    [Fact]
    public async Task pages_start_at_the_requested_rank_and_never_exceed_the_row_limit()
    {
        AddPlayers(1, 60);
        var grain = NewGrain();

        await grain.AddAsync([.. Enumerable.Range(1, 60).Select(id => Score(id, 1000 - id))], Ct);

        var first = await grain.GetTotalAsync(Request(playerId: 1, windowSize: 500), Ct);
        var second = await grain.GetTotalAsync(Request(playerId: 2, startRank: 51), Ct);
        var small = await grain.GetTotalAsync(
            Request(playerId: 3, startRank: 0, viewSize: 8, windowSize: 0),
            Ct
        );
        var negative = await grain.GetTotalAsync(
            Request(playerId: 4, startRank: -20, viewSize: -3, windowSize: -3),
            Ct
        );

        first!.Entries.Should().HaveCount(50);
        first.TotalListSize.Should().Be(60);
        second!.Entries.Select(e => e.Rank).Should().Equal(Enumerable.Range(51, 10));
        small!.Entries.Select(e => e.Rank).Should().Equal(Enumerable.Range(1, 8));
        negative!.Entries.Select(e => e.Rank).Should().Equal(1);
    }

    [Fact]
    public async Task rank_minus_one_centres_the_page_on_the_requester()
    {
        AddPlayers(1, 60);
        var grain = NewGrain();

        await grain.AddAsync([.. Enumerable.Range(1, 60).Select(id => Score(id, 1000 - id))], Ct);

        var around = await grain.GetTotalAsync(
            Request(playerId: 30, startRank: -1, viewSize: 8, windowSize: 0),
            Ct
        );
        var unranked = await grain.GetTotalAsync(
            Request(playerId: 99, startRank: -1, viewSize: 8, windowSize: 0),
            Ct
        );

        around!.Entries.Select(e => e.Rank).Should().Equal(Enumerable.Range(26, 8));
        unranked!.Entries.Select(e => e.Rank).Should().Equal(Enumerable.Range(1, 8));
    }

    [Fact]
    public async Task the_friends_leaderboard_ranks_the_requester_and_friends_only()
    {
        AddPlayers(1, 4);
        _fakes.Handlers["GetFriendsAsync"] = call =>
            Task.FromResult(
                Convert.ToInt64(call.Key) == 1
                    ? new List<MessengerFriendDto> { new() { PlayerId = 3 } }
                    : []
            );
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 5), Score(2, 50), Score(3, 20), Score(4, 40)], Ct);

        var friends = await grain.GetTotalAsync(
            Request(scope: SnowWarLeaderboardScope.Friends),
            Ct
        );

        friends!.Entries.Select(e => (e.UserId, e.Rank)).Should().Equal((3, 1), (1, 2));
        friends.TotalListSize.Should().Be(2);
    }

    [Fact]
    public async Task group_leaderboards_are_answered_empty()
    {
        AddPlayers(1, 1);
        var grain = NewGrain();

        await grain.AddAsync([Score(1, 5)], Ct);

        var groups = await grain.GetWeeklyAsync(
            Request(scope: SnowWarLeaderboardScope.Groups),
            0,
            Ct
        );

        groups!.Page.Entries.Should().BeEmpty();
        groups.Page.TotalListSize.Should().Be(0);
        groups.Week.Should().Be(42);
    }

    [Fact]
    public async Task one_answer_per_second_per_player_and_kind()
    {
        AddPlayers(1, 1);
        var grain = NewGrain();

        (await grain.GetTotalAsync(Request(), Ct)).Should().NotBeNull();
        (await grain.GetTotalAsync(Request(), Ct)).Should().BeNull();
        (await grain.GetWeeklyAsync(Request(), 0, Ct)).Should().NotBeNull();
        (await grain.GetTotalAsync(Request(scope: SnowWarLeaderboardScope.Friends), Ct))
            .Should()
            .NotBeNull();

        _clock.Advance(TimeSpan.FromSeconds(1));

        (await grain.GetTotalAsync(Request(), Ct)).Should().NotBeNull();
    }

    [Fact]
    public async Task a_failed_flush_keeps_the_scores_for_the_next_one()
    {
        var failing = new FailingDb();
        var grain = GrainHarness.Create(typeof(GamesModule).Assembly, GRAIN_TYPE, _fakes, failing);
        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", new SnowWarConfig());
        var scores = (ISnowWarScoreGrain)grain;

        await scores.AddAsync([Score(1, 10)], Ct);
        await FlushAsync(scores);

        failing.Fail = false;
        await FlushAsync(scores);

        using var ctx = failing.CreateDbContext();
        (await ctx.SnowWarScores.SingleAsync(Ct)).Score.Should().Be(10);
    }

    private sealed class FailingDb : IDbContextFactory<Turbo.Database.Context.TurboDbContext>
    {
        private readonly InMemoryDb _inner = new();

        public bool Fail { get; set; } = true;

        public Turbo.Database.Context.TurboDbContext CreateDbContext() =>
            Fail ? throw new InvalidOperationException("database down") : _inner.CreateDbContext();
    }
}
