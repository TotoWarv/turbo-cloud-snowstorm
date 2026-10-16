using Orleans;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Primitives.Game.Snapshots;

[GenerateSerializer, Immutable]
public sealed record SnowWarAccountStatusSnapshot
{
    [Id(0)]
    public required SnowWarDirectoryStatus Status { get; init; }

    [Id(1)]
    public required int BlockSeconds { get; init; }

    [Id(2)]
    public required int FreeGamesLeft { get; init; }
}
