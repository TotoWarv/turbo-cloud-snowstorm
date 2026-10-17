using System;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed class SnowWarInputLiveState
{
    public PlayerId PlayerId { get; init; }

    public DateTimeOffset WindowStartedAt { get; set; }

    public int InputsInWindow { get; set; }
}
