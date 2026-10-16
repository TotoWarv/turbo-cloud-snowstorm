using Turbo.Games.SnowWar.Simulation;
using Xunit;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarMathTests
{
    [Theory]
    [InlineData(90, 256)]
    [InlineData(270, -256)]
    [InlineData(30, 127)]
    public void BaseVectorX_MatchesTheControlValues(int direction, int expected) =>
        Assert.Equal(expected, SnowWarMath.BaseVectorX(direction));

    [Theory]
    [InlineData(0, -256)]
    [InlineData(180, 256)]
    [InlineData(1, -255)]
    public void BaseVectorY_MatchesTheControlValues(int direction, int expected) =>
        Assert.Equal(expected, SnowWarMath.BaseVectorY(direction));

    [Fact]
    public void Tables_HaveTheirDocumentedBounds()
    {
        Assert.Equal(360, SnowWarTables.BASE_VECTOR_X.Length);
        Assert.Equal(360, SnowWarTables.BASE_VECTOR_Y.Length);
        Assert.Equal(256, SnowWarTables.ARC_TANGENT.Length);
        Assert.Equal(256, SnowWarTables.SQUARE_ROOT.Length);
        Assert.Equal(0, SnowWarTables.ARC_TANGENT[0]);
        Assert.Equal(45, SnowWarTables.ARC_TANGENT[255]);
        Assert.Equal(255, SnowWarTables.SQUARE_ROOT[255]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(359, 359)]
    [InlineData(360, 0)]
    [InlineData(725, 5)]
    [InlineData(-1, 359)]
    [InlineData(-360, 360)]
    public void NormalizeDirection_FollowsTheRemainderRule(int value, int expected) =>
        Assert.Equal(expected, SnowWarMath.NormalizeDirection(value));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(21, 0)]
    [InlineData(22, 1)]
    [InlineData(66, 1)]
    [InlineData(67, 2)]
    [InlineData(90, 2)]
    [InlineData(180, 4)]
    [InlineData(270, 6)]
    [InlineData(336, 7)]
    [InlineData(337, 0)]
    [InlineData(360, 0)]
    [InlineData(-90, 6)]
    public void Dir360ToDir8_MatchesTheControlValues(int direction, int expected) =>
        Assert.Equal(expected, SnowWarMath.Dir360ToDir8(direction));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 135)]
    [InlineData(7, 315)]
    [InlineData(8, -1)]
    [InlineData(-1, -1)]
    public void Dir8ToDir360_MultipliesBy45(int direction, int expected) =>
        Assert.Equal(expected, SnowWarMath.Dir8ToDir360(direction));

    [Theory]
    [InlineData(7, 1, 0)]
    [InlineData(0, -1, 7)]
    [InlineData(3, 4, 7)]
    public void RotateDir8_WrapsOnThreeBits(int direction, int steps, int expected) =>
        Assert.Equal(expected, SnowWarMath.RotateDir8(direction, steps));

    [Theory]
    [InlineData(0, 0, -1)]
    [InlineData(1, 1, -1)]
    [InlineData(2, 1, 0)]
    [InlineData(3, 1, 1)]
    [InlineData(4, 0, 1)]
    [InlineData(5, -1, 1)]
    [InlineData(6, -1, 0)]
    [InlineData(7, -1, -1)]
    public void Dir8Offsets_PointToTheEightNeighbours(int direction, int dx, int dy)
    {
        Assert.Equal(dx, SnowWarMath.Dir8OffsetX(direction));
        Assert.Equal(dy, SnowWarMath.Dir8OffsetY(direction));
    }

    [Theory]
    [InlineData(0, -1, 360)]
    [InlineData(1, -1, 45)]
    [InlineData(1, 0, 90)]
    [InlineData(1, 1, 135)]
    [InlineData(0, 1, 180)]
    [InlineData(-1, 1, 225)]
    [InlineData(-1, 0, 270)]
    [InlineData(-1, -1, 315)]
    [InlineData(0, 0, 180)]
    [InlineData(160, 0, 90)]
    [InlineData(100, -50, 63)]
    public void Angle_MatchesTheControlValues(int x, int y, int expected) =>
        Assert.Equal(expected, SnowWarMath.Angle(x, y));

    [Theory]
    [InlineData(-5, -1)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(100, 10)]
    [InlineData(255, 15)]
    [InlineData(256, 16)]
    [InlineData(1000, 31)]
    [InlineData(4096, 64)]
    [InlineData(10000, 99)]
    [InlineData(25600, 160)]
    [InlineData(65536, 256)]
    [InlineData(160000, 398)]
    [InlineData(1000000, 996)]
    [InlineData(2147483647, 46080)]
    public void FastSqrt_MatchesTheControlValues(int value, int expected) =>
        Assert.Equal(expected, SnowWarMath.FastSqrt(value));

    [Theory]
    [InlineData(3, 4, 5, false)]
    [InlineData(3, 4, 6, true)]
    [InlineData(7, 0, 6, false)]
    [InlineData(-3, -4, 6, true)]
    public void IsWithinDistance_IsStrict(int dx, int dy, int radius, bool expected) =>
        Assert.Equal(expected, SnowWarMath.IsWithinDistance(100, 100, 100 + dx, 100 + dy, radius));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1599, 0)]
    [InlineData(1600, 1)]
    [InlineData(4799, 1)]
    [InlineData(4800, 2)]
    [InlineData(-4799, 0)]
    [InlineData(-4800, -1)]
    public void WorldToTile_TruncatesTowardZero(int world, int expected) =>
        Assert.Equal(expected, SnowWarMath.WorldToTile(world));

    [Theory]
    [InlineData(1599, -1599, true)]
    [InlineData(1600, 0, false)]
    [InlineData(0, -1600, false)]
    public void IsInTile_UsesStrictHalfTileBounds(int dx, int dy, bool expected) =>
        Assert.Equal(expected, SnowWarMath.IsInTile(6400 + dx, 6400 + dy, 6400, 6400));
}
