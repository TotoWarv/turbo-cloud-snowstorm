using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;
using Turbo.Games.SnowWar.Directory;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

[KeepAlive]
internal sealed partial class SnowWarDirectoryGrain : Grain, ISnowWarDirectoryGrain
{
    private const int UNLIMITED_FREE_GAMES = -1;
    private const int LOBBY_TICK_MILLISECONDS = 250;

    private readonly SnowWarConfig _config;
    private readonly SnowWarArenaCatalog _arenas;
    private readonly IGrainFactory _grainFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<ISnowWarDirectoryGrain> _logger;

    private readonly SnowWarDirectoryLiveState _state = new();

    private IDisposable? _lobbyTimer;

    public SnowWarDirectoryGrain(
        IOptions<SnowWarConfig> config,
        SnowWarArenaCatalog arenas,
        IGrainFactory grainFactory,
        TimeProvider time,
        ILogger<ISnowWarDirectoryGrain> logger
    )
    {
        _config = config.Value;
        _arenas = arenas;
        _grainFactory = grainFactory;
        _time = time;
        _logger = logger;
    }

    private DateTimeOffset Now => _time.GetUtcNow();

    public override Task OnActivateAsync(CancellationToken ct)
    {
        var period = TimeSpan.FromMilliseconds(LOBBY_TICK_MILLISECONDS);

        _lobbyTimer = this.RegisterGrainTimer<object?>(
            static async (self, ct) => await ((SnowWarDirectoryGrain)self!).OnLobbyTimerAsync(ct),
            this,
            period,
            period
        );

        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _lobbyTimer?.Dispose();
        _lobbyTimer = null;

        return Task.CompletedTask;
    }

    public Task<SnowWarAccountStatusSnapshot> GetAccountStatusAsync(
        PlayerId playerId,
        CancellationToken ct
    ) =>
        Task.FromResult(
            new SnowWarAccountStatusSnapshot
            {
                Status = _arenas.Arenas.IsEmpty
                    ? SnowWarDirectoryStatus.Unavailable
                    : SnowWarDirectoryStatus.Available,
                BlockSeconds = _arenas.Arenas.IsEmpty ? 0 : BlockSecondsOf(playerId),
                FreeGamesLeft = UNLIMITED_FREE_GAMES,
            }
        );

    public Task<int?> GetGameIdOfPlayerAsync(PlayerId playerId, CancellationToken ct) =>
        Task.FromResult(GameOf(playerId)?.Id);

    public async Task OnPlayerLeftGameAsync(
        int gameId,
        PlayerId playerId,
        bool duringPlay,
        CancellationToken ct
    )
    {
        if (!_state.Games.TryGetValue(gameId, out var game) || !game.Players.Contains(playerId))
            return;

        RemoveFromGame(game, playerId);

        if (duringPlay)
            await BlockAsync(playerId, ct);

        await AdvanceQueueAsync(ct);
    }

    public async Task OnGameFinishedAsync(int gameId, CancellationToken ct)
    {
        if (!_state.Games.TryGetValue(gameId, out var game) || game.Finished)
            return;

        _state.Games[gameId] = game with { Finished = true };

        await AdvanceQueueAsync(ct);
    }

    private async Task OnLobbyTimerAsync(CancellationToken ct)
    {
        ForgetExpiredBlocks();

        foreach (var lobbyId in _state.Lobbies.Select(l => l.Id).ToList())
            await UpdateLobbyAsync(lobbyId, ct);

        await AdvanceQueueAsync(ct);
    }

    private async Task BlockAsync(PlayerId playerId, CancellationToken ct)
    {
        _state.BlockedUntil[playerId] = Now.AddSeconds(_config.LeaveBlockSeconds);

        await SendAsync(
            playerId,
            new Game2UserBlockedMessageMessageComposer { BlockSeconds = _config.LeaveBlockSeconds },
            ct
        );
    }

    private int BlockSecondsOf(PlayerId playerId) =>
        _state.BlockedUntil.TryGetValue(playerId, out var until) && until > Now
            ? CeilingSeconds(until - Now)
            : 0;

    private void ForgetExpiredBlocks()
    {
        foreach (var expired in _state.BlockedUntil.Where(b => b.Value <= Now).ToList())
            _state.BlockedUntil.Remove(expired.Key);
    }

    private SnowWarDirectoryGame? GameOf(PlayerId playerId) =>
        _state.Games.Values.FirstOrDefault(g => g.Players.Contains(playerId));

    private void RemoveFromGame(SnowWarDirectoryGame game, PlayerId playerId)
    {
        var remaining = game.Players.Remove(playerId);

        if (remaining.IsEmpty)
            _state.Games.Remove(game.Id);
        else
            _state.Games[game.Id] = game with { Players = remaining };
    }

    private static int CeilingSeconds(TimeSpan remaining) =>
        (int)Math.Ceiling(remaining.TotalSeconds);

    private Task SendAsync(PlayerId playerId, IComposer composer, CancellationToken ct) =>
        _grainFactory.SendComposerToPlayerAsync(playerId, composer, ct);

    private Task SendAsync(
        IEnumerable<PlayerId> playerIds,
        IComposer composer,
        CancellationToken ct
    ) => _grainFactory.SendComposerToPlayersAsync(playerIds, composer, ct);
}
