using System.Collections.Immutable;
using Orleans;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarGameLobbySnapshot
{
    [Id(0)]
    public required int GameId { get; init; }

    [Id(1)]
    public required string LevelName { get; init; }

    [Id(2)]
    public required int GameType { get; init; }

    [Id(3)]
    public required int FieldType { get; init; }

    [Id(4)]
    public required int NumberOfTeams { get; init; }

    [Id(5)]
    public required int MaximumPlayers { get; init; }

    [Id(6)]
    public required string OwningPlayerName { get; init; }

    [Id(7)]
    public required int LevelEntryId { get; init; }

    [Id(8)]
    public required ImmutableArray<SnowWarPlayerSnapshot> Players { get; init; }
}
