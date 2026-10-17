using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Games.SnowWar.Directory;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarDirectoryGrain
{
    private const int GAME_TYPE = 0;

    public async Task QuickJoinAsync(
        PlayerId playerId,
        SnowWarPlayerSnapshot player,
        CancellationToken ct
    )
    {
        if (await RefuseJoinAsync(playerId, ct))
            return;

        if (GameOf(playerId) is { } finishedGame)
            RemoveFromGame(finishedGame, playerId);

        if (LobbyOf(playerId) is { } current)
        {
            await SendAsync(
                playerId,
                new Game2GameLongDataMessageComposer { Lobby = Snapshot(current) },
                ct
            );
            await SendCountdownStateAsync(playerId, current, ct);

            return;
        }

        await JoinLobbyAsync(playerId, player, ct);
    }

    public async Task LeaveLobbyAsync(PlayerId playerId, CancellationToken ct)
    {
        if (LobbyOf(playerId) is not { } lobby)
            return;

        var remaining = lobby with
        {
            Players = lobby.Players.RemoveAll(p => p.UserId == playerId.Value),
        };

        if (remaining.Players.IsEmpty)
            RemoveLobby(lobby.Id);
        else
            await AnnounceLeaverAsync(remaining, playerId, ct);

        await AdvanceQueueAsync(ct);
    }

    private async Task<bool> RefuseJoinAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_arenas.Arenas.IsEmpty)
        {
            await SendAsync(
                playerId,
                new Game2JoiningGameFailedMessageMessageComposer
                {
                    Reason = SnowWarJoinFailureReason.Kicked,
                },
                ct
            );

            return true;
        }

        var blockSeconds = BlockSecondsOf(playerId);

        if (blockSeconds > 0)
        {
            await SendAsync(
                playerId,
                new Game2UserBlockedMessageMessageComposer { BlockSeconds = blockSeconds },
                ct
            );

            return true;
        }

        if (GameOf(playerId) is not { Finished: false })
            return false;

        await SendAsync(
            playerId,
            new Game2JoiningGameFailedMessageMessageComposer
            {
                Reason = SnowWarJoinFailureReason.UserHasActiveInstance,
            },
            ct
        );

        return true;
    }

    private async Task JoinLobbyAsync(
        PlayerId playerId,
        SnowWarPlayerSnapshot player,
        CancellationToken ct
    )
    {
        var existing = _state.Lobbies.FirstOrDefault(l => l.Players.Length < _config.MaxPlayers);
        var lobby = existing ?? CreateLobby();
        var joiner = player with
        {
            UserId = playerId.Value,
            TeamId = SnowWarLobbyTeams.Smallest(lobby.Players),
        };
        var joined = lobby with { Players = lobby.Players.Add(joiner) };

        StoreLobby(joined);

        await SendAsync(
            lobby.PlayerIds,
            new Game2UserJoinedGameMessageComposer { Player = joiner, WasTeamSwitched = false },
            ct
        );
        await SendAsync(
            playerId,
            existing is null
                ? new Game2GameCreatedMessageComposer { Lobby = Snapshot(joined) }
                : new Game2GameLongDataMessageComposer { Lobby = Snapshot(joined) },
            ct
        );
        await SendCountdownStateAsync(playerId, joined, ct);
        await UpdateLobbyAsync(joined.Id, ct);
    }

    private async Task AnnounceLeaverAsync(
        SnowWarLobby remaining,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        StoreLobby(remaining);

        await SendAsync(
            remaining.PlayerIds,
            new Game2UserLeftGameMessageMessageComposer { UserId = playerId.Value },
            ct
        );
        await UpdateLobbyAsync(remaining.Id, ct);
    }

    private SnowWarLobby CreateLobby()
    {
        var fieldType = _arenas.Next(_state.LastFieldType);

        _state.LastFieldType = fieldType;

        return new SnowWarLobby
        {
            Id = ++_state.LastId,
            FieldType = fieldType,
            LevelName = _arenas.Get(fieldType).Name,
        };
    }

    private async Task UpdateLobbyAsync(int lobbyId, CancellationToken ct)
    {
        if (FindLobby(lobbyId) is not { } lobby)
            return;

        if (lobby.Players.Length < _config.MinPlayers)
            await StopCountdownAsync(lobby, ct);
        else if (lobby.Phase == SnowWarLobbyPhase.Waiting)
            await StartCountdownAsync(lobby, ct);
        else if (lobby.Phase == SnowWarLobbyPhase.Counting && Now >= lobby.CountdownEndsAt)
            EnqueueForArena(lobby);
    }

    private async Task StartCountdownAsync(SnowWarLobby lobby, CancellationToken ct)
    {
        StoreLobby(
            lobby with
            {
                Phase = SnowWarLobbyPhase.Counting,
                CountdownEndsAt = Now.AddSeconds(_config.LobbyCountdownSeconds),
            }
        );

        await SendAsync(
            lobby.PlayerIds,
            new Game2StartCounterMessageMessageComposer
            {
                CountDownSeconds = _config.LobbyCountdownSeconds,
            },
            ct
        );
    }

    private async Task StopCountdownAsync(SnowWarLobby lobby, CancellationToken ct)
    {
        if (lobby.Phase == SnowWarLobbyPhase.Waiting)
            return;

        _state.ArenaQueue.Remove(lobby.Id);
        StoreLobby(lobby with { Phase = SnowWarLobbyPhase.Waiting, AnnouncedQueuePosition = 0 });

        await SendAsync(lobby.PlayerIds, new Game2StopCounterMessageMessageComposer(), ct);
    }

    private void EnqueueForArena(SnowWarLobby lobby)
    {
        StoreLobby(lobby with { Phase = SnowWarLobbyPhase.Queued });
        _state.ArenaQueue.Add(lobby.Id);
    }

    private Task SendCountdownStateAsync(
        PlayerId playerId,
        SnowWarLobby lobby,
        CancellationToken ct
    ) =>
        lobby.Phase switch
        {
            SnowWarLobbyPhase.Queued => SendAsync(
                playerId,
                new Game2InArenaQueueMessageMessageComposer
                {
                    Position = QueuePositionOf(lobby.Id),
                },
                ct
            ),
            SnowWarLobbyPhase.Counting => SendAsync(
                playerId,
                new Game2StartCounterMessageMessageComposer
                {
                    CountDownSeconds = Math.Max(0, CeilingSeconds(lobby.CountdownEndsAt - Now)),
                },
                ct
            ),
            _ => Task.CompletedTask,
        };

    private SnowWarGameLobbySnapshot Snapshot(SnowWarLobby lobby) =>
        new()
        {
            GameId = lobby.Id,
            LevelName = lobby.LevelName,
            GameType = GAME_TYPE,
            FieldType = lobby.FieldType,
            NumberOfTeams = SnowWarLobbyTeams.TEAM_COUNT,
            MaximumPlayers = _config.MaxPlayers,
            OwningPlayerName = lobby.Players.IsEmpty ? string.Empty : lobby.Players[0].Name,
            LevelEntryId = lobby.FieldType,
            Players = lobby.Players,
        };

    private SnowWarLobby? LobbyOf(PlayerId playerId) =>
        _state.Lobbies.FirstOrDefault(l => l.Contains(playerId));

    private SnowWarLobby? FindLobby(int lobbyId) =>
        _state.Lobbies.FirstOrDefault(l => l.Id == lobbyId);

    private void StoreLobby(SnowWarLobby lobby)
    {
        var index = _state.Lobbies.FindIndex(l => l.Id == lobby.Id);

        if (index < 0)
            _state.Lobbies.Add(lobby);
        else
            _state.Lobbies[index] = lobby;
    }

    private void RemoveLobby(int lobbyId)
    {
        _state.Lobbies.RemoveAll(l => l.Id == lobbyId);
        _state.ArenaQueue.Remove(lobbyId);
    }
}
