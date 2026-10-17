using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;
using Turbo.Games.SnowWar.Game;
using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Grains;
using Turbo.Primitives.Game.Snapshots;
using Turbo.Primitives.Messages.Outgoing.Game.Snowwar.Arena;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players;

namespace Turbo.Games.Grains;

internal sealed partial class SnowWarGameGrain : Grain, ISnowWarGameGrain
{
    private const int GAME_TYPE = 0;
    private const int TICK_MILLISECONDS = 50;
    private const int NO_ROOM = -1;
    private const string ROOM_TYPE = "snowwar";

    private readonly SnowWarConfig _config;
    private readonly SnowWarArenaCatalog _arenas;
    private readonly IGrainFactory _grainFactory;
    private readonly IWordFilter _wordFilter;
    private readonly TimeProvider _time;
    private readonly ILogger<ISnowWarGameGrain> _logger;

    private readonly SnowWarGameLiveState _state = new();

    private IDisposable? _gameTimer;

    public SnowWarGameGrain(
        IOptions<SnowWarConfig> config,
        SnowWarArenaCatalog arenas,
        IGrainFactory grainFactory,
        IWordFilter wordFilter,
        TimeProvider time,
        ILogger<ISnowWarGameGrain> logger
    )
    {
        _config = config.Value;
        _arenas = arenas;
        _grainFactory = grainFactory;
        _wordFilter = wordFilter;
        _time = time;
        _logger = logger;
    }

    private DateTimeOffset Now => _time.GetUtcNow();

    private SnowWarArena Arena =>
        _state.Arena ?? throw new InvalidOperationException("The SnowWar game has not started.");

    private IEnumerable<SnowWarParticipant> Present => _state.Participants.Where(p => !p.HasLeft);

    private IEnumerable<PlayerId> PresentIds => Present.Select(p => p.PlayerId);

    private ISnowWarDirectoryGrain Directory => _grainFactory.GetSnowWarDirectoryGrain();

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        StopTimer();

        if (_state.Phase is SnowWarGamePhase.Idle or SnowWarGamePhase.Closed)
            return;

        _state.Phase = SnowWarGamePhase.Closed;

        try
        {
            await Directory.OnGameDeactivatedAsync(_state.GameId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "SnowWar game {GameId} could not tell the directory it went away",
                _state.GameId
            );
        }
    }

    public async Task StartAsync(SnowWarGameLobbySnapshot lobby, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lobby);

        if (_state.Phase != SnowWarGamePhase.Idle)
            return;

        var level = _arenas.Get(lobby.FieldType);
        var arena = new SnowWarArena(level, Random.Shared.Next(), _config.RayGunsEnabled);
        var participants = await PlaceAsync(arena, lobby.Players, ct);

        _state.GameId = lobby.GameId;
        _state.FieldType = lobby.FieldType;
        _state.Arena = arena;
        _state.Participants.AddRange(participants);

        EnterPhase(SnowWarGamePhase.Loading);
        StartTimer();

        await AnnounceArenaAsync(level, arena, ct);
    }

    private async Task<ImmutableArray<SnowWarParticipant>> PlaceAsync(
        SnowWarArena arena,
        ImmutableArray<SnowWarPlayerSnapshot> players,
        CancellationToken ct
    )
    {
        var placed = ImmutableArray.CreateBuilder<SnowWarParticipant>(players.Length);

        foreach (var player in players)
        {
            if (!arena.TryAddHuman(HumanEntry(player), out var humanId))
                throw new InvalidOperationException(
                    $"No free SnowWar spawn tile for player {player.UserId}."
                );

            placed.Add(
                new SnowWarParticipant
                {
                    Player = player,
                    HumanId = humanId,
                    PreviousRoomId = await PreviousRoomOfAsync(player.UserId, ct),
                }
            );
        }

        return placed.MoveToImmutable();
    }

    private static SnowWarHumanEntry HumanEntry(SnowWarPlayerSnapshot player) =>
        new()
        {
            UserId = player.UserId,
            Name = player.Name,
            Mission = string.Empty,
            Figure = player.Figure,
            Gender = player.Gender,
            Team = player.TeamId,
        };

    private async Task<int> PreviousRoomOfAsync(PlayerId playerId, CancellationToken ct)
    {
        var pointer = await _grainFactory.GetPlayerPresenceGrain(playerId).GetActiveRoomAsync(ct);

        return pointer is { RoomId.Value: > 0 } ? pointer.RoomId.Value : NO_ROOM;
    }

    private async Task AnnounceArenaAsync(
        SnowWarLevelData level,
        SnowWarArena arena,
        CancellationToken ct
    )
    {
        var arenaPlayers = _state.Participants.Select(ArenaPlayer).ToImmutableArray();

        await SendAsync(
            PresentIds,
            new Game2EnterArenaMessageComposer
            {
                GameType = GAME_TYPE,
                FieldType = _state.FieldType,
                NumberOfTeams = SnowWarConstants.TEAM_COUNT,
                Players = arenaPlayers,
                Level = SnowWarLevelSnapshots.Build(level, arena),
            },
            ct
        );

        foreach (var arenaPlayer in arenaPlayers)
            await SendAsync(
                PresentIds,
                new Game2ArenaEnteredMessageComposer { Player = arenaPlayer },
                ct
            );

        await SendAsync(PresentIds, new Game2StageLoadMessageComposer { GameType = GAME_TYPE }, ct);
    }

    private static SnowWarArenaPlayerSnapshot ArenaPlayer(SnowWarParticipant participant) =>
        new()
        {
            ReferenceId = participant.Player.UserId,
            UserName = participant.Player.Name,
            Figure = participant.Player.Figure,
            Gender = participant.Player.Gender,
            TeamId = participant.Player.TeamId,
        };

    private void StartTimer()
    {
        var period = TimeSpan.FromMilliseconds(TICK_MILLISECONDS);

        _gameTimer = this.RegisterGrainTimer<object?>(
            static async (self, ct) => await ((SnowWarGameGrain)self!).OnGameTimerAsync(ct),
            this,
            new GrainTimerCreationOptions
            {
                DueTime = period,
                Period = period,
                KeepAlive = true,
            }
        );
    }

    private void StopTimer()
    {
        _gameTimer?.Dispose();
        _gameTimer = null;
    }

    private void EnterPhase(SnowWarGamePhase phase)
    {
        _state.Phase = phase;
        _state.PhaseStartedAt = Now;
    }

    private TimeSpan PhaseElapsed => Now - _state.PhaseStartedAt;

    private SnowWarParticipant? PresentParticipant(PlayerId playerId) =>
        Present.FirstOrDefault(p => p.PlayerId == playerId);

    private void Store(SnowWarParticipant participant)
    {
        var index = _state.Participants.FindIndex(p => p.PlayerId == participant.PlayerId);

        _state.Participants[index] = participant;
    }

    private Task SendAsync(PlayerId playerId, IComposer composer, CancellationToken ct) =>
        _grainFactory.SendComposerToPlayerAsync(playerId, composer, ct);

    private Task SendAsync(
        IEnumerable<PlayerId> playerIds,
        IComposer composer,
        CancellationToken ct
    ) => _grainFactory.SendComposerToPlayersAsync(playerIds.ToList(), composer, ct);
}
