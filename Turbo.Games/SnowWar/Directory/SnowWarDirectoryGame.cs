using System.Collections.Immutable;
using Turbo.Primitives.Players;

namespace Turbo.Games.SnowWar.Directory;

internal sealed record SnowWarDirectoryGame
{
    public required int Id { get; init; }

    public required ImmutableArray<PlayerId> Players { get; init; }

    public bool Finished { get; init; }
}
