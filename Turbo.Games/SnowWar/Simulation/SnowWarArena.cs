using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Games.SnowWar.Simulation;

public sealed partial class SnowWarArena
{
    private readonly SnowWarLevelData _level;
    private readonly SnowWarRandom _random;
    private readonly bool _rayGunsEnabled;
    private readonly List<SnowWarScheduledEvent> _scheduled = [];
    private readonly List<SnowWarPendingInput> _pendingInputs = [];
    private readonly List<SnowWarTile> _spawnTiles = [];

    public SnowWarArena(SnowWarLevelData level, int seed, bool rayGunsEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(level);

        _level = level;
        _random = new SnowWarRandom(seed);
        _rayGunsEnabled = rayGunsEnabled;
        World = new SnowWarWorld(level);
    }

    public int NextTurn { get; private set; }

    internal SnowWarWorld World { get; }

    public SnowWarTurnResult RunTurn()
    {
        var turn = NextTurn;
        var events = _scheduled.Where(scheduled => scheduled.Turn == turn).ToList();
        _scheduled.RemoveAll(scheduled => scheduled.Turn <= turn);

        var checksum = SnowWarTurnRunner.Run(World, turn, events);
        NextTurn = turn + 1;

        PlacePendingInputs(NextTurn);
        ScheduleSourceDecisions(NextTurn);

        if (_rayGunsEnabled)
        {
            ScheduleRayGunBursts(NextTurn);
        }

        return new SnowWarTurnResult(turn, checksum);
    }

    public bool RemoveHuman(int humanId)
    {
        if (World.FindActive<SnowWarHuman>(humanId) is null || IsLeaving(humanId))
        {
            return false;
        }

        _pendingInputs.RemoveAll(pending => pending.HumanId == humanId);
        Schedule(NextTurn + 1, 0, SnowWarGameEventType.HumanLeftGame, humanId);
        return true;
    }

    public ImmutableArray<ImmutableArray<SnowWarGameEventSnapshot>> GetScheduledEvents(int turn)
    {
        var subturns = ImmutableArray.CreateBuilder<ImmutableArray<SnowWarGameEventSnapshot>>(
            SnowWarConstants.SUBTURNS_PER_TURN
        );

        for (var subturn = 0; subturn < SnowWarConstants.SUBTURNS_PER_TURN; subturn++)
        {
            subturns.Add([
                .. _scheduled
                    .Where(scheduled => scheduled.Turn == turn && scheduled.Subturn == subturn)
                    .Select(scheduled => scheduled.ToSnapshot()),
            ]);
        }

        return subturns.MoveToImmutable();
    }

    public ImmutableArray<SnowWarGameObjectSnapshot> GetFullStatus() => World.Snapshot();

    public SnowWarPlayerStatsSnapshot? GetHumanStats(int humanId) =>
        World.FindActive<SnowWarHuman>(humanId)?.Stats;

    public int GetTeamScore(int team) => World.GetTeamScore(team);

    internal bool IsLeaving(int humanId) =>
        HasPendingEvent(SnowWarGameEventType.HumanLeftGame, 0, humanId);

    internal bool HasPendingEvent(SnowWarGameEventType type, int fieldIndex, int value) =>
        CountPendingEvents(type, fieldIndex, value) > 0;

    internal int CountPendingEvents(SnowWarGameEventType type, int fieldIndex, int value)
    {
        var count = 0;

        foreach (var scheduled in _scheduled)
        {
            if (scheduled.Type == type && scheduled.Names(fieldIndex, value))
            {
                count++;
            }
        }

        return count;
    }

    private void Schedule(int turn, int subturn, SnowWarGameEventType type, params int[] fields) =>
        _scheduled.Add(new SnowWarScheduledEvent(turn, subturn, type, [.. fields]));
}
