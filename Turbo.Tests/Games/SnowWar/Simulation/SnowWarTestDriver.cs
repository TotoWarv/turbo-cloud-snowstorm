using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Turbo.Primitives.Game.Snapshots;
using Xunit;

namespace Turbo.Tests.Games.SnowWar.Simulation;

internal static class SnowWarTestDriver
{
    public static int Center(int tile) => tile * 3200;

    public static SnowWarHumanEntry Entry(int team, int userId) =>
        new()
        {
            UserId = userId,
            Name = $"player{userId}",
            Mission = "motto",
            Figure = "hd-180-1",
            Gender = "M",
            Team = team,
        };

    public static int AddHuman(SnowWarArena arena, int team, int x, int y)
    {
        var userId = 100 + arena.GetFullStatus().Length;
        Assert.True(arena.TryAddHumanAt(Entry(team, userId), x, y, out var humanId));
        return humanId;
    }

    public static SnowWarTurnResult Turns(SnowWarArena arena, int count)
    {
        SnowWarTurnResult last = default;

        for (var i = 0; i < count; i++)
        {
            last = arena.RunTurn();
        }

        return last;
    }

    public static void Send(
        SnowWarArena arena,
        int humanId,
        SnowWarInput input,
        int[] values,
        int subturn = 0
    ) => arena.ScheduleInput(humanId, input, values, arena.NextTurn + 1, subturn);

    public static void MoveTo(SnowWarArena arena, int humanId, int tileX, int tileY) =>
        Send(arena, humanId, SnowWarInput.MoveTarget, [Center(tileX), Center(tileY)]);

    public static void ThrowAt(
        SnowWarArena arena,
        int humanId,
        int tileX,
        int tileY,
        SnowWarTrajectory trajectory = SnowWarTrajectory.Quick
    ) =>
        Send(
            arena,
            humanId,
            SnowWarInput.ThrowAtPosition,
            [Center(tileX), Center(tileY), (int)trajectory]
        );

    public static void ThrowAtHuman(
        SnowWarArena arena,
        int humanId,
        int targetId,
        SnowWarTrajectory trajectory = SnowWarTrajectory.Quick
    ) => Send(arena, humanId, SnowWarInput.ThrowAtHuman, [targetId, (int)trajectory]);

    public static SnowWarGameObjectSnapshot Object(SnowWarArena arena, int id) =>
        arena.GetFullStatus().Single(snapshot => snapshot.Id == id);

    public static SnowWarHumanView Human(SnowWarArena arena, int id) =>
        SnowWarHumanView.From(Object(arena, id));

    public static bool Exists(SnowWarArena arena, int id) =>
        arena.GetFullStatus().Any(snapshot => snapshot.Id == id);

    public static IReadOnlyList<SnowWarBallView> Balls(SnowWarArena arena) =>
        [
            .. arena
                .GetFullStatus()
                .Where(snapshot => snapshot.Type == SnowWarGameObjectType.Snowball)
                .Select(SnowWarBallView.From),
        ];

    public static IReadOnlyList<SnowWarGameEventSnapshot> SubturnEvents(
        SnowWarArena arena,
        int turn,
        int subturn
    ) => arena.GetScheduledEvents(turn)[subturn];

    public static IReadOnlyList<SnowWarGameEventSnapshot> AllEvents(SnowWarArena arena, int turn) =>
        [.. arena.GetScheduledEvents(turn).SelectMany(events => events)];

    public static void WaitUntilThrowReady(SnowWarArena arena) => Turns(arena, 3);
}
