using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Entities.Players;
using Turbo.Events.Extensions;
using Turbo.Games;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;
using Turbo.Games.SnowWar.Events;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Players.Events;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Snapshots;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Runtime.AssemblyProcessing;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarHandlerTests
{
    private const string DIRECTORY_TYPE = "Turbo.Games.Grains.SnowWarDirectoryGrain";
    private const string GAME_TYPE = "Turbo.Games.Grains.SnowWarGameGrain";
    private const string SCORE_TYPE = "Turbo.Games.Grains.SnowWarScoreGrain";
    private const long SINGLETON_KEY = 0;
    private const int UNLIMITED = -1;

    private readonly PacketHarness _packets = new();
    private readonly InMemoryDb _db = new();
    private readonly ManualTimeProvider _clock = new(
        new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero)
    );

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static int In(string name) => PacketHarness.Incoming(name);

    private static int Out(string name) => PacketHarness.Outgoing(name);

    private object Grain(Fakes fakes, string type)
    {
        var grain = GrainHarness.Create(typeof(GamesModule).Assembly, type, fakes, _db);
        var config = new SnowWarConfig();

        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", config);

        if (type != SCORE_TYPE)
            RoomHarness.SetField(grain, "_arenas", new SnowWarArenaCatalog(Options.Create(config)));

        return grain;
    }

    private ISnowWarDirectoryGrain RealDirectory(Fakes fakes)
    {
        var directory = (ISnowWarDirectoryGrain)Grain(fakes, DIRECTORY_TYPE);

        fakes.Instances[(typeof(ISnowWarDirectoryGrain), (object)SINGLETON_KEY)] = directory;

        return directory;
    }

    private ISnowWarScoreGrain RealScores(Fakes fakes)
    {
        var scores = (ISnowWarScoreGrain)Grain(fakes, SCORE_TYPE);

        fakes.Instances[(typeof(ISnowWarScoreGrain), (object)SINGLETON_KEY)] = scores;

        return scores;
    }

    private ISnowWarGameGrain RealGame(Fakes fakes, long gameId)
    {
        var game = (ISnowWarGameGrain)Grain(fakes, GAME_TYPE);

        fakes.Instances[(typeof(ISnowWarGameGrain), (object)gameId)] = game;

        return game;
    }

    private static void KnownPlayers(Fakes fakes) =>
        fakes.Handlers[nameof(IPlayerGrain.GetSummaryAsync)] = call =>
        {
            var id = (int)Convert.ToInt64(call.Key);

            return Task.FromResult(
                new PlayerSummarySnapshot
                {
                    PlayerId = id,
                    Name = $"player{id}",
                    Motto = string.Empty,
                    Figure = $"hd-{id}",
                    Gender = AvatarGenderType.Female,
                    AchievementScore = 0,
                    BadgesRank = 0,
                    IsOnline = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    RespectPoints = 0,
                    RespectsLeft = 0,
                    PetRespectsLeft = 0,
                    RespectReplenishesLeft = 0,
                }
            );
        };

    private void AddPlayers(params int[] ids)
    {
        using var ctx = _db.CreateDbContext();

        foreach (var id in ids)
            ctx.Players.Add(
                new PlayerEntity
                {
                    Id = id,
                    Name = $"player{id}",
                    Figure = $"hd-{id}",
                    Gender = AvatarGenderType.Male,
                    PlayerStatus = PlayerStatusType.Offline,
                }
            );

        ctx.SaveChanges();
    }

    private static IEnumerable<ClientPacket> WithHeader(
        IEnumerable<ClientPacket> packets,
        string name
    ) => packets.Where(p => p.Header == Out(name));

    private static IEnumerable<object> SentTo(Fakes fakes, int playerId) =>
        fakes
            .Log.Of(nameof(IPlayerPresenceGrain.SendComposerAsync))
            .Where(call =>
                call.Interface == typeof(IPlayerPresenceGrain)
                && Convert.ToInt64(call.Key) == playerId
            )
            .Select(call => call.Args[0]!);

    [Fact]
    public async Task quick_join_answers_with_a_new_lobby_holding_the_player()
    {
        KnownPlayers(_packets.Fakes);
        RealDirectory(_packets.Fakes);
        RealScores(_packets.Fakes);

        var replies = await _packets.SendAsync(
            In("Game2QuickJoinGameMessageEvent"),
            PacketHarness.Payload(_ => { }),
            playerId: 5
        );

        var created = WithHeader(replies, "Game2GameCreatedMessageComposer")
            .Should()
            .ContainSingle()
            .Which;

        created.PopInt().Should().BeGreaterThan(0);
        created.PopString();
        created.PopInt().Should().Be(0);
        created.PopInt();
        created.PopInt().Should().Be(2);
        created.PopInt().Should().Be(8);
        created.PopString().Should().Be("player5");
        created.PopInt();
        created.PopInt().Should().Be(1);
        created.PopInt().Should().Be(5);
        created.PopString().Should().Be("player5");
        created.PopString().Should().Be("hd-5");
        created.PopString().Should().Be("F");
        created.PopInt().Should().Be(1);
        created.PopInt().Should().Be(1);
        created.PopInt().Should().Be(0);
        created.PopInt().Should().Be(50);
    }

    [Fact]
    public async Task account_game_status_announces_unlimited_games()
    {
        RealDirectory(_packets.Fakes);
        RealScores(_packets.Fakes);

        var replies = await _packets.SendAsync(
            In("Game2GetAccountGameStatusMessageEvent"),
            PacketHarness.Payload(w => w.Int(0))
        );

        var status = WithHeader(replies, "Game2AccountGameStatusMessageComposer")
            .Should()
            .ContainSingle()
            .Which;

        status.PopInt().Should().Be(0);
        status.PopInt().Should().Be(UNLIMITED);
        status.PopInt().Should().Be(0);
    }

    [Fact]
    public async Task directory_status_announces_unlimited_games_and_the_games_played()
    {
        RealDirectory(_packets.Fakes);
        var scores = RealScores(_packets.Fakes);

        await scores.AddAsync([new SnowWarScoreEntrySnapshot { PlayerId = 1, Score = 4 }], Ct);

        var replies = await _packets.SendAsync(
            In("Game2CheckGameDirectoryStatusMessageEvent"),
            PacketHarness.Payload(_ => { })
        );

        var status = WithHeader(replies, "Game2GameDirectoryStatusMessageComposer")
            .Should()
            .ContainSingle()
            .Which;

        status.PopInt().Should().Be(0);
        status.PopInt().Should().Be(0);
        status.PopInt().Should().Be(1);
        status.PopInt().Should().Be(UNLIMITED);
    }

    [Fact]
    public async Task the_game_tokens_offer_is_an_empty_list()
    {
        var replies = await _packets.SendAsync(
            In("GetSnowWarGameTokensOfferMessageEvent"),
            PacketHarness.Payload(_ => { })
        );

        var offers = replies.Should().ContainSingle().Which;

        offers.Header.Should().Be(Out("SnowWarGameTokensMessageComposer"));
        offers.PopInt().Should().Be(0);
        offers.End.Should().BeTrue();
    }

    [Fact]
    public async Task buying_game_tokens_is_refused_without_touching_any_balance()
    {
        var replies = await _packets.SendAsync(
            In("PurchaseSnowWarGameTokensOfferMessageEvent"),
            PacketHarness.Payload(w => w.Int(1))
        );

        replies
            .Should()
            .ContainSingle()
            .Which.Header.Should()
            .Be(Out("PurchaseNotAllowedMessageComposer"));
        _packets
            .Fakes.Log.Calls.Should()
            .NotContain(call =>
                call.Interface.Name.Contains("Wallet", StringComparison.Ordinal)
                || call.Interface.Name.Contains("Currency", StringComparison.Ordinal)
                || call.Interface.Name.Contains("Purchase", StringComparison.Ordinal)
            );
    }

    [Fact]
    public async Task end_of_game_scores_appear_in_the_weekly_leaderboard_in_order()
    {
        AddPlayers(1, 2, 3);
        var scores = RealScores(_packets.Fakes);

        await scores.AddAsync(
            [
                new SnowWarScoreEntrySnapshot { PlayerId = 1, Score = 6 },
                new SnowWarScoreEntrySnapshot { PlayerId = 2, Score = 12 },
                new SnowWarScoreEntrySnapshot { PlayerId = 3, Score = 1 },
            ],
            Ct
        );

        var replies = await _packets.SendAsync(
            In("Game2GetWeeklyLeaderboardMessageEvent"),
            PacketHarness.Payload(w => w.Int(0).Int(0).Int(1).Int(0).Int(8).Int(50))
        );

        var weekly = WithHeader(replies, "Game2WeeklyLeaderboardMessageComposer")
            .Should()
            .ContainSingle()
            .Which;

        weekly.PopInt().Should().Be(2026);
        weekly.PopInt().Should().Be(42);
        weekly.PopInt().Should().Be(0);
        weekly.PopInt().Should().Be(0);
        weekly.PopInt();
        weekly.PopInt().Should().Be(3);

        var rows = Enumerable
            .Range(0, 3)
            .Select(_ =>
            {
                var id = weekly.PopInt();
                var score = weekly.PopInt();
                var rank = weekly.PopInt();
                var name = weekly.PopString();
                weekly.PopString();
                weekly.PopString();
                return (id, score, rank, name);
            })
            .ToList();

        rows.Should().Equal((2, 12, 1, "player2"), (1, 6, 2, "player1"), (3, 1, 3, "player3"));
        weekly.PopInt().Should().Be(3);
        weekly.PopInt().Should().Be(0);
    }

    [Fact]
    public async Task a_leaderboard_for_another_game_type_gets_no_answer()
    {
        RealScores(_packets.Fakes);

        var replies = await _packets.SendAsync(
            In("Game2GetTotalLeaderboardMessageEvent"),
            PacketHarness.Payload(w => w.Int(3).Int(1).Int(0).Int(8).Int(50))
        );

        replies.Should().BeEmpty();
    }

    [Fact]
    public async Task group_leaderboards_are_answered_empty()
    {
        RealScores(_packets.Fakes);

        var replies = await _packets.SendAsync(
            In("Game2GetTotalGroupLeaderboardMessageEvent"),
            PacketHarness.Payload(w => w.Int(0).Int(1).Int(0).Int(8).Int(50))
        );

        var groups = WithHeader(replies, "Game2TotalGroupLeaderboardMessageComposer")
            .Should()
            .ContainSingle()
            .Which;

        groups.PopInt().Should().Be(0);
        groups.PopInt().Should().Be(0);
        groups.PopInt().Should().Be(0);
        groups.PopInt().Should().Be(0);
    }

    [Fact]
    public async Task a_move_target_reaches_the_players_game_with_its_coordinates()
    {
        _packets.Fakes.Handlers[nameof(ISnowWarDirectoryGrain.GetGameIdOfPlayerAsync)] = _ =>
            Task.FromResult<int?>(7);

        await _packets.SendAsync(
            In("Game2SetUserMoveTargetMessageEvent"),
            PacketHarness.Payload(w => w.Int(6400).Int(9600).Int(12).Int(1))
        );

        var call = _packets
            .Fakes.Log.Of(nameof(ISnowWarGameGrain.QueueInputAsync))
            .Should()
            .ContainSingle()
            .Which;

        Convert.ToInt64(call.Key).Should().Be(7);
        call.Args[0].Should().Be((PlayerId)1);
        call.Args[1].Should().Be(SnowWarInput.MoveTarget);
        ((int[])call.Args[2]!).Should().Equal(6400, 9600);
        call.Args[3].Should().Be(12);
        call.Args[4].Should().Be(1);
    }

    [Fact]
    public async Task game_inputs_from_a_player_outside_any_game_go_nowhere()
    {
        await _packets.SendAsync(
            In("Game2MakeSnowballMessageEvent"),
            PacketHarness.Payload(w => w.Int(3).Int(0))
        );

        _packets.Fakes.Log.On<ISnowWarGameGrain>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task an_out_of_range_loading_percentage_is_ignored(int percentage)
    {
        _packets.Fakes.Handlers[nameof(ISnowWarDirectoryGrain.GetGameIdOfPlayerAsync)] = _ =>
            Task.FromResult<int?>(7);

        await _packets.SendAsync(
            In("Game2LoadStageReadyMessageEvent"),
            PacketHarness.Payload(w => w.Int(percentage))
        );

        _packets.Fakes.Log.Of(nameof(ISnowWarGameGrain.StageReadyAsync)).Should().BeEmpty();
    }

    [Fact]
    public async Task a_player_whose_connection_goes_mid_game_leaves_it_and_the_other_is_told()
    {
        var hotel = new SessionHarness();
        var directory = RealDirectory(hotel.Fakes);
        RealScores(hotel.Fakes);

        await directory.QuickJoinAsync(1, LobbyPlayer(1), Ct);
        await directory.QuickJoinAsync(2, LobbyPlayer(2), Ct);

        var lobbyId = (await directory.GetGameIdOfPlayerAsync(1, Ct)) ?? 1;
        var game = RealGame(hotel.Fakes, lobbyId);

        _clock.Advance(TimeSpan.FromSeconds(new SnowWarConfig().LobbyCountdownSeconds));
        await TickAsync(directory, "OnLobbyTimerAsync");
        await game.StageReadyAsync(1, Ct);
        await game.StageReadyAsync(2, Ct);
        _clock.Advance(TimeSpan.FromSeconds(new SnowWarConfig().PreparingSeconds));
        await TickAsync(game, "OnGameTimerAsync");

        var session = hotel.NewSession(1);
        await hotel.Gateway.AddSessionAsync(session.SessionKey, session);
        await hotel.Gateway.AddSessionToPlayerAsync(session.SessionKey, SessionHarness.PlayerId);

        var offline = new SnowWarPlayerOfflineHandler(
            hotel.Fakes.Create<IGrainFactory>(),
            NullLogger<SnowWarPlayerOfflineHandler>.Instance
        );
        hotel.Events.OnAsync<PlayerOnlineChangedEvent>(
            (env, ct) => offline.HandleAsync(env, null!, ct)
        );

        await hotel.Gateway.DisconnectPlayerAsync(SessionHarness.PlayerId, null, Ct);
        await hotel.Gateway.RemoveSessionAsync(session.SessionKey, Ct);
        await SessionHarness.Settle();

        hotel.WasClosed(session.SessionKey).Should().BeTrue();
        SentTo(hotel.Fakes, 2)
            .OfType<Game2PlayerExitedGameArenaMessageComposer>()
            .Should()
            .ContainSingle()
            .Which.UserId.Should()
            .Be(1);
        (await directory.GetGameIdOfPlayerAsync(1, Ct)).Should().BeNull();
        (await directory.GetGameIdOfPlayerAsync(2, Ct)).Should().Be(lobbyId);
    }

    [Fact]
    public async Task a_player_whose_connection_goes_in_a_lobby_leaves_it()
    {
        var fakes = new Fakes();
        var directory = RealDirectory(fakes);

        await directory.QuickJoinAsync(1, LobbyPlayer(1), Ct);
        await directory.QuickJoinAsync(2, LobbyPlayer(2), Ct);

        var offline = new SnowWarPlayerOfflineHandler(
            fakes.Create<IGrainFactory>(),
            NullLogger<SnowWarPlayerOfflineHandler>.Instance
        );

        await offline.HandleAsync(
            new PlayerOnlineChangedEvent { PlayerId = 1, Online = false },
            null!,
            Ct
        );

        SentTo(fakes, 2)
            .OfType<Turbo.Primitives.Messages.Outgoing.Game.Directory.Game2UserLeftGameMessageMessageComposer>()
            .Should()
            .ContainSingle()
            .Which.UserId.Should()
            .Be(1);
    }

    [Fact]
    public async Task a_failed_lobby_leave_still_takes_the_player_out_of_the_game()
    {
        var fakes = new Fakes();
        var logger = new CapturingLogger<SnowWarPlayerOfflineHandler>();
        fakes.Handlers[nameof(ISnowWarDirectoryGrain.LeaveLobbyAsync)] = _ =>
            Task.FromException(new InvalidOperationException("directory down"));
        fakes.Handlers[nameof(ISnowWarDirectoryGrain.GetGameIdOfPlayerAsync)] = _ =>
            Task.FromResult<int?>(7);

        var offline = new SnowWarPlayerOfflineHandler(fakes.Create<IGrainFactory>(), logger);

        await offline.HandleAsync(
            new PlayerOnlineChangedEvent { PlayerId = 1, Online = false },
            null!,
            Ct
        );

        fakes
            .Log.Of(nameof(ISnowWarGameGrain.ExitAsync))
            .Should()
            .ContainSingle()
            .Which.Key.Should()
            .Be(7L);
        logger.AtLeast(LogLevel.Error).Should().ContainSingle();
    }

    [Fact]
    public async Task a_failed_game_lookup_is_logged_after_the_lobby_leave()
    {
        var fakes = new Fakes();
        var logger = new CapturingLogger<SnowWarPlayerOfflineHandler>();
        fakes.Handlers[nameof(ISnowWarDirectoryGrain.GetGameIdOfPlayerAsync)] = _ =>
            Task.FromException<int?>(new InvalidOperationException("directory down"));

        var offline = new SnowWarPlayerOfflineHandler(fakes.Create<IGrainFactory>(), logger);

        await offline.HandleAsync(
            new PlayerOnlineChangedEvent { PlayerId = 1, Online = false },
            null!,
            Ct
        );

        fakes.Log.Of(nameof(ISnowWarDirectoryGrain.LeaveLobbyAsync)).Should().ContainSingle();
        logger.AtLeast(LogLevel.Error).Should().ContainSingle();
    }

    [Fact]
    public async Task the_hotel_event_pipeline_discovers_the_disconnect_handler()
    {
        var fakes = new Fakes();
        var services = new ServiceCollection()
            .AddLogging()
            .AddTurboEventSystem()
            .AddSingleton(fakes.Create<IGrainFactory>())
            .AddSingleton<AssemblyProcessor>()
            .BuildServiceProvider();
        var module = new GamesModule();

        using var registrations = await services
            .GetRequiredService<AssemblyProcessor>()
            .ProcessAsync(module.GetType().Assembly, services, Ct);

        await services
            .GetRequiredService<Turbo.Events.EventSystem>()
            .PublishAsync(new PlayerOnlineChangedEvent { PlayerId = 4, Online = false }, Ct);

        module.Should().BeAssignableTo<Turbo.Contracts.Plugins.IHostPluginModule>();
        fakes
            .Log.Of(nameof(ISnowWarDirectoryGrain.LeaveLobbyAsync))
            .Should()
            .ContainSingle()
            .Which.Args[0]
            .Should()
            .Be((PlayerId)4);
    }

    private static SnowWarPlayerSnapshot LobbyPlayer(int id) =>
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

    private static Task TickAsync(object grain, string method) =>
        (Task)
            grain
                .GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grain, [Ct])!;
}
