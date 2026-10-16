using System.Collections.Immutable;
using Turbo.Primitives.Furniture.Snapshots.StuffData;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Incoming.Game.Arena;
using Turbo.Primitives.Messages.Incoming.Game.Directory;
using Turbo.Primitives.Messages.Incoming.Game.Ingame;
using Turbo.Primitives.Messages.Incoming.Game.Score;
using Turbo.Primitives.Messages.Outgoing.Catalog;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Messages.Outgoing.Game.Score;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;
using Turbo.Primitives.Networking;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Protocol;

public sealed class SnowWarPacketTests
{
    private static readonly SnowWarPlayerSnapshot Ann = new()
    {
        UserId = 4,
        Name = "Ann",
        Figure = "hd-180-1.ch-210-66",
        Gender = "F",
        TeamId = 1,
        SkillLevel = 3,
        TotalScore = 120,
        ScoreToNextLevel = 80,
    };

    private static readonly SnowWarPlayerSnapshot Bob = new()
    {
        UserId = 9,
        Name = "Bob",
        Figure = "hd-190-2",
        Gender = "M",
        TeamId = 2,
        SkillLevel = 1,
        TotalScore = 0,
        ScoreToNextLevel = 50,
    };

    private static readonly SnowWarGameLobbySnapshot Lobby = new()
    {
        GameId = 7,
        LevelName = "Île arctique",
        GameType = 0,
        FieldType = 8,
        NumberOfTeams = 2,
        MaximumPlayers = 8,
        OwningPlayerName = "Ann",
        LevelEntryId = 8,
        Players = [Ann, Bob],
    };

    private static readonly SnowWarArenaPlayerSnapshot AnnInArena = new()
    {
        ReferenceId = 20,
        UserName = "Ann",
        Figure = "hd-180-1",
        Gender = "F",
        TeamId = 1,
    };

    private static readonly SnowWarArenaPlayerSnapshot BobInArena = new()
    {
        ReferenceId = 21,
        UserName = "Bob",
        Figure = "hd-190-2",
        Gender = "M",
        TeamId = 2,
    };

    private static readonly ImmutableArray<SnowWarGameObjectSnapshot> Objects =
    [
        new()
        {
            Type = SnowWarGameObjectType.Human,
            Id = 20,
            Variables = [9600, 6400, 0, 9600, 6400, 4, 2, 5, 0, 0, 0, 0, 0, 1, 0, 0, 0],
            Strings = ["Ann", "", "hd-180-1", "F"],
        },
        new()
        {
            Type = SnowWarGameObjectType.Tree,
            Id = 3,
            Variables = [1, 0, 3, 6, 0, 0, 0],
            Strings = [],
        },
        new()
        {
            Type = SnowWarGameObjectType.Snowball,
            Id = 30,
            Variables = [9600, 6400, 3200, 3, -2, 1, 20, 6, 10],
            Strings = [],
        },
    ];

    private static readonly ImmutableArray<ImmutableArray<SnowWarGameEventSnapshot>> Subturns =
    [
        [
            new() { Type = SnowWarGameEventType.NewMoveTarget, Fields = [20, 3200, 6400] },
            new()
            {
                Type = SnowWarGameEventType.HumanThrowsSnowballAtPosition,
                Fields = [21, 9600, 3200, 1],
            },
        ],
        [],
        [
            new() { Type = SnowWarGameEventType.MachineCreatesSnowball, Fields = [5] },
            new() { Type = SnowWarGameEventType.CreateSnowball, Fields = [30, 20, 9600, 6400, 1] },
        ],
    ];

    private static readonly ImmutableArray<SnowWarLeaderboardEntrySnapshot> Entries =
    [
        new()
        {
            UserId = 4,
            Score = 50,
            Rank = 1,
            Name = "Ann",
            Figure = "hd-180-1",
            Gender = "F",
        },
        new()
        {
            UserId = 9,
            Score = 20,
            Rank = 2,
            Name = "Bob",
            Figure = "hd-190-2",
            Gender = "M",
        },
    ];

    [Fact]
    public void account_game_status_announces_the_free_games_left_and_games_played() =>
        AssertWritesFixture(
            "Game2AccountGameStatusMessageComposer",
            new Game2AccountGameStatusMessageMessageComposer
            {
                GameTypeId = 0,
                FreeGamesLeft = -1,
                GamesPlayedTotal = 12,
            }
        );

