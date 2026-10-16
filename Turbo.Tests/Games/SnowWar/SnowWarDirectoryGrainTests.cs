using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Turbo.Games;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarDirectoryGrainTests
{
    private const string GRAIN_TYPE = "Turbo.Games.Grains.SnowWarDirectoryGrain";
    private const string TICK_METHOD = "OnLobbyTimerAsync";

    private readonly Fakes _fakes = new();
    private readonly ManualTimeProvider _clock = new(
        new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)
    );

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ISnowWarDirectoryGrain NewDirectory(SnowWarConfig? config = null)
    {
        var settings = config ?? new SnowWarConfig();
        var grain = GrainHarness.Create(typeof(GamesModule).Assembly, GRAIN_TYPE, _fakes);

        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", settings);
        RoomHarness.SetField(grain, "_arenas", new SnowWarArenaCatalog(Options.Create(settings)));

        return (ISnowWarDirectoryGrain)grain;
    }

    private static Task TickAsync(ISnowWarDirectoryGrain directory) =>
        (Task)
            directory
                .GetType()
                .GetMethod(TICK_METHOD, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(directory, [Ct])!;

    private static SnowWarPlayerSnapshot Player(int id) =>
        new()
        {
            UserId = id,
            Name = $"player{id}",
            Figure = "hd-180-1",
            Gender = "M",
            TeamId = 0,
            SkillLevel = 1,
            TotalScore = 0,
            ScoreToNextLevel = 50,
        };

    private static Task JoinAsync(ISnowWarDirectoryGrain directory, int id) =>
        directory.QuickJoinAsync(id, Player(id), Ct);

    private static async Task JoinAllAsync(ISnowWarDirectoryGrain directory, int from, int to)
    {
        for (var id = from; id <= to; id++)
            await JoinAsync(directory, id);
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

    private IReadOnlyList<SnowWarGameLobbySnapshot> StartedGames() =>
        [
            .. _fakes
                .Log.Of(nameof(ISnowWarGameGrain.StartAsync))
                .Where(call => call.Interface == typeof(ISnowWarGameGrain))
                .Select(call => (SnowWarGameLobbySnapshot)call.Args[0]!),
        ];

    private async Task<int> StartGameAsync(ISnowWarDirectoryGrain directory, int first, int last)
    {
        await JoinAllAsync(directory, first, last);
        _clock.Advance(TimeSpan.FromSeconds(new SnowWarConfig().LobbyCountdownSeconds));
        await TickAsync(directory);

        return (await directory.GetGameIdOfPlayerAsync(first, Ct))!.Value;
    }

    [Fact]
    public async Task a_lone_player_gets_a_new_lobby_and_no_countdown()
    {
        var directory = NewDirectory();

        await JoinAsync(directory, 1);
        await TickAsync(directory);

        var lobby = Sent<Game2GameCreatedMessageComposer>(1).Should().ContainSingle().Which.Lobby;
        lobby.Players.Should().ContainSingle().Which.UserId.Should().Be(1);
        lobby.Players[0].TeamId.Should().Be(1);
        lobby.OwningPlayerName.Should().Be("player1");
        lobby.NumberOfTeams.Should().Be(2);
        lobby.MaximumPlayers.Should().Be(8);
        lobby.LevelEntryId.Should().Be(lobby.FieldType);
        new[] { 8, 9, 11 }.Should().Contain(lobby.FieldType);
        Sent<Game2StartCounterMessageMessageComposer>(1).Should().BeEmpty();
    }

    [Fact]
    public async Task a_second_player_joins_the_other_team_and_starts_the_countdown_for_both()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 2);

        Sent<Game2UserJoinedGameMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.Should()
            .Match<Game2UserJoinedGameMessageComposer>(m =>
                m.Player.UserId == 2 && m.Player.TeamId == 2 && !m.WasTeamSwitched
            );
        Sent<Game2GameLongDataMessageComposer>(2)
            .Should()
            .ContainSingle()
            .Which.Lobby.Players.Select(p => p.UserId)
            .Should()
            .Equal(1, 2);
        Sent<Game2StartCounterMessageMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.CountDownSeconds.Should()
            .Be(15);
        Sent<Game2StartCounterMessageMessageComposer>(2)
            .Should()
            .ContainSingle()
            .Which.CountDownSeconds.Should()
            .Be(15);
    }

    [Fact]
    public async Task more_arrivals_do_not_restart_the_countdown()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 2);
        _clock.Advance(TimeSpan.FromSeconds(4.5));
        await JoinAsync(directory, 3);
        await TickAsync(directory);

        Sent<Game2StartCounterMessageMessageComposer>(1).Should().ContainSingle();
        Sent<Game2StartCounterMessageMessageComposer>(3)
            .Should()
            .ContainSingle()
            .Which.CountDownSeconds.Should()
            .Be(11);
        Sent<Game2UserJoinedGameMessageComposer>(3).Should().BeEmpty();
        Sent<Game2UserJoinedGameMessageComposer>(1).Should().HaveCount(2);
    }

    [Fact]
    public async Task asking_again_from_the_lobby_resends_its_data_and_the_countdown()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 2);
        _clock.Advance(TimeSpan.FromSeconds(4.5));
        await JoinAsync(directory, 1);

        Sent<Game2GameLongDataMessageComposer>(1).Should().ContainSingle();
        Sent<Game2StartCounterMessageMessageComposer>(1)
            .Select(m => m.CountDownSeconds)
            .Should()
            .Equal(15, 11);
        Sent<Game2UserJoinedGameMessageComposer>(2).Should().BeEmpty();
    }

    [Fact]
    public async Task leaving_during_the_countdown_stops_it_and_the_other_keeps_waiting()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 2);
        var sentToLeaverBefore = SentTo(2).Count();
        await directory.LeaveLobbyAsync(2, Ct);

        Sent<Game2UserLeftGameMessageMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.UserId.Should()
            .Be(2);
        Sent<Game2StopCounterMessageMessageComposer>(1).Should().ContainSingle();
        SentTo(2).Should().HaveCount(sentToLeaverBefore);

        _clock.Advance(TimeSpan.FromSeconds(20));
        await TickAsync(directory);

        StartedGames().Should().BeEmpty();
        Sent<Game2GameStartedMessageComposer>(1).Should().BeEmpty();
        (await directory.GetGameIdOfPlayerAsync(1, Ct)).Should().BeNull();

        await JoinAsync(directory, 3);

        Sent<Game2GameLongDataMessageComposer>(3)
            .Should()
            .ContainSingle()
            .Which.Lobby.Players.Select(p => p.UserId)
            .Should()
            .Equal(1, 3);
        Sent<Game2StartCounterMessageMessageComposer>(1).Should().HaveCount(2);
    }

    [Fact]
    public async Task the_last_player_leaving_removes_the_lobby()
    {
        var directory = NewDirectory();

        await JoinAsync(directory, 1);
        var firstLobby = Sent<Game2GameCreatedMessageComposer>(1).Single().Lobby.GameId;
        await directory.LeaveLobbyAsync(1, Ct);
        await JoinAsync(directory, 2);

        Sent<Game2GameCreatedMessageComposer>(2)
            .Should()
            .ContainSingle()
            .Which.Lobby.GameId.Should()
            .NotBe(firstLobby);
    }

    [Fact]
    public async Task a_ninth_player_opens_a_new_lobby()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 9);

        var first = Sent<Game2GameCreatedMessageComposer>(1).Single().Lobby;
        var ninth = Sent<Game2GameCreatedMessageComposer>(9).Should().ContainSingle().Which.Lobby;
        ninth.GameId.Should().NotBe(first.GameId);
        ninth.Players.Select(p => p.UserId).Should().Equal(9);
        Sent<Game2UserJoinedGameMessageComposer>(1).Should().HaveCount(7);
    }

    [Fact]
    public async Task teams_stay_balanced_by_arrival()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 8);

        var lobby = Sent<Game2GameLongDataMessageComposer>(8).Single().Lobby;
        lobby.Players.Select(p => p.TeamId).Should().Equal(1, 2, 1, 2, 1, 2, 1, 2);
    }

    [Fact]
    public async Task the_end_of_the_countdown_starts_the_game_grain_and_announces_it()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 2);
        _clock.Advance(TimeSpan.FromSeconds(14));
        await TickAsync(directory);

        StartedGames().Should().BeEmpty();

        _clock.Advance(TimeSpan.FromSeconds(1));
        await TickAsync(directory);

        var started = StartedGames().Should().ContainSingle().Which;
        started.Players.Select(p => p.UserId).Should().Equal(1, 2);
        Sent<Game2GameStartedMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.Lobby.Should()
            .Be(started);
        Sent<Game2GameStartedMessageComposer>(2).Should().ContainSingle();

        var gameGrainKey = _fakes
            .Log.Of(nameof(ISnowWarGameGrain.StartAsync))
            .Single(call => call.Interface == typeof(ISnowWarGameGrain))
            .Key;
        Convert.ToInt64(gameGrainKey).Should().Be(started.GameId);
        (await directory.GetGameIdOfPlayerAsync(1, Ct)).Should().Be(started.GameId);
        (await directory.GetGameIdOfPlayerAsync(2, Ct)).Should().Be(started.GameId);
        (await directory.GetGameIdOfPlayerAsync(3, Ct)).Should().BeNull();
    }

    [Fact]
    public async Task teams_are_rebalanced_when_the_game_starts()
    {
        var directory = NewDirectory();

        await JoinAllAsync(directory, 1, 3);
        await directory.LeaveLobbyAsync(2, Ct);
        _clock.Advance(TimeSpan.FromSeconds(15));
        await TickAsync(directory);

        StartedGames()
            .Should()
            .ContainSingle()
            .Which.Players.Select(p => (p.UserId, p.TeamId))
            .Should()
            .Equal((1, 1), (3, 2));
    }

    [Fact]
    public async Task past_the_concurrent_game_limit_lobbies_wait_in_the_arena_queue()
    {
        var directory = NewDirectory(new SnowWarConfig { MaxConcurrentGames = 1 });

        var running = await StartGameAsync(directory, 1, 2);
        await JoinAllAsync(directory, 3, 12);
        _clock.Advance(TimeSpan.FromSeconds(15));
        await TickAsync(directory);
        await TickAsync(directory);

        StartedGames().Should().ContainSingle();
        Sent<Game2InArenaQueueMessageMessageComposer>(3)
            .Should()
            .ContainSingle()
            .Which.Position.Should()
            .Be(1);
        Sent<Game2InArenaQueueMessageMessageComposer>(11)
            .Should()
            .ContainSingle()
            .Which.Position.Should()
            .Be(2);
        Sent<Game2GameStartedMessageComposer>(3).Should().BeEmpty();

        await directory.OnGameFinishedAsync(running, Ct);

        StartedGames().Should().HaveCount(2);
        StartedGames()[1].Players.Select(p => p.UserId).Should().Equal(3, 4, 5, 6, 7, 8, 9, 10);
        Sent<Game2GameStartedMessageComposer>(3).Should().ContainSingle();
        Sent<Game2InArenaQueueMessageMessageComposer>(11)
            .Select(m => m.Position)
            .Should()
            .Equal(2, 1);
    }

    [Fact]
    public async Task a_player_joining_a_queued_lobby_is_told_its_position()
    {
        var directory = NewDirectory(new SnowWarConfig { MaxConcurrentGames = 1 });

        await StartGameAsync(directory, 1, 2);
        await JoinAllAsync(directory, 3, 4);
        _clock.Advance(TimeSpan.FromSeconds(15));
        await TickAsync(directory);
        await JoinAsync(directory, 5);

        Sent<Game2GameLongDataMessageComposer>(5).Should().ContainSingle();
        Sent<Game2InArenaQueueMessageMessageComposer>(5)
            .Should()
            .ContainSingle()
            .Which.Position.Should()
            .Be(1);
        Sent<Game2StartCounterMessageMessageComposer>(5).Should().BeEmpty();
    }

    [Fact]
    public async Task a_queued_lobby_falling_under_the_minimum_leaves_the_queue()
    {
        var directory = NewDirectory(new SnowWarConfig { MaxConcurrentGames = 1 });

        var running = await StartGameAsync(directory, 1, 2);
        await JoinAllAsync(directory, 3, 4);
        _clock.Advance(TimeSpan.FromSeconds(15));
        await TickAsync(directory);
        await directory.LeaveLobbyAsync(4, Ct);

        Sent<Game2StopCounterMessageMessageComposer>(3).Should().ContainSingle();

        await directory.OnGameFinishedAsync(running, Ct);

        StartedGames().Should().ContainSingle();
    }

    [Fact]
    public async Task a_player_in_a_running_game_cannot_join_another()
    {
        var directory = NewDirectory();

        await StartGameAsync(directory, 1, 2);
        await JoinAsync(directory, 1);

        Sent<Game2JoiningGameFailedMessageMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(SnowWarJoinFailureReason.UserHasActiveInstance);
        Sent<Game2GameCreatedMessageComposer>(1).Should().ContainSingle();
    }

    [Fact]
    public async Task a_player_on_the_results_screen_leaves_it_to_join_a_new_lobby()
    {
        var directory = NewDirectory();

        var game = await StartGameAsync(directory, 1, 2);
        await directory.OnGameFinishedAsync(game, Ct);
        await JoinAsync(directory, 1);

        Sent<Game2GameCreatedMessageComposer>(1).Should().HaveCount(2);
        (await directory.GetGameIdOfPlayerAsync(1, Ct)).Should().BeNull();
        (await directory.GetGameIdOfPlayerAsync(2, Ct)).Should().Be(game);
    }

    [Fact]
    public async Task leaving_a_game_in_play_blocks_the_player_for_the_configured_time()
    {
        var directory = NewDirectory();

        var game = await StartGameAsync(directory, 1, 2);
        await directory.OnPlayerLeftGameAsync(game, 1, true, Ct);

        (await directory.GetGameIdOfPlayerAsync(1, Ct)).Should().BeNull();
        Sent<Game2UserBlockedMessageMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.BlockSeconds.Should()
            .Be(180);

        _clock.Advance(TimeSpan.FromSeconds(99.5));
        await JoinAsync(directory, 1);

        Sent<Game2UserBlockedMessageMessageComposer>(1)
            .Select(m => m.BlockSeconds)
            .Should()
            .Equal(180, 81);
        Sent<Game2GameCreatedMessageComposer>(1).Should().ContainSingle();
        (await directory.GetAccountStatusAsync(1, Ct)).BlockSeconds.Should().Be(81);

        _clock.Advance(TimeSpan.FromSeconds(81));
        await JoinAsync(directory, 1);

        Sent<Game2GameCreatedMessageComposer>(1).Should().HaveCount(2);
        (await directory.GetAccountStatusAsync(1, Ct)).BlockSeconds.Should().Be(0);
    }

    [Fact]
    public async Task leaving_a_game_outside_play_does_not_block()
    {
        var directory = NewDirectory();

        var game = await StartGameAsync(directory, 1, 2);
        await directory.OnPlayerLeftGameAsync(game, 1, false, Ct);
        await JoinAsync(directory, 1);

        Sent<Game2UserBlockedMessageMessageComposer>(1).Should().BeEmpty();
        Sent<Game2GameCreatedMessageComposer>(1).Should().HaveCount(2);
    }

    [Fact]
    public async Task the_last_player_leaving_an_unfinished_game_frees_its_place()
    {
        var directory = NewDirectory(new SnowWarConfig { MaxConcurrentGames = 1 });

        var game = await StartGameAsync(directory, 1, 2);
        await directory.OnPlayerLeftGameAsync(game, 1, true, Ct);
        await directory.OnPlayerLeftGameAsync(game, 2, true, Ct);
        await StartGameAsync(directory, 3, 4);

        StartedGames().Should().HaveCount(2);
    }

    [Fact]
    public async Task the_account_status_announces_unlimited_free_games()
    {
        var directory = NewDirectory();

        var status = await directory.GetAccountStatusAsync(1, Ct);

        status.Status.Should().Be(SnowWarDirectoryStatus.Available);
        status.FreeGamesLeft.Should().Be(-1);
        status.BlockSeconds.Should().Be(0);
    }

    [Fact]
    public async Task without_a_playable_arena_the_game_is_unavailable_and_joining_fails()
    {
        var directory = NewDirectory(new SnowWarConfig { Arenas = [] });

        (await directory.GetAccountStatusAsync(1, Ct))
            .Status.Should()
            .Be(SnowWarDirectoryStatus.Unavailable);

        await JoinAsync(directory, 1);

        Sent<Game2JoiningGameFailedMessageMessageComposer>(1)
            .Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(SnowWarJoinFailureReason.Kicked);
    }

    [Fact]
    public async Task a_game_grain_that_fails_to_start_reports_the_failure_and_frees_the_players()
    {
        var directory = NewDirectory();
        _fakes.Handlers[nameof(ISnowWarGameGrain.StartAsync)] = call =>
            call.Interface == typeof(ISnowWarGameGrain)
                ? Task.FromException(new InvalidOperationException("no spawn"))
                : Fakes.NotHandled;

        await JoinAllAsync(directory, 1, 2);
        _clock.Advance(TimeSpan.FromSeconds(15));
        await TickAsync(directory);

        Sent<Game2StartingGameFailedMessageMessageComposer>(1).Should().ContainSingle();
        Sent<Game2StartingGameFailedMessageMessageComposer>(2).Should().ContainSingle();
        (await directory.GetGameIdOfPlayerAsync(1, Ct)).Should().BeNull();

        await JoinAsync(directory, 1);

        Sent<Game2GameCreatedMessageComposer>(1).Should().HaveCount(2);
    }
}
