using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Games.SnowWar.Directory;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Directory;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarDirectoryGrain
{
    public async Task RematchAsync(
        int gameId,
        ImmutableArray<SnowWarPlayerSnapshot> players,
        CancellationToken ct
    )
    {
        if (_arenas.Arenas.IsEmpty)
            return;

        var rematchers = players
            .Where(p => IsInGame(gameId, p.UserId) && LobbyOf(p.UserId) is null)
            .Take(_config.MaxPlayers)
            .ToImmutableArray();

        foreach (var rematcher in rematchers)
        {
            if (_state.Games.TryGetValue(gameId, out var game))
                RemoveFromGame(game, rematcher.UserId);
        }

        if (rematchers.IsEmpty)
            return;

        var lobby = CreateLobby();

        foreach (var rematcher in rematchers)
            lobby = await AddRematcherAsync(lobby, rematcher, ct);

        await UpdateLobbyAsync(lobby.Id, ct);
        await AdvanceQueueAsync(ct);
    }

    private async Task<SnowWarLobby> AddRematcherAsync(
        SnowWarLobby lobby,
        SnowWarPlayerSnapshot rematcher,
        CancellationToken ct
    )
    {
        var joined = lobby with { Players = lobby.Players.Add(rematcher) };

        StoreLobby(joined);

        await SendAsync(
            lobby.PlayerIds,
            new Game2UserJoinedGameMessageComposer { Player = rematcher, WasTeamSwitched = false },
            ct
        );
        await SendAsync(
            (PlayerId)rematcher.UserId,
            new Game2GameCreatedMessageComposer { Lobby = Snapshot(joined) },
            ct
        );

        return joined;
    }

    private bool IsInGame(int gameId, PlayerId playerId) =>
        _state.Games.TryGetValue(gameId, out var game) && game.Players.Contains(playerId);
}
