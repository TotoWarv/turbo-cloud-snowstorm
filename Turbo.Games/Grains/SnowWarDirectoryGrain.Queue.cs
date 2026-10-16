using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Games.SnowWar.Directory;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarDirectoryGrain
{
    private int RunningGameCount => _state.Games.Values.Count(g => !g.Finished);

    private int QueuePositionOf(int lobbyId) => _state.ArenaQueue.IndexOf(lobbyId) + 1;

    private async Task AdvanceQueueAsync(CancellationToken ct)
    {
        while (_state.ArenaQueue.Count > 0 && RunningGameCount < _config.MaxConcurrentGames)
        {
            var lobbyId = _state.ArenaQueue[0];
            var lobby = FindLobby(lobbyId);

            RemoveLobby(lobbyId);

            if (lobby is not null)
                await StartGameAsync(lobby, ct);
        }

        await AnnounceQueuePositionsAsync(ct);
    }

    private async Task AnnounceQueuePositionsAsync(CancellationToken ct)
    {
        foreach (var lobbyId in _state.ArenaQueue.ToList())
        {
            var position = QueuePositionOf(lobbyId);

            if (FindLobby(lobbyId) is not { } lobby || lobby.AnnouncedQueuePosition == position)
                continue;

            StoreLobby(lobby with { AnnouncedQueuePosition = position });

            await SendAsync(
                lobby.PlayerIds,
                new Game2InArenaQueueMessageMessageComposer { Position = position },
                ct
            );
        }
    }

    private async Task StartGameAsync(SnowWarLobby lobby, CancellationToken ct)
    {
        var snapshot = Snapshot(
            lobby with
            {
                Players = SnowWarLobbyTeams.Rebalance(lobby.Players),
            }
        );

        _state.Games[lobby.Id] = new SnowWarDirectoryGame
        {
            Id = lobby.Id,
            Players = lobby.PlayerIds,
        };

        await SendAsync(
            lobby.PlayerIds,
            new Game2GameStartedMessageComposer { Lobby = snapshot },
            ct
        );

        try
        {
            await _grainFactory.GetSnowWarGameGrain(lobby.Id).StartAsync(snapshot, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "SnowWar game {GameId} failed to start", lobby.Id);

            await CancelGameAsync(snapshot, ct);
        }
    }

    private async Task CancelGameAsync(SnowWarGameLobbySnapshot game, CancellationToken ct)
    {
        _state.Games.Remove(game.GameId);

        await SendAsync(
            game.Players.Select(p => (PlayerId)p.UserId),
            new Game2StartingGameFailedMessageMessageComposer
            {
                Reason = SnowWarJoinFailureReason.Kicked,
            },
            ct
        );
    }
}
