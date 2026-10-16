using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarGameGrain
{
    private const int MAX_CHAT_LENGTH = 100;
    private const int CHAT_INTERVAL_MILLISECONDS = 750;
    private const int FULL_STATUS_INTERVAL_SECONDS = 1;
    private const int FULL_PERCENTAGE = 100;
    private const int PLAYERS_TO_GO_ON = 2;

    public async Task StageReadyAsync(PlayerId playerId, CancellationToken ct)
    {
        if (
            _state.Phase != SnowWarGamePhase.Loading
            || PresentParticipant(playerId) is not { IsReady: false } participant
        )
            return;

        Store(participant with { IsReady = true });

        var present = Present.ToList();
        var ready = present.Where(p => p.IsReady).ToList();

        await SendAsync(
            PresentIds,
            new Game2StageStillLoadingMessageComposer
            {
                Percentage = ready.Count * FULL_PERCENTAGE / present.Count,
                FinishedUserIds = [.. ready.Select(p => p.Player.UserId)],
            },
            ct
        );

        if (ready.Count == present.Count)
            await BeginStageAsync(ct);
    }

    public Task QueueInputAsync(
        PlayerId playerId,
        SnowWarInput input,
        int[] values,
        int turn,
        int subturn,
        CancellationToken ct
    )
    {
        if (
            values is not null
            && _state.Phase == SnowWarGamePhase.Running
            && PresentParticipant(playerId) is { } participant
        )
            Arena.ScheduleInput(participant.HumanId, input, values, turn, subturn);

        return Task.CompletedTask;
    }

    public Task RequestFullStatusAsync(PlayerId playerId, CancellationToken ct)
    {
        if (
            _state.Phase == SnowWarGamePhase.Running
            && PresentParticipant(playerId) is { } participant
            && Now - participant.LastFullStatusRequestAt
                >= TimeSpan.FromSeconds(FULL_STATUS_INTERVAL_SECONDS)
        )
            Store(participant with { AwaitsFullStatus = true, LastFullStatusRequestAt = Now });

        return Task.CompletedTask;
    }

    public async Task ChatAsync(PlayerId playerId, string message, CancellationToken ct)
    {
        var text = message?.Trim() ?? string.Empty;

        if (
            text.Length == 0
            || PresentParticipant(playerId) is not { } participant
            || Now - participant.LastChatAt < TimeSpan.FromMilliseconds(CHAT_INTERVAL_MILLISECONDS)
        )
            return;

        Store(participant with { LastChatAt = Now });

        var cut = text.Length > MAX_CHAT_LENGTH ? text[..MAX_CHAT_LENGTH] : text;

        await SendAsync(
            PresentIds,
            new Game2GameChatFromPlayerMessageComposer
            {
                UserId = participant.Player.UserId,
                Message = _wordFilter.Filter(cut),
            },
            ct
        );
    }

    public async Task PlayAgainAsync(PlayerId playerId, CancellationToken ct)
    {
        if (
            _state.Phase != SnowWarGamePhase.Ended
            || PresentParticipant(playerId) is not { WantsRematch: false } participant
        )
            return;

        Store(participant with { WantsRematch = true });

        await SendAsync(
            PresentIds,
            new Game2PlayerRematchesMessageComposer { UserId = participant.Player.UserId },
            ct
        );
    }

    public async Task ExitAsync(PlayerId playerId, CancellationToken ct)
    {
        if (
            _state.Phase is SnowWarGamePhase.Idle or SnowWarGamePhase.Closed
            || PresentParticipant(playerId) is not { } participant
        )
            return;

        var phase = _state.Phase;

        Store(participant with { HasLeft = true, AwaitsFullStatus = false });

        if (phase != SnowWarGamePhase.Ended && Arena.RemoveHuman(participant.HumanId))
            await SendAsync(
                PresentIds,
                new Game2PlayerExitedGameArenaMessageComposer
                {
                    UserId = participant.Player.UserId,
                    PlayerGameObjectId = participant.HumanId,
                },
                ct
            );

        await SendAsync(
            playerId,
            new Game2GameRejoinMessageComposer { RoomId = participant.PreviousRoomId },
            ct
        );
        await Directory.OnPlayerLeftGameAsync(
            _state.GameId,
            playerId,
            phase == SnowWarGamePhase.Running,
            ct
        );

        await AfterDepartureAsync(ct);
    }

    private async Task AfterDepartureAsync(CancellationToken ct)
    {
        var remaining = Present.ToList();

        if (remaining.Count == 0)
        {
            await CloseAsync(ct);
            return;
        }

        if (_state.Phase == SnowWarGamePhase.Ended)
            return;

        if (remaining.Count < Math.Min(PLAYERS_TO_GO_ON, _config.MinPlayers))
        {
            await EndGameAsync(ct);
            return;
        }

        if (_state.Phase == SnowWarGamePhase.Loading && remaining.All(p => p.IsReady))
            await BeginStageAsync(ct);
    }
}
