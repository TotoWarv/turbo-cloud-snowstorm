using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarTrajectoryTests
{
    [Theory]
    [InlineData(10, 0, 10, 5, 2000, 0)]
    [InlineData(10, 1, 17, 8, 1882, 1)]
    [InlineData(10, 2, 22, 11, 1454, 2)]
    [InlineData(30, 1, 33, 16, 1818, 1)]
    [InlineData(40, 2, 70, 35, 1428, 2)]
    [InlineData(10, 3, 10, 5, 2000, 0)]
    [InlineData(15, 3, 26, 13, 1846, 1)]
    [InlineData(25, 3, 56, 28, 1421, 2)]
    public void Create_MatchesTheControlValues(
        int tiles,
        int requested,
        int life,
        int parabolaOffset,
        int speed,
        int resolved
    )
    {
        var ball = SnowWarSnowball.Create(
            1,
            2,
            0,
            0,
            tiles * 3200,
            0,
            (SnowWarTrajectory)requested
        );

        Assert.Equal(life, ball.Life);
        Assert.Equal(parabolaOffset, ball.ParabolaOffset);
        Assert.Equal(speed, ball.Speed);
        Assert.Equal((SnowWarTrajectory)resolved, ball.Trajectory);
        Assert.Equal(90, ball.Direction);
        Assert.Equal(3000, ball.Z);
        Assert.Equal(2, ball.ThrowerId);
    }

    [Theory]
    [InlineData(42500, SnowWarTrajectory.Quick)]
    [InlineData(48500, SnowWarTrajectory.ShortLob)]
    public void Create_ResolvesTheDefaultTrajectoryByDistance(
        int targetX,
        SnowWarTrajectory expected
    ) =>
        Assert.Equal(
            expected,
            SnowWarSnowball
                .Create(1, 2, 500, 700, targetX, 700, SnowWarTrajectory.Default)
                .Trajectory
        );

    [Fact]
    public void Create_CapsLongThrowsInsteadOfRefusingThem()
    {
        var ball = SnowWarSnowball.Create(1, 2, 0, 0, 3200 * 200, 0, SnowWarTrajectory.ShortLob);

        Assert.Equal(33, ball.Life);
        Assert.Equal(1818, ball.Speed);
    }

    [Fact]
    public void Create_ALobOnTheSpotHasNoLifeAndNoSpeed()
    {
        var ball = SnowWarSnowball.Create(1, 2, 6400, 6400, 6400, 6400, SnowWarTrajectory.LongLob);

        Assert.Equal(0, ball.Life);
        Assert.Equal(0, ball.Speed);
        Assert.Equal(0, ball.ParabolaOffset);
        Assert.Equal(180, ball.Direction);
    }

    [Fact]
    public void Create_TurnsAFullNorthAngleIntoZero() =>
        Assert.Equal(
            0,
            SnowWarSnowball.Create(1, 2, 6400, 32000, 6400, 0, SnowWarTrajectory.Quick).Direction
        );
}
