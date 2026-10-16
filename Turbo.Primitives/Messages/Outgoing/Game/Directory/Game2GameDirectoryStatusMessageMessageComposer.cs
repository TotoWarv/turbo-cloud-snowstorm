using Orleans;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2GameDirectoryStatusMessageMessageComposer : IComposer
{
    [Id(0)]
    public required SnowWarDirectoryStatus Status { get; init; }

    [Id(1)]
    public required int BlockSeconds { get; init; }

    [Id(2)]
    public required int GamesPlayed { get; init; }

    [Id(3)]
    public required int FreeGamesLeft { get; init; }
}