    [Fact]
    public void game_cancelled_has_no_payload() =>
        AssertWritesFixture(
            "Game2GameCancelledMessageComposer",
            new Game2GameCancelledMessageMessageComposer()
        );

    [Fact]
    public void game_created_writes_the_lobby_and_its_players() =>
        AssertWritesFixture(
            "Game2GameCreatedMessageComposer",
            new Game2GameCreatedMessageComposer { Lobby = Lobby }
        );

    [Fact]
    public void directory_status_writes_availability_block_and_game_counts() =>
        AssertWritesFixture(
            "Game2GameDirectoryStatusMessageComposer",
            new Game2GameDirectoryStatusMessageMessageComposer
            {
                Status = SnowWarDirectoryStatus.Available,
                BlockSeconds = 180,
                GamesPlayed = 4,
                FreeGamesLeft = -1,
            }
        );

    [Fact]
    public void game_long_data_writes_the_lobby() =>
        AssertWritesFixture(
            "Game2GameLongDataMessageComposer",
            new Game2GameLongDataMessageComposer { Lobby = Lobby }
        );

    [Fact]
    public void game_not_found_has_no_payload() =>
        AssertWritesFixture(
            "Game2GameNotFoundMessageComposer",
            new Game2GameNotFoundMessageMessageComposer()
        );

    [Fact]
    public void game_started_writes_the_lobby() =>
        AssertWritesFixture(
            "Game2GameStartedMessageComposer",
            new Game2GameStartedMessageComposer { Lobby = Lobby }
        );

    [Fact]
    public void in_arena_queue_writes_the_queue_position() =>
        AssertWritesFixture(
            "Game2InArenaQueueMessageComposer",
            new Game2InArenaQueueMessageMessageComposer { Position = 3 }
        );

    [Fact]
    public void joining_game_failed_writes_the_reason() =>
        AssertWritesFixture(
            "Game2JoiningGameFailedMessageComposer",
            new Game2JoiningGameFailedMessageMessageComposer
            {
                Reason = SnowWarJoinFailureReason.UserHasActiveInstance,
            }
        );

    [Fact]
    public void start_counter_writes_the_countdown() =>
        AssertWritesFixture(
            "Game2StartCounterMessageComposer",
            new Game2StartCounterMessageMessageComposer { CountDownSeconds = 15 }
        );

    [Fact]
    public void starting_game_failed_writes_the_reason() =>
        AssertWritesFixture(
            "Game2StartingGameFailedMessageComposer",
            new Game2StartingGameFailedMessageMessageComposer
            {
                Reason = SnowWarJoinFailureReason.Kicked,
            }
        );

    [Fact]
    public void stop_counter_writes_the_queue_shape_the_client_ignores() =>
        AssertWritesFixture(
            "Game2StopCounterMessageComposer",
            new Game2StopCounterMessageMessageComposer()
        );

    [Fact]
    public void user_blocked_writes_the_block_seconds() =>
        AssertWritesFixture(
            "Game2UserBlockedMessageComposer",
            new Game2UserBlockedMessageMessageComposer { BlockSeconds = 180 }
        );

    [Fact]
    public void user_joined_writes_the_lobby_player_and_the_team_switch() =>
        AssertWritesFixture(
            "Game2UserJoinedGameMessageComposer",
            new Game2UserJoinedGameMessageComposer { Player = Bob, WasTeamSwitched = true }
        );

    [Fact]
    public void user_left_writes_the_user_id() =>
        AssertWritesFixture(
            "Game2UserLeftGameMessageComposer",
            new Game2UserLeftGameMessageMessageComposer { UserId = 9 }
        );

