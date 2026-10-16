using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Turbo.Primitives.Game.Enums;

namespace Turbo.Games.SnowWar.Simulation;

public sealed partial class SnowWarArena
{
    public void ScheduleInput(
        int humanId,
        SnowWarInput input,
        IReadOnlyList<int> values,
        int turn,
        int subturn
    )
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!SnowWarServerRules.IsWellFormed(input, values, subturn))
        {
            return;
        }

        if (World.FindActive<SnowWarHuman>(humanId) is null || IsLeaving(humanId))
        {
            return;
        }

        if (
            _pendingInputs.Count(pending => pending.HumanId == humanId)
            >= SnowWarConstants.MAX_PENDING_INPUTS
        )
        {
            return;
        }

        _pendingInputs.Add(new SnowWarPendingInput(humanId, input, [.. values], turn, subturn));
    }

    internal static (int Turn, int Subturn) Place(int firstOpenTurn, int turn, int subturn)
    {
        if (turn < firstOpenTurn)
        {
            return (firstOpenTurn, 0);
        }

        if (turn > firstOpenTurn + 1)
        {
            return (firstOpenTurn + 1, 0);
        }

        return (turn, subturn);
    }

    private void PlacePendingInputs(int firstOpenTurn)
    {
        var inputs = _pendingInputs.ToList();
        _pendingInputs.Clear();

        for (var i = 0; i < inputs.Count; i++)
        {
            var pending = inputs[i];

            if (pending.Input == SnowWarInput.MoveTarget && HasLaterMove(inputs, i))
            {
                continue;
            }

            if (!SnowWarServerRules.Accepts(this, pending.HumanId, pending.Input, pending.Values))
            {
                continue;
            }

            var (turn, subturn) = Place(firstOpenTurn, pending.Turn, pending.Subturn);
            Emit(pending, turn, subturn);
        }
    }

    private static bool HasLaterMove(List<SnowWarPendingInput> inputs, int index)
    {
        for (var j = index + 1; j < inputs.Count; j++)
        {
            if (
                inputs[j].HumanId == inputs[index].HumanId
                && inputs[j].Input == SnowWarInput.MoveTarget
            )
            {
                return true;
            }
        }

        return false;
    }

    private void Emit(SnowWarPendingInput pending, int turn, int subturn)
    {
        var values = pending.Values;
        var humanId = pending.HumanId;

        switch (pending.Input)
        {
            case SnowWarInput.MoveTarget:
                Schedule(
                    turn,
                    subturn,
                    SnowWarGameEventType.NewMoveTarget,
                    humanId,
                    World.Grid.ClampX(values[0]),
                    World.Grid.ClampY(values[1])
                );
                break;
            case SnowWarInput.MakeSnowball:
                Schedule(turn, subturn, SnowWarGameEventType.HumanStartsToMakeASnowball, humanId);
                break;
            case SnowWarInput.ThrowAtPosition:
                EmitThrowAtPosition(humanId, values, turn, subturn);
                break;
            case SnowWarInput.ThrowAtHuman:
                EmitThrowAtHuman(humanId, values, turn, subturn);
                break;
        }
    }

    private void EmitThrowAtPosition(int humanId, ImmutableArray<int> values, int turn, int subturn)
    {
        var x = World.Grid.ClampX(values[0]);
        var y = World.Grid.ClampY(values[1]);
        var trajectory = values[2];
        var snowballId = World.AllocateObjectId();

        Schedule(
            turn,
            subturn,
            SnowWarGameEventType.HumanThrowsSnowballAtPosition,
            humanId,
            x,
            y,
            trajectory
        );
        Schedule(
            turn,
            subturn,
            SnowWarGameEventType.CreateSnowball,
            snowballId,
            humanId,
            x,
            y,
            trajectory
        );
    }

    private void EmitThrowAtHuman(int humanId, ImmutableArray<int> values, int turn, int subturn)
    {
        var target = World.FindActive<SnowWarHuman>(values[0])!;
        var trajectory = values[1];
        var snowballId = World.AllocateObjectId();

        Schedule(
            turn,
            subturn,
            SnowWarGameEventType.HumanThrowsSnowballAtHuman,
            humanId,
            target.Id,
            trajectory
        );
        Schedule(
            turn,
            subturn,
            SnowWarGameEventType.CreateSnowball,
            snowballId,
            humanId,
            target.X,
            target.Y,
            trajectory
        );
    }
}
