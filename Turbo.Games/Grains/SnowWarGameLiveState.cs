using System;
using System.Collections.Generic;
using Turbo.Games.SnowWar.Game;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.Grains;

internal sealed class SnowWarGameLiveState
{
    public int GameId { get; set; }

    public int FieldType { get; set; }

    public SnowWarGamePhase Phase { get; set; } = SnowWarGamePhase.Idle;

    public SnowWarArena? Arena { get; set; }

    public List<SnowWarParticipant> Participants { get; } = [];

    public DateTimeOffset PhaseStartedAt { get; set; }

    public int TurnsRun { get; set; }

    public bool FinishReported { get; set; }
}
