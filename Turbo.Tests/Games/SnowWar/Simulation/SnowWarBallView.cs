using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Tests.Games.SnowWar.Simulation;

internal sealed record SnowWarBallView(
    int Id,
    int X,
    int Y,
    int Z,
    int Direction,
    int Trajectory,
    int Life,
    int ThrowerId,
    int ParabolaOffset,
    int Speed
)
{
    public static SnowWarBallView From(SnowWarGameObjectSnapshot snapshot)
    {
        var v = snapshot.Variables;
        return new(snapshot.Id, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8]);
    }
}
