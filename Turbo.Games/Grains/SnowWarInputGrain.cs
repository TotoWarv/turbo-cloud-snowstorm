using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Games.Configuration;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Orleans;

namespace Turbo.Games.Grains;

internal sealed class SnowWarInputGrain : Grain, ISnowWarInputGrain
{
    private static readonly TimeSpan InputWindow = TimeSpan.FromSeconds(1);

    private readonly SnowWarConfig _config;
    private readonly IGrainFactory _grainFactory;
    private readonly TimeProvider _time;

    private readonly SnowWarInputLiveState _state;

    public SnowWarInputGrain(
        IOptions<SnowWarConfig> config,
        IGrainFactory grainFactory,
        TimeProvider time
    )
    {
        _config = config.Value;
        _grainFactory = grainFactory;
        _time = time;

        _state = new() { PlayerId = this.GetPlayerId() };
    }

    public async Task QueueInputAsync(
        SnowWarInput input,
        int[] values,
        int turn,
        int subturn,
        CancellationToken ct
    )
    {
        if (!TryAdmit() || await GameAsync(ct) is not { } game)
            return;

        await game.QueueInputAsync(_state.PlayerId, input, values, turn, subturn, ct);
    }

    public async Task RequestFullStatusAsync(CancellationToken ct)
    {
        if (!TryAdmit() || await GameAsync(ct) is not { } game)
            return;

        await game.RequestFullStatusAsync(_state.PlayerId, ct);
    }

    private bool TryAdmit()
    {
        var now = _time.GetUtcNow();

        if (now - _state.WindowStartedAt >= InputWindow)
        {
            _state.WindowStartedAt = now;
            _state.InputsInWindow = 0;
        }

        if (_state.InputsInWindow >= _config.MaxInputsPerSecond)
            return false;

        _state.InputsInWindow++;

        return true;
    }

    private async Task<ISnowWarGameGrain?> GameAsync(CancellationToken ct)
    {
        var gameId = await _grainFactory
            .GetSnowWarDirectoryGrain()
            .GetGameIdOfPlayerAsync(_state.PlayerId, ct);

        return gameId is { } id ? _grainFactory.GetSnowWarGameGrain(id) : null;
    }
}
