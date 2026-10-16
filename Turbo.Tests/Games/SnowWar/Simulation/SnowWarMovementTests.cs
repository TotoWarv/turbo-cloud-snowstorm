using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarMovementTests
{
    [Fact]
    public void Walking_CrossesATileInSixSubturns()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);
        MoveTo(arena, human, 3, 5);
        Turns(arena, 2);

        var halfway = Human(arena, human);
        Assert.Equal(6400 + (3 * 534), halfway.X);
        Assert.Equal((2, 5), (halfway.TileX, halfway.TileY));
        Assert.Equal((3, 5), (halfway.NextX, halfway.NextY));
        Assert.Equal(2, halfway.BodyDirection);

        Turns(arena, 1);

        var arrived = Human(arena, human);
        Assert.Equal(9600, arrived.X);
        Assert.Equal((3, 5), (arrived.TileX, arrived.TileY));
        Assert.Equal((3, 5), (arrived.NextX, arrived.NextY));
    }

    [Fact]
    public void Walking_DiagonallyAdvancesOnBothAxes()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 2);
        MoveTo(arena, human, 3, 3);
        Turns(arena, 2);

        var view = Human(arena, human);
        Assert.Equal((8002, 8002), (view.X, view.Y));
        Assert.Equal(3, view.BodyDirection);
    }

    [Fact]
    public void Walking_GoesAroundAnObstacleWithoutPathfinding()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("snst_block1", 4, 5)), 1);
        var human = AddHuman(arena, 1, 2, 5);
        MoveTo(arena, human, 6, 5);

        var tiles = new List<(int, int)> { (2, 5) };

        for (var i = 0; i < 14; i++)
        {
            Turns(arena, 1);
            var view = Human(arena, human);

            if (tiles[^1] != (view.TileX, view.TileY))
            {
                tiles.Add((view.TileX, view.TileY));
            }
        }

        Assert.Equal([(2, 5), (3, 5), (4, 4), (5, 5), (6, 5)], tiles);
    }

    [Fact]
    public void Walking_TowardABlockedTileStopsInFrontOfIt()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("snst_block1", 4, 5)), 1);
        var human = AddHuman(arena, 1, 2, 5);
        MoveTo(arena, human, 4, 5);
        Turns(arena, 8);

        var view = Human(arena, human);
        Assert.Equal((3, 5), (view.TileX, view.TileY));
        Assert.Equal((Center(3), Center(5)), (view.TargetX, view.TargetY));
        Assert.Equal((Center(3), Center(5)), (view.X, view.Y));
    }

    [Fact]
    public void Walking_AvoidsATileHeldByAnotherHuman()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var walker = AddHuman(arena, 1, 2, 5);
        AddHuman(arena, 2, 3, 5);
        MoveTo(arena, walker, 4, 5);
        Turns(arena, 10);

        var view = Human(arena, walker);
        Assert.Equal((4, 5), (view.TileX, view.TileY));
    }

    [Fact]
    public void Walking_WaitsInPlaceAndKeepsItsTargetWhenAllThreeCandidatesAreBlocked()
    {
        var arena = new SnowWarArena(
            Open(
                10,
                10,
                Furni("snst_block1", 3, 4),
                Furni("snst_block1", 3, 5),
                Furni("snst_block1", 3, 6)
            ),
            1
        );
        var human = AddHuman(arena, 1, 2, 5);
        MoveTo(arena, human, 5, 5);
        Turns(arena, 6);

        var view = Human(arena, human);
        Assert.Equal((Center(2), Center(5)), (view.X, view.Y));
        Assert.Equal((Center(5), Center(5)), (view.TargetX, view.TargetY));
    }

    [Fact]
    public void Walking_NeverStepsOnAMissingTile()
    {
        var arena = new SnowWarArena(Rows(["0000000", "000x000", "0000000", "0000000"]), 1);
        var human = AddHuman(arena, 1, 1, 1);
        MoveTo(arena, human, 5, 1);

        for (var i = 0; i < 14; i++)
        {
            Turns(arena, 1);
            var view = Human(arena, human);
            Assert.NotEqual((3, 1), (view.TileX, view.TileY));
            Assert.NotEqual((3, 1), (view.NextX, view.NextY));
        }

        Assert.Equal(5, Human(arena, human).TileX);
    }

    [Fact]
    public void MoveTarget_IsBoundedToTheArenaRectangle()
    {
        var arena = new SnowWarArena(Open(10, 8), 1);
        var human = AddHuman(arena, 1, 2, 5);
        Send(arena, human, SnowWarInput.MoveTarget, [-5000, 999999]);
        Turns(arena, 1);

        var move = Assert.Single(AllEvents(arena, arena.NextTurn));
        Assert.Equal(SnowWarGameEventType.NewMoveTarget, move.Type);
        Assert.Equal([human, 0, 7 * 3200], move.Fields);
    }

    [Fact]
    public void MoveTarget_OnAMissingTileIsIgnored()
    {
        var arena = new SnowWarArena(Rows(["00000", "00x00", "00000"]), 1);
        var human = AddHuman(arena, 1, 0, 0);
        MoveTo(arena, human, 2, 1);
        Turns(arena, 1);

        Assert.Empty(AllEvents(arena, arena.NextTurn));
    }

    [Fact]
    public void MoveTarget_KeepsOnlyTheLastRequestOfABatch()
    {
        var arena = new SnowWarArena(Open(10, 10), 1);
        var human = AddHuman(arena, 1, 2, 5);
        MoveTo(arena, human, 3, 5);
        MoveTo(arena, human, 4, 5);
        MoveTo(arena, human, 5, 6);
        Turns(arena, 1);

        var move = Assert.Single(AllEvents(arena, arena.NextTurn));
        Assert.Equal([human, Center(5), Center(6)], move.Fields);
    }
}
