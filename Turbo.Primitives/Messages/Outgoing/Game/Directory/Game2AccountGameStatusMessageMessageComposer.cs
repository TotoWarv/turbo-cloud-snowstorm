using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Directory;

[GenerateSerializer, Immutable]
public sealed record Game2AccountGameStatusMessageMessageComposer : IComposer
{
    [Id(0)]
    public required int GameTypeId { get; init; }

    [Id(1)]
    public required int FreeGamesLeft { get; init; }

    [Id(2)]
    public required int GamesPlayedTotal { get; init; }
}
