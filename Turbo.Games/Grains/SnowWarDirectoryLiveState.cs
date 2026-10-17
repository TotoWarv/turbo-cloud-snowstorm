using System;
using System.Collections.Generic;
using Turbo.Games.SnowWar.Directory;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed class SnowWarDirectoryLiveState
{
    public int LastId { get; set; }

    public int LastFieldType { get; set; }

    public List<SnowWarLobby> Lobbies { get; } = [];

    public List<int> ArenaQueue { get; } = [];

    public Dictionary<int, SnowWarDirectoryGame> Games { get; } = [];

    public Dictionary<PlayerId, int> GameIdOfPlayer { get; } = [];

    public Dictionary<PlayerId, DateTimeOffset> BlockedUntil { get; } = [];
}
