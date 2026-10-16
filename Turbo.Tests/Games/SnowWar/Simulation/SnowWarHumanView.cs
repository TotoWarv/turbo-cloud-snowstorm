using Turbo.Primitives.Game.Snapshots;

namespace Turbo.Tests.Games.SnowWar.Simulation;

internal sealed record SnowWarHumanView(
    int Id,
    int X,
    int Y,
    int TileX,
    int TileY,
    int BodyDirection,
    int Hp,
    int Balls,
    int ActivityTimer,
    int ActivityState,
    int NextX,
    int NextY,
    int TargetX,
    int TargetY,
    int Score,
    int Team,
    int UserId
)
{
    public static SnowWarHumanView From(SnowWarGameObjectSnapshot snapshot)
    {
        var v = snapshot.Variables;
        return new(
            snapshot.Id,
            v[0],
            v[1],
            v[2],
            v[3],
            v[4],
            v[5],
            v[6],
            v[8],
            v[9],
            v[10],
            v[11],
            v[12],
            v[13],
            v[14],
            v[15],
            v[16]
        );
    }
}