    [Fact]
    public void enter_arena_writes_players_and_the_level_with_legacy_and_map_stuff() =>
        AssertWritesFixture(
            "Game2EnterArenaMessageComposer",
            new Game2EnterArenaMessageComposer
            {
                GameType = 0,
                FieldType = 8,
                NumberOfTeams = 2,
                Players = [AnnInArena, BobInArena],
                Level = new SnowWarLevelSnapshot
                {
                    Width = 3,
                    Height = 2,
                    HeightMap = "000\r0x0",
                    FuseObjects =
                    [
                        new()
                        {
                            Name = "snst_tree1",
                            Id = 1,
                            X = 1,
                            Y = 0,
                            XDimension = 1,
                            YDimension = 1,
                            Height = 3200,
                            Direction = 2,
                            Altitude = 1440,
                            CanStandOn = false,
                            StuffData = new LegacyStuffSnapshot { StuffBitmask = 0, Data = "0" },
                        },
                        new()
                        {
                            Name = "ads_background",
                            Id = 2,
                            X = 0,
                            Y = 0,
                            XDimension = 1,
                            YDimension = 1,
                            Height = 0,
                            Direction = 1,
                            Altitude = 0,
                            CanStandOn = true,
                            StuffData = new MapStuffSnapshot
                            {
                                StuffBitmask = 1,
                                Data = ImmutableDictionary<string, string>.Empty.Add(
                                    "imageUrl",
                                    "/cms/flash-files/c_images/snowwar/bg.png"
                                ),
                            },
                        },
                    ],
                },
            }
        );

    [Fact]
    public void arena_entered_writes_the_arena_player() =>
        AssertWritesFixture(
            "Game2ArenaEnteredMessageComposer",
            new Game2ArenaEnteredMessageComposer { Player = AnnInArena }
        );

    [Fact]
    public void enter_arena_failed_writes_the_reason() =>
        AssertWritesFixture(
            "Game2EnterArenaFailedMessageComposer",
            new Game2EnterArenaFailedMessageComposer
            {
                Reason = SnowWarEnterArenaFailureReason.GameAlreadyStarted,
            }
        );

    [Fact]
    public void stage_load_writes_the_game_type() =>
        AssertWritesFixture(
            "Game2StageLoadMessageComposer",
            new Game2StageLoadMessageComposer { GameType = 0 }
        );

    [Fact]
    public void stage_still_loading_writes_the_percentage_and_the_finished_players() =>
        AssertWritesFixture(
            "Game2StageStillLoadingMessageComposer",
            new Game2StageStillLoadingMessageComposer { Percentage = 50, FinishedUserIds = [4, 9] }
        );

    [Fact]
    public void stage_starting_writes_the_room_type_countdown_and_game_objects() =>
        AssertWritesFixture(
            "Game2StageStartingMessageComposer",
            new Game2StageStartingMessageComposer
            {
                GameType = 0,
                RoomType = "snowwar",
                CountDown = 5,
                Objects = Objects,
            }
        );

    [Fact]
    public void stage_running_writes_the_time_to_stage_end() =>
        AssertWritesFixture(
            "Game2StageRunningMessageComposer",
            new Game2StageRunningMessageComposer { TimeToStageEnd = 180 }
        );

    [Fact]
    public void game_status_writes_the_turn_checksum_and_events_per_subturn() =>
        AssertWritesFixture(
            "Game2GameStatusMessageComposer",
            new Game2GameStatusMessageComposer
            {
                Turn = 9,
                Checksum = -77,
                Subturns = Subturns,
            }
        );

    [Fact]
    public void full_game_status_writes_the_times_objects_teams_and_status() =>
        AssertWritesFixture(
            "Game2FullGameStatusMessageComposer",
            new Game2FullGameStatusMessageComposer
            {
                RemainingSeconds = 170,
                DurationSeconds = 180,
                Objects = Objects,
                NumberOfTeams = 2,
                Turn = 9,
                Checksum = -77,
                Subturns = Subturns,
            }
        );

    [Fact]
    public void stage_ending_writes_the_time_to_next_state() =>
        AssertWritesFixture(
            "Game2StageEndingMessageComposer",
            new Game2StageEndingMessageComposer { TimeToNextState = 5 }
        );

