using System.Collections.Immutable;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Simulation;

internal sealed record SnowWarScheduledEvent(
    int Turn,
    int Subturn,
    SnowWarGameEventType Type,
    ImmutableArray<int> Fields
)
{
    public SnowWarGameEventSnapshot ToSnapshot() => new() { Type = Type, Fields = Fields };

    public bool Names(int fieldIndex, int value) =>
        fieldIndex < Fields.Length && Fields[fieldIndex] == value;
}
