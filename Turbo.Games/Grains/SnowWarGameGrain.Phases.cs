using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Games.SnowWar.Game;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Ingame;
using Turbo.Primitives.Orleans;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarGameGrain
{
    private const int TURN_MILLISECONDS = 150;
    private const int MAX_TURNS_PER_TICK = 10;
    private const int STAGE_ENDING_DELAY = 0;

    private int TotalTurns => _config.GameLengthSeconds * 1000 / TURN_MILLISECONDS;

    private async Task OnGameTimerAsync(CancellationToken ct)
    {
        switch (_state.Phase)
        {
            case SnowWarGamePhase.Loading
                when PhaseElapsed >= TimeSpan.FromSeconds(_config.StageLoadTimeoutSeconds):
                await BeginStageAsync(ct);
                break;
            case SnowWarGamePhase.Starting
                when PhaseElapsed >= TimeSpan.FromSeconds(_config.PreparingSeconds):
                await StartRunningAsync(ct);
                break;
            case SnowWarGamePhase.Running:
                await RunDueTurnsAsync(ct);
                break;
            case SnowWarGamePhase.Ended
                when PhaseElapsed >= TimeSpan.FromSeconds(_config.RematchSeconds):
                await CloseResultsAsync(ct);
                break;
        }
    }

    private async Task BeginStageAsync(CancellationToken ct)
    {
        EnterPhase(SnowWarGamePhase.Starting);

        await SendAsync(
            PresentIds,
            new Game2StageStartingMessageComposer
            {
                GameType = GAME_TYPE,
                RoomType = ROOM_TYPE,
                CountDown = _config.PreparingSeconds,
                Objects = Arena.GetFullStatus(),
            },
            ct
        );
    }

    private async Task StartRunningAsync(CancellationToken ct)
    {
        EnterPhase(SnowWarGamePhase.Running);

        await SendAsync(
            PresentIds,
            new Game2StageRunningMessageComposer { TimeToStageEnd = _config.GameLengthSeconds },
            ct
        );
        await RunDueTurnsAsync(ct);
    }

    private async Task RunDueTurnsAsync(CancellationToken ct)
    {
        var elapsedTurns = (int)(
            PhaseElapsed.Ticks / TimeSpan.FromMilliseconds(TURN_MILLISECONDS).Ticks
        );
        var due = Math.Min(TotalTurns, elapsedTurns + 1);
        var batch = Math.Min(MAX_TURNS_PER_TICK, due - _state.TurnsRun);

        for (var i = 0; i < batch && _state.Phase == SnowWarGamePhase.Running; i++)
            await RunTurnAsync(ct);

        if (_state.Phase == SnowWarGamePhase.Running && _state.TurnsRun >= TotalTurns)
            await EndGameAsync(ct);
    }

    private async Task RunTurnAsync(CancellationToken ct)
    {
        var result = Arena.RunTurn();
        _state.TurnsRun++;

        var subturns = Arena.GetScheduledEvents(result.Turn + 1);
        var requesters = Present.Where(p => p.AwaitsFullStatus).ToList();

        await SendAsync(
            Present.Where(p => !p.AwaitsFullStatus).Select(p => p.PlayerId),
            new Game2GameStatusMessageComposer
            {
                Turn = result.Turn,
                Checksum = result.Checksum,
                Subturns = subturns,
            },
            ct
        );

        if (requesters.Count == 0)
            return;

        foreach (var requester in requesters)
            Store(requester with { AwaitsFullStatus = false });

        await SendAsync(requesters.Select(p => p.PlayerId), FullStatus(result, subturns), ct);
    }

    private Game2FullGameStatusMessageComposer FullStatus(
        SnowWarTurnResult result,
        ImmutableArray<ImmutableArray<SnowWarGameEventSnapshot>> subturns
    ) =>
        new()
        {
            RemainingSeconds = Math.Max(
                0,
                _config.GameLengthSeconds - (_state.TurnsRun * TURN_MILLISECONDS / 1000)
            ),
            DurationSeconds = _config.GameLengthSeconds,
            Objects = Arena.GetFullStatus(),
            NumberOfTeams = SnowWarConstants.TEAM_COUNT,
            Turn = result.Turn,
            Checksum = result.Checksum,
            Subturns = subturns,
        };

    private async Task EndGameAsync(CancellationToken ct)
    {
        EnterPhase(SnowWarGamePhase.Ended);

        await SendAsync(
            PresentIds,
            new Game2StageEndingMessageComposer { TimeToNextState = STAGE_ENDING_DELAY },
            ct
        );
        await SendAsync(PresentIds, Results(), ct);
        await RecordScoresAsync(ct);
        await ReportFinishedAsync(ct);
    }

    private async Task RecordScoresAsync(CancellationToken ct)
    {
        ImmutableArray<SnowWarScoreEntrySnapshot> scores =
        [
            .. Present.Select(p => new SnowWarScoreEntrySnapshot
            {
                PlayerId = p.PlayerId,
                Score = (Arena.GetHumanStats(p.HumanId) ?? EmptyStats).Score,
            }),
        ];

        try
        {
            await _grainFactory.GetSnowWarScoreGrain().AddAsync(scores, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to record the scores of SnowWar game {GameId}",
                _state.GameId
            );
        }
    }

    private Game2GameEndingMessageComposer Results() =>
        SnowWarGameResults.Build(
            _config.RematchSeconds,
            [
                .. Enumerable
                    .Range(1, SnowWarConstants.TEAM_COUNT)
                    .Select(team => Arena.GetTeamScore(team)),
            ],
            [
                .. Present.Select(p => new SnowWarResultEntry(
                    p.Player,
                    Arena.GetHumanStats(p.HumanId) ?? EmptyStats
                )),
            ]
        );

    private static readonly SnowWarPlayerStatsSnapshot EmptyStats = new()
    {
        Score = 0,
        Kills = 0,
        Deaths = 0,
        SnowballHits = 0,
        SnowballHitsTaken = 0,
        SnowballsThrown = 0,
        SnowballsCreated = 0,
        SnowballsFromMachine = 0,
        FriendlyHits = 0,
        FriendlyKills = 0,
    };

    private async Task CloseResultsAsync(CancellationToken ct)
    {
        var remaining = Present.ToList();
        var rematchers = remaining.Where(p => p.WantsRematch).ToList();

        foreach (var participant in remaining)
        {
            Store(participant with { HasLeft = true });

            await SendAsync(
                participant.PlayerId,
                new Game2GameRejoinMessageComposer { RoomId = participant.PreviousRoomId },
                ct
            );
        }

        foreach (var participant in remaining.Where(p => !p.WantsRematch))
            await Directory.OnPlayerLeftGameAsync(_state.GameId, participant.PlayerId, false, ct);

        if (rematchers.Count > 0)
            await Directory.RematchAsync(_state.GameId, [.. rematchers.Select(p => p.Player)], ct);

        await CloseAsync(ct);
    }

    private async Task CloseAsync(CancellationToken ct)
    {
        _state.Phase = SnowWarGamePhase.Closed;
        StopTimer();

        await ReportFinishedAsync(ct);

        DeactivateOnIdle();
    }

    private async Task ReportFinishedAsync(CancellationToken ct)
    {
        if (_state.FinishReported)
            return;

        _state.FinishReported = true;

        await Directory.OnGameFinishedAsync(_state.GameId, ct);
    }
}
