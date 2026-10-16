using System;
using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Games.SnowWar.Directory;

internal sealed record SnowWarLobby
{
    public required int Id { get; init; }

    public required int FieldType { get; init; }

    public required string LevelName { get; init; }

    public ImmutableArray<SnowWarPlayerSnapshot> Players { get; init; } = [];

    public SnowWarLobbyPhase Phase { get; init; } = SnowWarLobbyPhase.Waiting;

    public DateTimeOffset CountdownEndsAt { get; init; }

    public int AnnouncedQueuePosition { get; init; }

    public ImmutableArray<PlayerId> PlayerIds => [.. Players.Select(p => (PlayerId)p.UserId)];

    public bool Contains(PlayerId playerId) => Players.Any(p => p.UserId == playerId.Value);
}