    [Fact]
    public void game_ending_writes_the_result_teams_player_stats_and_best_players() =>
        AssertWritesFixture(
            "Game2GameEndingMessageComposer",
            new Game2GameEndingMessageComposer
            {
                TimeToNextState = 30,
                IsDeathMatch = false,
                ResultType = SnowWarGameResultType.Win,
                WinnerId = 1,
                Teams =
                [
                    new()
                    {
                        TeamId = 1,
                        Score = 6,
                        Players =
                        [
                            new()
                            {
                                UserName = "Ann",
                                UserId = 4,
                                Figure = "hd-180-1",
                                Gender = "F",
                                Score = 6,
                                Stats = Stats(6, 1, 0, 1, 0, 2, 1, 0, 0, 0),
                            },
                        ],
                    },
                    new()
                    {
                        TeamId = 2,
                        Score = 0,
                        Players =
                        [
                            new()
                            {
                                UserName = "Bob",
                                UserId = 9,
                                Figure = "hd-190-2",
                                Gender = "M",
                                Score = 0,
                                Stats = Stats(0, 0, 1, 0, 1, 3, 2, 1, 0, 0),
                            },
                        ],
                    },
                ],
                PlayerWithMostKills = 4,
                PlayerWithMostHits = 4,
            }
        );

    [Fact]
    public void player_exited_arena_writes_the_user_and_its_game_object() =>
        AssertWritesFixture(
            "Game2PlayerExitedGameArenaMessageComposer",
            new Game2PlayerExitedGameArenaMessageComposer { UserId = 9, PlayerGameObjectId = 21 }
        );

    [Fact]
    public void game_rejoin_writes_the_previous_room() =>
        AssertWritesFixture(
            "Game2GameRejoinMessageComposer",
            new Game2GameRejoinMessageComposer { RoomId = 1234 }
        );

    [Fact]
    public void player_rematches_writes_the_user_id() =>
        AssertWritesFixture(
            "Game2PlayerRematchesMessageComposer",
            new Game2PlayerRematchesMessageComposer { UserId = 4 }
        );

    [Fact]
    public void game_chat_writes_the_user_and_the_message() =>
        AssertWritesFixture(
            "Game2GameChatFromPlayerMessageComposer",
            new Game2GameChatFromPlayerMessageComposer { UserId = 4, Message = "salut à tous" }
        );

    [Fact]
    public void total_leaderboard_writes_entries_list_size_and_game_type() =>
        AssertWritesFixture(
            "Game2TotalLeaderboardMessageComposer",
            new Game2TotalLeaderboardMessageComposer
            {
                Entries = Entries,
                TotalListSize = 9,
                GameTypeId = 0,
            }
        );

    [Fact]
    public void friends_leaderboard_writes_the_total_shape() =>
        AssertWritesFixture(
            "Game2FriendsLeaderboardMessageComposer",
            new Game2FriendsLeaderboardMessageComposer
            {
                Entries = Entries,
                TotalListSize = 9,
                GameTypeId = 0,
            }
        );

    [Fact]
    public void total_group_leaderboard_adds_the_favourite_group() =>
        AssertWritesFixture(
            "Game2TotalGroupLeaderboardMessageComposer",
            new Game2TotalGroupLeaderboardMessageComposer
            {
                Entries = Entries,
                TotalListSize = 9,
                GameTypeId = 0,
                FavouriteGroupId = 3,
            }
        );

    [Fact]
    public void weekly_leaderboard_starts_with_the_week_header() =>
        AssertWritesFixture(
            "Game2WeeklyLeaderboardMessageComposer",
            new Game2WeeklyLeaderboardMessageComposer
            {
                Year = 2026,
                Week = 42,
                MaxOffset = 2,
                CurrentOffset = 0,
                MinutesUntilReset = 600,
                Entries = Entries,
                TotalListSize = 9,
                GameTypeId = 0,
            }
        );

    [Fact]
    public void weekly_friends_leaderboard_writes_the_weekly_shape() =>
        AssertWritesFixture(
            "Game2WeeklyFriendsLeaderboardMessageComposer",
            new Game2WeeklyFriendsLeaderboardMessageComposer
            {
                Year = 2026,
                Week = 42,
                MaxOffset = 2,
                CurrentOffset = 0,
                MinutesUntilReset = 600,
                Entries = Entries,
                TotalListSize = 9,
                GameTypeId = 0,
            }
        );

    [Fact]
    public void weekly_group_leaderboard_adds_the_favourite_group() =>
        AssertWritesFixture(
            "Game2WeeklyGroupLeaderboardMessageComposer",
            new Game2WeeklyGroupLeaderboardMessageComposer
            {
                Year = 2026,
                Week = 42,
                MaxOffset = 2,
                CurrentOffset = 0,
                MinutesUntilReset = 600,
                Entries = Entries,
                TotalListSize = 9,
                GameTypeId = 0,
                FavouriteGroupId = 3,
            }
        );

