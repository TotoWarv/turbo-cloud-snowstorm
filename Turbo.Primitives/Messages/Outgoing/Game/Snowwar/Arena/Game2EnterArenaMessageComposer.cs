using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2EnterArenaMessageComposer : IComposer
{
    [Id(0)]
    public required int GameType { get; init; }

    [Id(1)]
    public required int FieldType { get; init; }

    [Id(2)]
    public required int NumberOfTeams { get; init; }

    [Id(3)]
    public required ImmutableArray<SnowWarArenaPlayerSnapshot> Players { get; init; }

    [Id(4)]
    public required SnowWarLevelSnapshot Level { get; init; }
}
