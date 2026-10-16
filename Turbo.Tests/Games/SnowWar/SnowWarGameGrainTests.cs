using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Turbo.Games;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Snapshots;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarGameGrainTests
{
    private const string GRAIN_TYPE = "Turbo.Games.Grains.SnowWarGameGrain";
    private const string TICK_METHOD = "OnGameTimerAsync";
    private const int GAME_ID = 7;
    private const int USER_ID_VARIABLE = 16;
    private const int X_VARIABLE = 0;
    private const int Y_VARIABLE = 1;

    private readonly Fakes _fakes = new();
    private readonly ManualTimeProvider _clock = new(
        new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)
    );

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ISnowWarGameGrain NewGame(SnowWarConfig? config = null)
    {
        var settings = config ?? new SnowWarConfig();
        var grain = GrainHarness.Create(typeof(GamesModule).Assembly, GRAIN_TYPE, _fakes);

        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", settings);
        RoomHarness.SetField(grain, "_arenas", new SnowWarArenaCatalog(Options.Create(settings)));

        return (ISnowWarGameGrain)grain;
    }

    private static Task TickAsync(ISnowWarGameGrain game) =>
        (Task)
            game.GetType()
                .GetMethod(TICK_METHOD, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, [Ct])!;

    private static SnowWarGameLobbySnapshot Lobby(int playerCount, int fieldType = 8) =>
        new()
        {
            GameId = GAME_ID,
            LevelName = "arena",
            GameType = 0,
            FieldType = fieldType,
            NumberOfTeams = 2,
            MaximumPlayers = 8,
            OwningPlayerName = "player1",
            LevelEntryId = fieldType,
            Players =
            [
                .. Enumerable
                    .Range(1, playerCount)
                    .Select(id => new SnowWarPlayerSnapshot
                    {
                        UserId = id,
                        Name = $"player{id}",
                        Figure = "hd-180-1",
                        Gender = "M",
                        TeamId = id % 2 == 1 ? 1 : 2,
                        SkillLevel = 1,
                        TotalScore = 0,
                        ScoreToNextLevel = 50,
                    }),
            ],
        };

    private void PlayersComeFromRooms()
    {
        _fakes.Handlers[nameof(IPlayerPresenceGrain.GetActiveRoomAsync)] = call =>
            Task.FromResult(
                new RoomPointerSnapshot
                {
                    RoomId = 100 + (int)Convert.ToInt64(call.Key),
                    ActiveSinceUtc = DateTime.UtcNow,
                }
            );
    }

    private IEnumerable<object> SentTo(int playerId) =>
        _fakes
            .Log.Of(nameof(IPlayerPresenceGrain.SendComposerAsync))
            .Where(call =>
                call.Interface == typeof(IPlayerPresenceGrain)
                && Convert.ToInt64(call.Key) == playerId
            )
            .Select(call => call.Args[0]!);

    private IEnumerable<T> Sent<T>(int playerId) => SentTo(playerId).OfType<T>();

    private IEnumerable<FakeCall> DirectoryCalls(string method) =>
        _fakes.Log.Of(method).Where(call => call.Interface == typeof(ISnowWarDirectoryGrain));

    private IEnumerable<FakeCall> DeactivationRequests() =>
        _fakes.Log.Calls.Where(call =>
            call.Method.Contains("Deactivate", StringComparison.Ordinal)
        );

    private static async Task ReadyAllAsync(ISnowWarGameGrain game, int playerCount)
    {
        for (var id = 1; id <= playerCount; id++)
            await game.StageReadyAsync(id, Ct);
    }

    private async Task<ISnowWarGameGrain> RunningGameAsync(int playerCount)
    {
        var game = NewGame();

        await game.StartAsync(Lobby(playerCount), Ct);
        await ReadyAllAsync(game, playerCount);
        _clock.Advance(TimeSpan.FromSeconds(5));
        await TickAsync(game);

        return game;
    }

    private async Task PlayToTheEndAsync(ISnowWarGameGrain game)
    {
        _clock.Advance(TimeSpan.FromSeconds(180));

        for (var tick = 0; tick < 130; tick++)
            await TickAsync(game);
    }

    private SnowWarGameObjectSnapshot HumanOf(int playerId) =>
        Sent<Game2StageStartingMessageComposer>(playerId)
            .Single()
            .Objects.Single(o =>
                o.Type == SnowWarGameObjectType.Human && o.Variables[USER_ID_VARIABLE] == playerId
            );

    [Fact]
    public async Task starting_enters_the_arena_and_asks_every_client_to_load()
    {
        var game = NewGame();

        await game.StartAsync(Lobby(2), Ct);

        var enter = Sent<Game2EnterArenaMessageComposer>(1).Should().ContainSingle().Which;
        enter.FieldType.Should().Be(8);
        enter.NumberOfTeams.Should().Be(2);
        enter.Players.Select(p => (p.ReferenceId, p.TeamId)).Should().Equal((1, 1), (2, 2));
        enter.Level.Width.Should().Be(50);
        enter.Level.HeightMap.Should().Contain("\r");
        enter.Level.FuseObjects.Should().HaveCount(103);
        enter.Level.FuseObjects[^1].Name.Should().Be("ads_background");
        enter.Level.FuseObjects[^1].CanStandOn.Should().BeTrue();
        Sent<Game2ArenaEnteredMessageComposer>(2)
            .Select(m => m.Player.ReferenceId)
            .Should()
            .Equal(1, 2);
        Sent<Game2StageLoadMessageComposer>(2).Should().ContainSingle();
        SentTo(1)
            .Select(m => m.GetType())
            .Should()
            .Equal(
                typeof(Game2EnterArenaMessageComposer),
                typeof(Game2ArenaEnteredMessageComposer),
                typeof(Game2ArenaEnteredMessageComposer),
                typeof(Game2StageLoadMessageComposer)
            );
    }

    [Fact]
    public async Task an_unknown_arena_fails_the_start()
    {
        var game = NewGame();

        var start = () => game.StartAsync(Lobby(2, fieldType: 42), Ct);

        await start.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task two_ready_players_get_the_stage_countdown_then_the_running_stage_after_five_seconds()
    {
        var game = NewGame();
        await game.StartAsync(Lobby(2), Ct);

        await game.StageReadyAsync(1, Ct);

        var loading = Sent<Game2StageStillLoadingMessageComposer>(2).Should().ContainSingle().Which;
        loading.Percentage.Should().Be(50);
        loading.FinishedUserIds.Should().Equal(1);
        Sent<Game2StageStartingMessageComposer>(1).Should().BeEmpty();

        await game.StageReadyAsync(1, Ct);
        await game.StageReadyAsync(2, Ct);

        Sent<Game2StageStillLoadingMessageComposer>(1)
            .Select(m => m.Percentage)
            .Should()
            .Equal(50, 100);
        var starting = Sent<Game2StageStartingMessageComposer>(1).Should().ContainSingle().Which;
        starting.CountDown.Should().Be(5);
        starting.RoomType.Should().Be("snowwar");
        starting.Objects.Count(o => o.Type == SnowWarGameObjectType.Human).Should().Be(2);

        _clock.Advance(TimeSpan.FromSeconds(4.9));
        await TickAsync(game);

        Sent<Game2StageRunningMessageComposer>(1).Should().BeEmpty();

        _clock.Advance(TimeSpan.FromSeconds(0.1));
        await TickAsync(game);

        Sent<Game2StageRunningMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.TimeToStageEnd.Should()
            .Be(180);
        Sent<Game2GameStatusMessageComposer>(2).Should().ContainSingle().Which.Turn.Should().Be(0);
        SentTo(1)
            .SkipWhile(m => m is not Game2StageRunningMessageComposer)
            .Skip(1)
            .First()
            .Should()
            .BeOfType<Game2GameStatusMessageComposer>();
    }

    [Fact]
    public async Task a_player_who_does_not_load_within_ten_seconds_does_not_hold_the_stage()
    {
        var game = NewGame();
        await game.StartAsync(Lobby(2), Ct);
        await game.StageReadyAsync(1, Ct);

        _clock.Advance(TimeSpan.FromSeconds(9.9));
        await TickAsync(game);

        Sent<Game2StageStartingMessageComposer>(1).Should().BeEmpty();

        _clock.Advance(TimeSpan.FromSeconds(0.1));
        await TickAsync(game);

        Sent<Game2StageStartingMessageComposer>(1).Should().ContainSingle();
        Sent<Game2StageStartingMessageComposer>(2).Should().ContainSingle();
    }

    [Fact]
    public async Task a_late_clock_catches_up_at_most_ten_turns_per_tick()
    {
        var game = await RunningGameAsync(2);

        _clock.Advance(TimeSpan.FromSeconds(3));
        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(1)
            .Select(m => m.Turn)
            .Should()
            .Equal(Enumerable.Range(0, 11));

        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(1)
            .Select(m => m.Turn)
            .Should()
            .Equal(Enumerable.Range(0, 21));

        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(1).Should().HaveCount(21);
    }

    [Fact]
    public async Task twelve_hundred_turns_make_three_minutes_and_end_with_the_team_scores()
    {
        var game = await RunningGameAsync(2);

        _clock.Advance(TimeSpan.FromSeconds(179.7));

        for (var tick = 0; tick < 130; tick++)
            await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(1).Should().HaveCount(1199);
        Sent<Game2GameEndingMessageComposer>(1).Should().BeEmpty();

        _clock.Advance(TimeSpan.FromSeconds(0.15));
        await TickAsync(game);
        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(1)
            .Select(m => m.Turn)
            .Should()
            .Equal(Enumerable.Range(0, 1200));
        Sent<Game2StageEndingMessageComposer>(1).Should().ContainSingle();
        var ending = Sent<Game2GameEndingMessageComposer>(2).Should().ContainSingle().Which;
        ending.TimeToNextState.Should().Be(30);
        ending.IsDeathMatch.Should().BeFalse();
        ending.ResultType.Should().Be(SnowWarGameResultType.Tie);
        ending.WinnerId.Should().Be(0);
        ending.Teams.Select(t => (t.TeamId, t.Score)).Should().Equal((1, 0), (2, 0));
        ending.Teams[0].Players.Select(p => p.UserId).Should().Equal(1);
        ending.Teams[1].Players.Select(p => p.UserId).Should().Equal(2);
        ending.PlayerWithMostKills.Should().Be(0);
        ending.PlayerWithMostHits.Should().Be(0);
        SentTo(1)
            .SkipWhile(m => m is not Game2StageEndingMessageComposer)
            .Skip(1)
            .First()
            .Should()
            .BeOfType<Game2GameEndingMessageComposer>();
        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnGameFinishedAsync))
            .Should()
            .ContainSingle()
            .Which.Args[0]
            .Should()
            .Be(GAME_ID);
    }

    [Fact]
    public async Task the_end_of_the_game_records_the_score_of_every_player_still_there()
    {
        var game = await RunningGameAsync(3);

        await game.ExitAsync(3, Ct);
        await PlayToTheEndAsync(game);

        var recorded = _fakes
            .Log.Of(nameof(ISnowWarScoreGrain.AddAsync))
            .Where(call => call.Interface == typeof(ISnowWarScoreGrain))
            .Should()
            .ContainSingle()
            .Which.Args[0];

        ((ImmutableArray<SnowWarScoreEntrySnapshot>)recorded!)
            .Select(entry => (entry.PlayerId.Value, entry.Score))
            .Should()
            .Equal((1, 0), (2, 0));
    }

    [Fact]
    public async Task a_player_leaving_during_play_is_announced_and_the_game_goes_on()
    {
        PlayersComeFromRooms();
        var game = await RunningGameAsync(3);
        var leaver = HumanOf(3);

        await game.ExitAsync(3, Ct);

        foreach (var other in new[] { 1, 2 })
        {
            Sent<Game2PlayerExitedGameArenaMessageComposer>(other)
                .Should()
                .ContainSingle()
                .Which.Should()
                .Match<Game2PlayerExitedGameArenaMessageComposer>(m =>
                    m.UserId == 3 && m.PlayerGameObjectId == leaver.Id
                );
        }

        Sent<Game2PlayerExitedGameArenaMessageComposer>(3).Should().BeEmpty();
        Sent<Game2GameRejoinMessageComposer>(3)
            .Should()
            .ContainSingle()
            .Which.RoomId.Should()
            .Be(103);
        var left = DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnPlayerLeftGameAsync))
            .Should()
            .ContainSingle()
            .Which;
        left.Args[0].Should().Be(GAME_ID);
        Convert.ToInt32(left.Args[1]!.ToString()).Should().Be(3);
        left.Args[2].Should().Be(true);

        var statusesBefore = Sent<Game2GameStatusMessageComposer>(3).Count();
        _clock.Advance(TimeSpan.FromSeconds(0.3));
        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(3).Should().HaveCount(statusesBefore);
        var statuses = Sent<Game2GameStatusMessageComposer>(1).ToList();
        statuses.Should().HaveCount(3);
        statuses
            .SelectMany(s => s.Subturns.SelectMany(events => events))
            .Should()
            .ContainSingle(e => e.Type == SnowWarGameEventType.HumanLeftGame)
            .Which.Fields.Should()
            .Equal(leaver.Id);
        Sent<Game2GameEndingMessageComposer>(1).Should().BeEmpty();
    }

    [Fact]
    public async Task one_player_left_alone_ends_the_game_and_the_last_one_out_closes_it()
    {
        var game = await RunningGameAsync(2);

        await game.ExitAsync(1, Ct);

        Sent<Game2GameEndingMessageComposer>(2)
            .Should()
            .ContainSingle()
            .Which.Teams.SelectMany(t => t.Players)
            .Select(p => p.UserId)
            .Should()
            .Equal(2);
        Sent<Game2GameEndingMessageComposer>(1).Should().BeEmpty();
        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnGameFinishedAsync)).Should().ContainSingle();
        DeactivationRequests().Should().BeEmpty();

        await game.ExitAsync(2, Ct);

        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnPlayerLeftGameAsync))
            .Select(call => call.Args[2])
            .Should()
            .Equal(true, false);
        DeactivationRequests().Should().ContainSingle();

        var sentBefore = SentTo(2).Count();
        _clock.Advance(TimeSpan.FromSeconds(60));
        await TickAsync(game);

        SentTo(2).Should().HaveCount(sentBefore);
    }

    [Fact]
    public async Task everyone_leaving_while_loading_closes_the_game_silently()
    {
        var game = NewGame();
        await game.StartAsync(Lobby(2), Ct);

        await game.ExitAsync(1, Ct);
        await game.ExitAsync(2, Ct);

        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnGameFinishedAsync)).Should().ContainSingle();
        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnPlayerLeftGameAsync))
            .Select(call => call.Args[2])
            .Should()
            .Equal(false, false);
        DeactivationRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task after_the_results_everyone_goes_back_to_the_room_they_came_from()
    {
        PlayersComeFromRooms();
        var game = await RunningGameAsync(2);
        await PlayToTheEndAsync(game);

        Sent<Game2GameRejoinMessageComposer>(1).Should().BeEmpty();

        _clock.Advance(TimeSpan.FromSeconds(29.9));
        await TickAsync(game);

        Sent<Game2GameRejoinMessageComposer>(1).Should().BeEmpty();

        _clock.Advance(TimeSpan.FromSeconds(0.1));
        await TickAsync(game);

        Sent<Game2GameRejoinMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.RoomId.Should()
            .Be(101);
        Sent<Game2GameRejoinMessageComposer>(2)
            .Should()
            .ContainSingle()
            .Which.RoomId.Should()
            .Be(102);
        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnPlayerLeftGameAsync))
            .Select(call => (Convert.ToInt32(call.Args[1]!.ToString()), call.Args[2]))
            .Should()
            .Equal((1, (object)false), (2, (object)false));
        DeactivationRequests().Should().ContainSingle();
    }

    [Fact]
    public async Task a_player_without_a_room_is_sent_back_nowhere()
    {
        var game = await RunningGameAsync(3);

        await game.ExitAsync(1, Ct);

        Sent<Game2GameRejoinMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.RoomId.Should()
            .Be(-1);
    }

    [Fact]
    public async Task rematch_requests_are_announced_once_and_handed_to_the_directory()
    {
        var game = await RunningGameAsync(3);
        await game.PlayAgainAsync(1, Ct);

        Sent<Game2PlayerRematchesMessageComposer>(2).Should().BeEmpty();

        await PlayToTheEndAsync(game);
        await game.PlayAgainAsync(1, Ct);
        await game.PlayAgainAsync(1, Ct);
        await game.PlayAgainAsync(3, Ct);

        Sent<Game2PlayerRematchesMessageComposer>(2).Select(m => m.UserId).Should().Equal(1, 3);
        Sent<Game2PlayerRematchesMessageComposer>(1).Should().HaveCount(2);

        _clock.Advance(TimeSpan.FromSeconds(30));
        await TickAsync(game);

        var rematch = DirectoryCalls(nameof(ISnowWarDirectoryGrain.RematchAsync))
            .Should()
            .ContainSingle()
            .Which;
        rematch.Args[0].Should().Be(GAME_ID);
        ((ImmutableArray<SnowWarPlayerSnapshot>)rematch.Args[1]!)
            .Select(p => (p.UserId, p.TeamId))
            .Should()
            .Equal((1, 1), (3, 1));
        DirectoryCalls(nameof(ISnowWarDirectoryGrain.OnPlayerLeftGameAsync))
            .Select(call => Convert.ToInt32(call.Args[1]!.ToString()))
            .Should()
            .Equal(2);
        Sent<Game2GameRejoinMessageComposer>(1).Should().ContainSingle();
    }

    [Fact]
    public async Task chat_is_trimmed_cut_to_a_hundred_characters_and_sent_to_every_participant()
    {
        var game = await RunningGameAsync(2);

        await game.ChatAsync(1, "  " + new string('a', 150) + "  ", Ct);

        foreach (var player in new[] { 1, 2 })
        {
            var chat = Sent<Game2GameChatFromPlayerMessageComposer>(player)
                .Should()
                .ContainSingle()
                .Which;
            chat.UserId.Should().Be(1);
            chat.Message.Should().Be(new string('a', 100));
        }
    }

    [Fact]
    public async Task chat_ignores_blank_messages_floods_and_strangers()
    {
        var game = await RunningGameAsync(2);

        await game.ChatAsync(1, "   ", Ct);
        await game.ChatAsync(99, "hello", Ct);
        await game.ChatAsync(1, "first", Ct);
        _clock.Advance(TimeSpan.FromMilliseconds(700));
        await game.ChatAsync(1, "too soon", Ct);
        _clock.Advance(TimeSpan.FromMilliseconds(50));
        await game.ChatAsync(1, "second", Ct);

        Sent<Game2GameChatFromPlayerMessageComposer>(2)
            .Select(m => m.Message)
            .Should()
            .Equal("first", "second");
    }

    [Fact]
    public async Task inputs_of_participants_become_events_and_those_of_others_are_ignored()
    {
        var game = await RunningGameAsync(2);
        var human = HumanOf(1);

        await game.QueueInputAsync(
            99,
            SnowWarInput.MoveTarget,
            [human.Variables[X_VARIABLE], human.Variables[Y_VARIABLE]],
            0,
            0,
            Ct
        );
        _clock.Advance(TimeSpan.FromSeconds(0.15));
        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(2)
            .SelectMany(s => s.Subturns.SelectMany(events => events))
            .Where(e => e.Type == SnowWarGameEventType.NewMoveTarget)
            .Should()
            .BeEmpty();

        await game.QueueInputAsync(
            1,
            SnowWarInput.MoveTarget,
            [human.Variables[X_VARIABLE], human.Variables[Y_VARIABLE]],
            1,
            0,
            Ct
        );
        _clock.Advance(TimeSpan.FromSeconds(0.15));
        await TickAsync(game);

        var last = Sent<Game2GameStatusMessageComposer>(2).Last();
        last.Turn.Should().Be(2);
        last.Subturns[0]
            .Should()
            .ContainSingle(e => e.Type == SnowWarGameEventType.NewMoveTarget)
            .Which.Fields[0]
            .Should()
            .Be(human.Id);
    }

    [Fact]
    public async Task inputs_before_the_stage_runs_are_ignored()
    {
        var game = NewGame();
        await game.StartAsync(Lobby(2), Ct);
        await ReadyAllAsync(game, 2);
        var human = HumanOf(1);

        await game.QueueInputAsync(
            1,
            SnowWarInput.MoveTarget,
            [human.Variables[X_VARIABLE], human.Variables[Y_VARIABLE]],
            0,
            0,
            Ct
        );
        _clock.Advance(TimeSpan.FromSeconds(5.15));
        await TickAsync(game);

        Sent<Game2GameStatusMessageComposer>(1)
            .SelectMany(s => s.Subturns.SelectMany(events => events))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task a_full_status_replaces_the_next_turn_status_and_carries_the_arena_checksum()
    {
        var game = await RunningGameAsync(2);

        await game.RequestFullStatusAsync(1, Ct);
        _clock.Advance(TimeSpan.FromSeconds(0.15));
        await TickAsync(game);

        var full = Sent<Game2FullGameStatusMessageComposer>(1).Should().ContainSingle().Which;
        full.Turn.Should().Be(1);
        full.DurationSeconds.Should().Be(180);
        full.RemainingSeconds.Should().Be(180);
        full.NumberOfTeams.Should().Be(2);
        full.Subturns.Should().HaveCount(3);
        full.Checksum.Should().Be(SnowWarChecksum.Compute(full.Turn, full.Objects));
        Sent<Game2GameStatusMessageComposer>(1).Select(m => m.Turn).Should().Equal(0);
        Sent<Game2GameStatusMessageComposer>(2)
            .Single(m => m.Turn == 1)
            .Checksum.Should()
            .Be(full.Checksum);
        Sent<Game2FullGameStatusMessageComposer>(2).Should().BeEmpty();

        await game.RequestFullStatusAsync(1, Ct);
        _clock.Advance(TimeSpan.FromSeconds(0.15));
        await TickAsync(game);

        Sent<Game2FullGameStatusMessageComposer>(1).Should().ContainSingle();

        _clock.Advance(TimeSpan.FromSeconds(0.7));
        await game.RequestFullStatusAsync(1, Ct);
        _clock.Advance(TimeSpan.FromSeconds(0.15));
        await TickAsync(game);

        Sent<Game2FullGameStatusMessageComposer>(1).Should().HaveCount(2);
    }
}
