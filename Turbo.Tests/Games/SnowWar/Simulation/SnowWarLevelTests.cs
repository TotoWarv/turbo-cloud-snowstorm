using Turbo.Games.SnowWar.Simulation;
using Turbo.Primitives.Game.Enums;
using Xunit;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestDriver;
using static Turbo.Tests.Games.SnowWar.Simulation.SnowWarTestLevels;

namespace Turbo.Tests.Games.SnowWar.Simulation;

public sealed class SnowWarLevelTests
{
    [Fact]
    public void DecorObjects_BecomeGameObjectsInListOrderBeforeTheHumans()
    {
        var arena = new SnowWarArena(
            Open(
                14,
                4,
                Furni("snst_tree1", 1, 1, 2),
                Furni("snst_ballpile", 3, 1),
                Furni("s_snowball_machine", 5, 1, 4),
                Furni("snst_block1", 7, 1),
                Furni("snst_tree1_d", 9, 1),
                Furni("snowball_machine", 11, 1)
            ),
            1
        );
        var human = AddHuman(arena, 1, 1, 3);

        var status = arena.GetFullStatus();

        Assert.Equal([1, 2, 3, 4, 5, 6], status.Select(snapshot => snapshot.Id));
        Assert.Equal(6, human);
        Assert.Equal(SnowWarGameObjectType.Tree, status[0].Type);
        Assert.Equal([3200, 3200, 2, 3200, 1, 3, 0], status[0].Variables);
        Assert.Equal(SnowWarGameObjectType.Pile, status[1].Type);
        Assert.Equal([9600, 3200, 12, 12, 2], status[1].Variables);
        Assert.Equal(SnowWarGameObjectType.Machine, status[2].Type);
        Assert.Equal([16000, 3200, 4, 5, 0, 3], status[2].Variables);
        Assert.Equal([28800, 3200, 0, 3200, 5, 3, 0], status[3].Variables);
        Assert.Equal([35200, 3200, 0, 5, 0, 6], status[4].Variables);
        Assert.Equal(SnowWarGameObjectType.Human, status[5].Type);
    }

    [Fact]
    public void DecorObjectOnAMissingTile_CreatesNothingButKeepsItsRank()
    {
        var arena = new SnowWarArena(
            Rows(
                ["00000", "0x000", "00000"],
                Furni("snst_tree1", 1, 1),
                Furni("snst_ballpile", 3, 1)
            ),
            1
        );

        var status = arena.GetFullStatus();

        var pile = Assert.Single(status);
        Assert.Equal(1, pile.Id);
        Assert.Equal(2, pile.Variables[4]);
    }

    [Fact]
    public void UnknownFurni_IsRejected() =>
        Assert.Throws<ArgumentException>(() =>
            new SnowWarArena(Open(5, 5, Furni("throne", 1, 1)), 1)
        );

    [Fact]
    public void Fence_BlocksTheTileSouthOfItsAnchorWhenFacingNorth()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("snst_fence", 5, 5)), 1);
        var human = AddHuman(arena, 1, 5, 8);
        MoveTo(arena, human, 5, 6);
        Turns(arena, 8);

        var view = Human(arena, human);
        Assert.Equal((5, 7), (view.TileX, view.TileY));
        Assert.Equal((Center(5), Center(7)), (view.TargetX, view.TargetY));
    }

    [Fact]
    public void Fence_SwapsItsFootprintWhenFacingEast()
    {
        var arena = new SnowWarArena(Open(10, 10, Furni("snst_fence", 5, 5, 2)), 1);
        var south = AddHuman(arena, 1, 5, 8);
        var east = AddHuman(arena, 2, 8, 5);
        MoveTo(arena, south, 5, 6);
        MoveTo(arena, east, 6, 5);
        Turns(arena, 8);

        Assert.Equal((5, 6), (Human(arena, south).TileX, Human(arena, south).TileY));
        Assert.Equal((7, 5), (Human(arena, east).TileX, Human(arena, east).TileY));
    }

    [Fact]
    public void Backdrop_DoesNotBlockAnEmptyTile()
    {
        var level = Open(10, 10) with
        {
            Backdrop = new SnowWarBackdropData
            {
                ImageUrl = "/c_images/bg.png",
                X = 4,
                Y = 5,
                OffsetX = 0,
                OffsetY = 0,
                OffsetZ = 0,
            },
        };
        var arena = new SnowWarArena(level, 1);
        var human = AddHuman(arena, 1, 2, 5);
        MoveTo(arena, human, 6, 5);

        var tiles = new List<(int, int)>();

        for (var i = 0; i < 12; i++)
        {
            Turns(arena, 1);
            var view = Human(arena, human);
            tiles.Add((view.TileX, view.TileY));
        }

        Assert.Contains((4, 5), tiles);
        Assert.DoesNotContain(tiles, tile => tile.Item2 != 5);
        Assert.Equal((6, 5), tiles[^1]);
    }

    [Fact]
    public void MissingCharacterAtTheEndOfARow_IsAnExistingTile()
    {
        var arena = new SnowWarArena(Rows(["0000000000", "0000000", "0000000000"]), 1);
        var human = AddHuman(arena, 1, 5, 1);
        MoveTo(arena, human, 8, 1);
        Turns(arena, 10);

        Assert.Equal(8, Human(arena, human).TileX);
    }
}