    [Fact]
    public void game_tokens_writes_each_offer() =>
        AssertWritesFixture(
            "SnowWarGameTokensMessageComposer",
            new SnowWarGameTokensMessageMessageComposer
            {
                Offers =
                [
                    new()
                    {
                        Id = 2,
                        LocalizationId = "GET_SNOWWAR_TOKENS",
                        PriceCredits = 10,
                        PricePoints = 0,
                        PointsType = 0,
                    },
                ],
            }
        );

    [Fact]
    public void load_stage_ready_reads_the_percentage()
    {
        var message = Parse<Game2LoadStageReadyMessage>("Game2LoadStageReadyMessageEvent");

        Assert.Equal(100, message.Percentage);
    }

    [Fact]
    public void exit_game_reads_whether_to_return_to_the_previous_room()
    {
        Assert.True(Parse<Game2ExitGameMessage>("Game2ExitGameMessageEvent").ExitToRoomBeforeGame);

        var withoutFlag = Assert.IsType<Game2ExitGameMessage>(
            PacketHarness.Parse(PacketHarness.Incoming("Game2ExitGameMessageEvent"), [])
        );
        Assert.True(withoutFlag.ExitToRoomBeforeGame);
    }

    [Fact]
    public void game_chat_reads_the_message() =>
        Assert.Equal(
            "salut à tous",
            Parse<Game2GameChatMessage>("Game2GameChatMessageEvent").Message
        );

    [Fact]
    public void play_again_has_no_payload() =>
        Parse<Game2PlayAgainMessage>("Game2PlayAgainMessageEvent");

    [Fact]
    public void check_directory_status_has_no_payload() =>
        Parse<Game2CheckGameDirectoryStatusMessage>("Game2CheckGameDirectoryStatusMessageEvent");

    [Fact]
    public void get_account_game_status_reads_the_game_type() =>
        Assert.Equal(
            0,
            Parse<Game2GetAccountGameStatusMessage>(
                "Game2GetAccountGameStatusMessageEvent"
            ).GameTypeId
        );

    [Fact]
    public void leave_game_has_no_payload() =>
        Parse<Game2LeaveGameMessage>("Game2LeaveGameMessageEvent");

    [Fact]
    public void quick_join_has_no_payload() =>
        Parse<Game2QuickJoinGameMessage>("Game2QuickJoinGameMessageEvent");

    [Fact]
    public void make_snowball_reads_turn_and_subturn()
    {
        var message = Parse<Game2MakeSnowballMessage>("Game2MakeSnowballMessageEvent");

        Assert.Equal((7, 2), (message.Turn, message.Subturn));
    }

    [Fact]
    public void request_full_status_reads_the_reason() =>
        Assert.Equal(
            1,
            Parse<Game2RequestFullStatusUpdateMessage>(
                "Game2RequestFullStatusUpdateMessageEvent"
            ).Reason
        );

    [Fact]
    public void set_move_target_reads_world_position_turn_and_subturn()
    {
        var message = Parse<Game2SetUserMoveTargetMessage>("Game2SetUserMoveTargetMessageEvent");

        Assert.Equal((3200, 6400, 7, 2), (message.X, message.Y, message.Turn, message.Subturn));
    }

    [Fact]
    public void throw_at_human_reads_target_trajectory_turn_and_subturn()
    {
        var message = Parse<Game2ThrowSnowballAtHumanMessage>(
            "Game2ThrowSnowballAtHumanMessageEvent"
        );

        Assert.Equal(
            (21, 3, 7, 2),
            (message.TargetHumanId, message.Trajectory, message.Turn, message.Subturn)
        );
    }

    [Fact]
    public void throw_at_position_reads_position_trajectory_turn_and_subturn()
    {
        var message = Parse<Game2ThrowSnowballAtPositionMessage>(
            "Game2ThrowSnowballAtPositionMessageEvent"
        );

        Assert.Equal(
            (9600, 3200, 1, 7, 2),
            (message.X, message.Y, message.Trajectory, message.Turn, message.Subturn)
        );
    }

    [Fact]
    public void total_leaderboard_request_reads_the_window()
    {
        var message = Parse<Game2GetTotalLeaderboardMessage>(
            "Game2GetTotalLeaderboardMessageEvent"
        );

        Assert.Equal(
            (0, -1, 0, 8, 50),
            (
                message.GameTypeId,
                message.StartRank,
                message.Direction,
                message.ViewSize,
                message.WindowSize
            )
        );
    }

