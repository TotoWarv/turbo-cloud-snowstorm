using System;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Games.SnowWar.Game;

internal sealed record SnowWarParticipant
{
    public required SnowWarPlayerSnapshot Player { get; init; }

    public required int HumanId { get; init; }

    public required int PreviousRoomId { get; init; }

    public bool IsReady { get; init; }

    public bool HasLeft { get; init; }

    public bool WantsRematch { get; init; }

    public bool AwaitsFullStatus { get; init; }

    public DateTimeOffset LastChatAt { get; init; } = DateTimeOffset.MinValue;

    public DateTimeOffset LastFullStatusRequestAt { get; init; } = DateTimeOffset.MinValue;

    public PlayerId PlayerId => Player.UserId;
}
