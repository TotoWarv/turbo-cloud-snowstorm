using System.Reflection;
using FluentAssertions;
using Orleans.Concurrency;
using Turbo.Games;
using Turbo.Games.Configuration;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Players;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarInputGrainTests
{
    private const string GRAIN_TYPE = "Turbo.Games.Grains.SnowWarInputGrain";
    private const int FLOODER = 1;
    private const int FLOODER_GAME = 7;
    private const int OTHER_PLAYER = 3;
    private const int OTHER_GAME = 8;
    private const int BURST = 50;

    private readonly Fakes _fakes = new();
    private readonly SnowWarConfig _config = new();
    private readonly ManualTimeProvider _clock = new(
        new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero)
    );
    private int _lookups;

    public SnowWarInputGrainTests()
    {
        _fakes.Handlers[nameof(ISnowWarDirectoryGrain.GetGameIdOfPlayerAsync)] = call =>
        {
            _lookups++;

            return Task.FromResult<int?>(
                PlayerIdOf(call.Args[0]!) switch
                {
                    FLOODER => FLOODER_GAME,
                    OTHER_PLAYER => OTHER_GAME,
                    _ => null,
                }
            );
        };
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static int PlayerIdOf(object playerId) => ((PlayerId)playerId).Value;

    private ISnowWarInputGrain InputOf(int playerId)
    {
        var grain = GrainHarness.Create(
            typeof(GamesModule).Assembly,
            GRAIN_TYPE,
            _fakes,
            playerId: playerId
        );

        RoomHarness.SetField(grain, "_time", _clock);
        RoomHarness.SetField(grain, "_config", _config);

        return (ISnowWarInputGrain)grain;
    }

    private int InputsReaching(int gameId) =>
        _fakes
            .Log.Of(nameof(ISnowWarGameGrain.QueueInputAsync))
            .Count(call =>
                call.Interface == typeof(ISnowWarGameGrain) && Convert.ToInt64(call.Key) == gameId
            );

    private static async Task BurstAsync(ISnowWarInputGrain input, int count)
    {
        for (var i = 0; i < count; i++)
            await input.QueueInputAsync(SnowWarInput.MakeSnowball, [], i, 0, Ct);
    }

    [Fact]
    public void the_game_lookup_does_not_wait_behind_the_directory_turns() =>
        typeof(ISnowWarDirectoryGrain)
            .GetMethod(nameof(ISnowWarDirectoryGrain.GetGameIdOfPlayerAsync))!
            .GetCustomAttribute<AlwaysInterleaveAttribute>()
            .Should()
            .NotBeNull();

    [Fact]
    public async Task a_burst_past_the_cap_is_dropped_before_any_grain_call()
    {
        await BurstAsync(InputOf(FLOODER), BURST);

        InputsReaching(FLOODER_GAME).Should().Be(_config.MaxInputsPerSecond);
        _lookups.Should().Be(_config.MaxInputsPerSecond);
    }

    [Fact]
    public async Task a_flooding_player_leaves_another_games_inputs_untouched()
    {
        var flooder = InputOf(FLOODER);
        var other = InputOf(OTHER_PLAYER);

        await BurstAsync(flooder, BURST);
        await other.QueueInputAsync(SnowWarInput.MoveTarget, [6400, 9600], 3, 1, Ct);
        await other.RequestFullStatusAsync(Ct);

        var forwarded = _fakes
            .Log.Of(nameof(ISnowWarGameGrain.QueueInputAsync))
            .Where(call => Convert.ToInt64(call.Key) == OTHER_GAME)
            .Should()
            .ContainSingle()
            .Which;

        PlayerIdOf(forwarded.Args[0]!).Should().Be(OTHER_PLAYER);
        forwarded.Args[1].Should().Be(SnowWarInput.MoveTarget);
        ((int[])forwarded.Args[2]!).Should().Equal(6400, 9600);
        _fakes
            .Log.Of(nameof(ISnowWarGameGrain.RequestFullStatusAsync))
            .Should()
            .ContainSingle(call => Convert.ToInt64(call.Key) == OTHER_GAME);
    }

    [Fact]
    public async Task full_status_requests_share_the_same_cap()
    {
        var flooder = InputOf(FLOODER);

        for (var i = 0; i < BURST; i++)
            await flooder.RequestFullStatusAsync(Ct);

        _fakes
            .Log.Of(nameof(ISnowWarGameGrain.RequestFullStatusAsync))
            .Count(call => call.Interface == typeof(ISnowWarGameGrain))
            .Should()
            .Be(_config.MaxInputsPerSecond);
    }

    [Fact]
    public async Task the_cap_opens_again_a_second_later()
    {
        var flooder = InputOf(FLOODER);

        await BurstAsync(flooder, BURST);
        _clock.Advance(TimeSpan.FromSeconds(1));
        await BurstAsync(flooder, 1);

        InputsReaching(FLOODER_GAME).Should().Be(_config.MaxInputsPerSecond + 1);
    }

    [Fact]
    public async Task a_player_outside_any_game_reaches_no_game()
    {
        await BurstAsync(InputOf(5), 1);

        _fakes.Log.On<ISnowWarGameGrain>().Should().BeEmpty();
    }
}
