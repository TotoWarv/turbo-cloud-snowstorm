using Turbo.Games.SnowWar.Simulation;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarSpawnTests
{
    [Fact]
    public void Human_AppearsOnASpawnTileOfHisTeamFacingTheArenaCentre()
    {
        var level = WithSpawns(Open(50, 50), [new(25, 10)], [new(25, 40)]);
        var arena = new SnowWarArena(level, 1234);

        Assert.True(arena.TryAddHuman(Entry(1, 10), out var blue));
        Assert.True(arena.TryAddHuman(Entry(2, 11), out var red));

        var blueView = Human(arena, blue);
        var redView = Human(arena, red);
        Assert.Equal((25, 10), (blueView.TileX, blueView.TileY));
        Assert.Equal(4, blueView.BodyDirection);
        Assert.Equal((25, 40), (redView.TileX, redView.TileY));
        Assert.Equal(0, redView.BodyDirection);
    }

    [Fact]
    public void Human_StartsWithFullLifeAndFiveSnowballsStandingOnHisTile()
    {
        var level = WithSpawns(Open(20, 20), [new(3, 3)], [new(10, 10)]);
        var arena = new SnowWarArena(level, 7);
        Assert.True(arena.TryAddHuman(Entry(1, 42), out var id));

        var view = Human(arena, id);
        Assert.Equal((9600, 9600), (view.X, view.Y));
        Assert.Equal((3, 3), (view.NextX, view.NextY));
        Assert.Equal((9600, 9600), (view.TargetX, view.TargetY));
        Assert.Equal(5, view.Hp);
        Assert.Equal(5, view.Balls);
        Assert.Equal(0, view.ActivityState);
        Assert.Equal(0, view.ActivityTimer);
        Assert.Equal(0, view.Score);
        Assert.Equal(1, view.Team);
        Assert.Equal(42, view.UserId);
        Assert.Equal(3, view.BodyDirection);

        var snapshot = Object(arena, id);
        Assert.Equal(17, snapshot.Variables.Length);
        Assert.Equal(0, snapshot.Variables[7]);
        Assert.Equal(["player42", "motto", "hd-180-1", "M"], snapshot.Strings);
    }

    [Fact]
    public void TakenSpawnTile_SendsTheNextHumanElsewhere()
    {
        var level = WithSpawns(Open(20, 20), [new(3, 3)], [new(10, 10)]);
        var arena = new SnowWarArena(level, 7);
        Assert.True(arena.TryAddHuman(Entry(1, 1), out var first));
        Assert.True(arena.TryAddHuman(Entry(1, 2), out var second));

        Assert.NotEqual(
            (Human(arena, first).TileX, Human(arena, first).TileY),
            (Human(arena, second).TileX, Human(arena, second).TileY)
        );
    }

    [Fact]
    public void WithoutSpawnTiles_HumansAreSpreadOut()
    {
        var arena = new SnowWarArena(Open(30, 30), 1234);
        var tiles = new List<(int X, int Y)>();

        for (var i = 0; i < 3; i++)
        {
            Assert.True(arena.TryAddHuman(Entry(1 + (i % 2), i), out var id));
            tiles.Add((Human(arena, id).TileX, Human(arena, id).TileY));
        }

        for (var a = 0; a < tiles.Count; a++)
        {
            for (var b = a + 1; b < tiles.Count; b++)
            {
                var dx = tiles[a].X - tiles[b].X;
                var dy = tiles[a].Y - tiles[b].Y;
                Assert.True((dx * dx) + (dy * dy) >= 100);
            }
        }
    }

    [Fact]
    public void SameSeed_GivesTheSameSpawns()
    {
        static List<(int, int)> Spawn(int seed)
        {
            var arena = new SnowWarArena(Open(30, 30), seed);
            var tiles = new List<(int, int)>();

            for (var i = 0; i < 4; i++)
            {
                arena.TryAddHuman(Entry(1 + (i % 2), i), out var id);
                tiles.Add((Human(arena, id).TileX, Human(arena, id).TileY));
            }

            return tiles;
        }

        Assert.Equal(Spawn(99), Spawn(99));
    }

    [Fact]
    public void ArenaWithoutAWalkableTile_RefusesTheHuman()
    {
        var arena = new SnowWarArena(Open(1, 1, Furni("snst_block1", 0, 0)), 1);

        Assert.False(arena.TryAddHuman(Entry(1, 1), out _));
    }
}
