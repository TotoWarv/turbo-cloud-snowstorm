using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
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

    private ISnowWarScoreGrain NewGrain(
        SnowWarConfig? config = null,
        IDbContextFactory<Turbo.Database.Context.TurboDbContext>? db = null,
        CapturingLogger<ISnowWarScoreGrain>? logger = null
    )
    {
        var grain = GrainHarness.Create(
            typeof(GamesModule).Assembly,
            GRAIN_TYPE,
            _fakes,
            db ?? _db
        );

        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", config ?? new SnowWarConfig());

        if (logger is not null)
            RoomHarness.SetField(grain, "_logger", logger);

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
        var db = new ObservedDb { Fail = true };
        var scores = NewGrain(db: db);

        await scores.AddAsync([Score(1, 10)], Ct);
        await FlushAsync(scores);

        db.Fail = false;
        await FlushAsync(scores);

        using var ctx = db.CreateDbContext();
        (await ctx.SnowWarScores.SingleAsync(Ct)).Score.Should().Be(10);
    }

    [Fact]
    public async Task repeated_totals_of_one_player_query_the_database_once()
    {
        var db = new ObservedDb();
        var scores = NewGrain(db: db);

        for (var call = 0; call < 100; call++)
            await scores.GetPlayerTotalsAsync(1, Ct);

        db.Created.Should().Be(1);
    }

    [Fact]
    public async Task totals_show_a_finished_game_before_it_is_written()
    {
        var db = new ObservedDb();
        var scores = NewGrain(db: db);

        (await scores.GetPlayerTotalsAsync(1, Ct)).TotalScore.Should().Be(0);

        await scores.AddAsync([Score(1, 60)], Ct);

        var totals = await scores.GetPlayerTotalsAsync(1, Ct);

        totals.TotalScore.Should().Be(60);
        totals.GamesPlayed.Should().Be(1);
        totals.SkillLevel.Should().Be(2);
        db.Created.Should().Be(1);

        await FlushAsync(scores);

        (await scores.GetPlayerTotalsAsync(1, Ct)).TotalScore.Should().Be(60);
        (await scores.GetPlayerTotalsAsync(1, Ct)).GamesPlayed.Should().Be(1);
    }

    [Fact]
    public async Task cached_totals_expire_after_the_configured_time()
    {
        var db = new ObservedDb();
        var config = new SnowWarConfig();
        var scores = NewGrain(config, db);

        await scores.GetPlayerTotalsAsync(1, Ct);
        _clock.Advance(TimeSpan.FromSeconds(config.PlayerTotalsCacheSeconds));
        await scores.GetPlayerTotalsAsync(1, Ct);

        db.Created.Should().Be(2);
    }

    [Fact]
    public async Task many_players_asking_for_the_same_board_scan_the_scores_once()
    {
        AddPlayers(1, 30);
        var db = new ObservedDb(_db);
        var scores = NewGrain(db: db);

        await scores.AddAsync([.. Enumerable.Range(1, 30).Select(id => Score(id, id))], Ct);
        await scores.GetWeeklyAsync(Request(playerId: 1), 0, Ct);
        await scores.GetTotalAsync(Request(playerId: 1), Ct);

        var afterFirst = db.Created;

        for (var id = 2; id <= 30; id++)
        {
            var weekly = await scores.GetWeeklyAsync(Request(playerId: id), 0, Ct);
            var total = await scores.GetTotalAsync(Request(playerId: id), Ct);

            weekly!.Page.Entries.Select(e => e.UserId).Should().StartWith([30, 29, 28]);
            total!.Entries[0].Name.Should().Be("player30");
        }

        db.Created.Should().Be(afterFirst);
    }

    [Fact]
    public async Task a_flush_refreshes_the_cached_boards()
    {
        AddPlayers(1, 2);
        var scores = NewGrain();

        await scores.AddAsync([Score(1, 5)], Ct);
        (await scores.GetTotalAsync(Request(playerId: 1), Ct))!
            .Entries.Select(e => e.UserId)
            .Should()
            .Equal(1);

        await scores.AddAsync([Score(2, 9)], Ct);

        (await scores.GetTotalAsync(Request(playerId: 2), Ct))!
            .Entries.Select(e => (e.UserId, e.Rank))
            .Should()
            .Equal((2, 1), (1, 2));
    }

    [Fact]
    public async Task cached_boards_keep_shared_ranks_for_ties()
    {
        AddPlayers(1, 3);
        var scores = NewGrain();

        await scores.AddAsync([Score(1, 7), Score(2, 7), Score(3, 2)], Ct);
        await scores.GetTotalAsync(Request(playerId: 1), Ct);

        (await scores.GetTotalAsync(Request(playerId: 2), Ct))!
            .Entries.Select(e => e.Rank)
            .Should()
            .Equal(1, 1, 3);
    }

    [Fact]
    public async Task hostile_page_sizes_and_ranks_stay_within_bounds()
    {
        AddPlayers(1, 60);
        var scores = NewGrain();

        await scores.AddAsync([.. Enumerable.Range(1, 60).Select(id => Score(id, 1000 - id))], Ct);

        var lowest = await scores.GetTotalAsync(
            Request(
                playerId: 1,
                startRank: int.MinValue,
                viewSize: int.MaxValue,
                windowSize: int.MaxValue
            ),
            Ct
        );
        var centred = await scores.GetTotalAsync(
            Request(playerId: 30, startRank: -1, viewSize: int.MaxValue, windowSize: int.MinValue),
            Ct
        );
        var beyond = await scores.GetTotalAsync(
            Request(playerId: 2, startRank: int.MaxValue, viewSize: int.MaxValue, windowSize: 0),
            Ct
        );

        lowest!.Entries.Select(e => e.Rank).Should().Equal(Enumerable.Range(1, 50));
        centred!.Entries.Select(e => e.Rank).Should().Equal(Enumerable.Range(1, 50));
        beyond!.Entries.Should().BeEmpty();
        beyond.TotalListSize.Should().Be(60);
    }

    [Fact]
    public async Task deactivation_writes_what_is_still_queued()
    {
        var scores = NewGrain();

        await scores.AddAsync([Score(1, 10)], Ct);
        await ((Grain)scores).OnDeactivateAsync(Deactivation(), Ct);

        using var ctx = _db.CreateDbContext();
        (await ctx.SnowWarScores.SingleAsync(Ct)).Score.Should().Be(10);
    }

    [Fact]
    public async Task deactivation_ignores_a_cancelled_token_and_says_when_scores_are_lost()
    {
        var logger = new CapturingLogger<ISnowWarScoreGrain>();
        var db = new ObservedDb();
        var scores = NewGrain(db: db, logger: logger);
        using var cancelled = new CancellationTokenSource();

        await scores.AddAsync([Score(1, 10)], Ct);
        await cancelled.CancelAsync();
        await ((Grain)scores).OnDeactivateAsync(Deactivation(), cancelled.Token);

        using (var ctx = db.CreateDbContext())
            (await ctx.SnowWarScores.SingleAsync(Ct)).Score.Should().Be(10);

        var failing = new ObservedDb { Fail = true };
        var lost = NewGrain(db: failing, logger: logger);

        await lost.AddAsync([Score(2, 3)], Ct);
        await ((Grain)lost).OnDeactivateAsync(Deactivation(), Ct);

        logger
            .AtLeast(LogLevel.Error)
            .Should()
            .ContainSingle()
            .Which.Message.Should()
            .Contain("lost")
            .And.NotContain("stay queued");
    }

    [Fact]
    public async Task an_overflowing_queue_keeps_the_newest_scores_and_logs_once()
    {
        var logger = new CapturingLogger<ISnowWarScoreGrain>();
        var scores = NewGrain(new SnowWarConfig { MaxPendingScores = 2 }, logger: logger);

        await scores.AddAsync([.. Enumerable.Range(1, 5).Select(id => Score(id, id))], Ct);
        await FlushAsync(scores);

        using var ctx = _db.CreateDbContext();
        (await ctx.SnowWarScores.Select(s => s.PlayerId).OrderBy(id => id).ToListAsync(Ct))
            .Should()
            .Equal(4, 5);
        logger
            .AtLeast(LogLevel.Warning)
            .Should()
            .ContainSingle()
            .Which.Message.Should()
            .Contain("3");
    }

    private static DeactivationReason Deactivation() =>
        new(DeactivationReasonCode.ShuttingDown, "test");

    private sealed class ObservedDb(InMemoryDb? inner = null)
        : IDbContextFactory<Turbo.Database.Context.TurboDbContext>
    {
        private readonly InMemoryDb _inner = inner ?? new InMemoryDb();

        public bool Fail { get; set; }

        public int Created { get; private set; }

        public Turbo.Database.Context.TurboDbContext CreateDbContext()
        {
            if (Fail)
                throw new InvalidOperationException("database down");

            Created++;

            return _inner.CreateDbContext();
        }
    }
}