    [Fact]
    public void friends_leaderboard_request_reads_the_window()
    {
        var message = Parse<Game2GetFriendsLeaderboardMessage>(
            "Game2GetFriendsLeaderboardMessageEvent"
        );

        Assert.Equal(
            (0, 1, 1, 8, 50),
            (
                message.GameTypeId,
                message.StartRank,
                message.Direction,
                message.ViewSize,
                message.WindowSize
            )
        );
    }

    [Fact]
    public void total_group_leaderboard_request_reads_the_window()
    {
        var message = Parse<Game2GetTotalGroupLeaderboardMessage>(
            "Game2GetTotalGroupLeaderboardMessageEvent"
        );

        Assert.Equal(
            (0, 5, 1, 8, 50),
            (
                message.GameTypeId,
                message.StartRank,
                message.Direction,
                message.ViewSize,
                message.WindowSize
            )
        );
    }

    [Fact]
    public void weekly_leaderboard_request_reads_the_week_offset_and_the_window()
    {
        var message = Parse<Game2GetWeeklyLeaderboardMessage>(
            "Game2GetWeeklyLeaderboardMessageEvent"
        );

        Assert.Equal(
            (0, 1, -1, 0, 8, 50),
            (
                message.GameTypeId,
                message.WeekOffset,
                message.StartRank,
                message.Direction,
                message.ViewSize,
                message.WindowSize
            )
        );
    }

    [Fact]
    public void weekly_friends_leaderboard_request_reads_the_week_offset_and_the_window()
    {
        var message = Parse<Game2GetWeeklyFriendsLeaderboardMessage>(
            "Game2GetWeeklyFriendsLeaderboardMessageEvent"
        );

        Assert.Equal(
            (0, 0, 1, 0, 8, 50),
            (
                message.GameTypeId,
                message.WeekOffset,
                message.StartRank,
                message.Direction,
                message.ViewSize,
                message.WindowSize
            )
        );
    }

    [Fact]
    public void weekly_group_leaderboard_request_reads_the_week_offset_and_the_window()
    {
        var message = Parse<Game2GetWeeklyGroupLeaderboardMessage>(
            "Game2GetWeeklyGroupLeaderboardMessageEvent"
        );

        Assert.Equal(
            (0, 1, 5, 1, 8, 50),
            (
                message.GameTypeId,
                message.WeekOffset,
                message.StartRank,
                message.Direction,
                message.ViewSize,
                message.WindowSize
            )
        );
    }

    private static SnowWarPlayerStatsSnapshot Stats(
        int score,
        int kills,
        int deaths,
        int snowballHits,
        int snowballHitsTaken,
        int snowballsThrown,
        int snowballsCreated,
        int snowballsFromMachine,
        int friendlyHits,
        int friendlyKills
    ) =>
        new()
        {
            Score = score,
            Kills = kills,
            Deaths = deaths,
            SnowballHits = snowballHits,
            SnowballHitsTaken = snowballHitsTaken,
            SnowballsThrown = snowballsThrown,
            SnowballsCreated = snowballsCreated,
            SnowballsFromMachine = snowballsFromMachine,
            FriendlyHits = friendlyHits,
            FriendlyKills = friendlyKills,
        };

    private static void AssertWritesFixture(string header, IComposer composer)
    {
        var packet = PacketHarness.Encode(composer);

        Assert.Equal(PacketHarness.Outgoing(header), packet.Header);
        Assert.Equal(Fixture(header), Convert.ToHexString(packet.PopBytes(packet.Remaining)));
    }

    private static T Parse<T>(string header)
        where T : IMessageEvent =>
        Assert.IsType<T>(
            PacketHarness.Parse(
                PacketHarness.Incoming(header),
                Convert.FromHexString(Fixture(header))
            )
        );

    private static string Fixture(string name)
    {
        using var stream =
            typeof(SnowWarPacketTests).Assembly.GetManifestResourceStream(
                $"Protocol.Fixtures.snowwar.{name}.hex"
            ) ?? throw new InvalidOperationException($"The fixture {name} is missing.");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Trim();
    }
}
