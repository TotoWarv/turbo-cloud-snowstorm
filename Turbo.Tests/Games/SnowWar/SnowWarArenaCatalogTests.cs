using FluentAssertions;
using Microsoft.Extensions.Options;
using Turbo.Games.Configuration;
using Turbo.Games.SnowWar;
using Turbo.Games.SnowWar.Simulation;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarArenaCatalogTests
{
    private const string BACKDROP_FOLDER = "/cms/flash-files/c_images/snowstorm_client/official/";

    private static SnowWarArenaCatalog Catalog(SnowWarConfig? config = null) =>
        new(Options.Create(config ?? new SnowWarConfig()));

    [Theory]
    [InlineData(8, "Arctic Island", 102)]
    [InlineData(9, "Dragon Top", 37)]
    [InlineData(11, "Fight Night", 64)]
    public void each_official_arena_loads_with_its_name_size_and_objects(
        int fieldType,
        string name,
        int objectCount
    )
    {
        var level = Catalog().Get(fieldType);

        level.FieldType.Should().Be(fieldType);
        level.Name.Should().Be(name);
        level.Width.Should().Be(50);
        level.Height.Should().Be(50);
        level.HeightMap.Should().HaveCount(50);
        level.Objects.Should().HaveCount(objectCount);
    }

    [Fact]
    public void arctic_island_has_its_composition_and_five_spawns_per_team()
    {
        var level = Catalog().Get(8);

        level.Objects.Count(o => o.Name == "snst_block1").Should().Be(67);
        level.Objects.Count(o => o.Name == "snst_tree1").Should().Be(18);
        level.Objects.Count(o => o.Name == "snst_fence").Should().Be(11);
        level.Objects.Count(o => o.Name == "ads_igorraygun").Should().Be(5);
        level.Objects.Count(o => o.Name == "s_snowball_machine").Should().Be(1);
        level.Objects.Count(o => o.Altitude != 0).Should().Be(30);

        level.TeamSpawns.Should().HaveCount(2);
        level.TeamSpawns[0].Should().HaveCount(5);
        level.TeamSpawns[1].Should().HaveCount(5);
        level.TeamSpawns[0][0].Should().Be(new SnowWarTilePosition(22, 9));
        level.TeamSpawns[1][0].Should().Be(new SnowWarTilePosition(30, 43));
    }

    [Fact]
    public void an_object_line_gives_name_anchor_direction_and_optional_altitude()
    {
        var level = Catalog().Get(8);

        level
            .Objects[^1]
            .Should()
            .Be(
                new SnowWarLevelObjectData
                {
                    Name = "s_snowball_machine",
                    X = 26,
                    Y = 24,
                    Direction = 0,
                }
            );
        level
            .Objects.Should()
            .Contain(o => o.Name == "snst_block1" && o.X == 38 && o.Y == 37 && o.Altitude == 1440);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void arenas_without_spawns_have_none(int fieldType) =>
        Catalog().Get(fieldType).TeamSpawns.Should().BeEmpty();

    [Theory]
    [InlineData(8, 0, 19, -1160, 1554, 10000, "snst_bg_1_a_big.png")]
    [InlineData(9, 0, 22, -1106, 1496, 9920, "snst_bg_2_big.png")]
    [InlineData(11, 0, 22, -1096, 1444, 9950, "snst_bg_3_noscale.png")]
    public void each_arena_carries_its_backdrop_served_from_the_flash_files(
        int fieldType,
        int x,
        int y,
        int offsetX,
        int offsetY,
        int offsetZ,
        string image
    ) =>
        Catalog()
            .Get(fieldType)
            .Backdrop.Should()
            .Be(
                new SnowWarBackdropData
                {
                    ImageUrl = BACKDROP_FOLDER + image,
                    X = x,
                    Y = y,
                    OffsetX = offsetX,
                    OffsetY = offsetY,
                    OffsetZ = offsetZ,
                }
            );

    [Fact]
    public void an_arena_without_a_configured_backdrop_image_has_no_backdrop()
    {
        var config = new SnowWarConfig { Backdrops = new Dictionary<int, string> { [8] = "" } };

        Catalog(config).Get(8).Backdrop.Should().BeNull();
        Catalog(config).Get(9).Backdrop.Should().BeNull();
    }

    [Fact]
    public void an_unknown_arena_is_refused() =>
        FluentActions
            .Invoking(() => Catalog().Get(42))
            .Should()
            .Throw<ArgumentOutOfRangeException>();

    [Theory]
    [InlineData(8, 9)]
    [InlineData(9, 11)]
    [InlineData(11, 8)]
    public void the_next_arena_follows_the_configured_rotation(int previous, int next) =>
        Catalog().Next(previous).Should().Be(next);

    [Fact]
    public void an_arena_left_out_of_the_configuration_is_never_drawn()
    {
        var catalog = Catalog(new SnowWarConfig { Arenas = [11, 8] });

        catalog.Arenas.Should().Equal(11, 8);
        catalog.Next(11).Should().Be(8);
        catalog.Next(8).Should().Be(11);
        catalog.Next(9).Should().Be(11);

        var drawn = new List<int>();
        var previous = 9;

        for (var i = 0; i < 12; i++)
        {
            previous = catalog.Next(previous);
            drawn.Add(previous);
        }

        drawn.Should().NotContain(9).And.OnlyContain(field => field == 8 || field == 11);
    }

    [Fact]
    public void a_configured_arena_without_data_is_never_drawn()
    {
        var catalog = Catalog(new SnowWarConfig { Arenas = [8, 42, 9] });

        catalog.Arenas.Should().Equal(8, 9);
        catalog.Next(8).Should().Be(9);
        catalog.Next(9).Should().Be(8);
    }

    [Fact]
    public void with_no_playable_arena_none_can_be_drawn()
    {
        var catalog = Catalog(new SnowWarConfig { Arenas = [42] });

        catalog.Arenas.Should().BeEmpty();
        FluentActions.Invoking(() => catalog.Next(8)).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(8, 19)]
    [InlineData(9, 20)]
    [InlineData(11, 51)]
    public void each_arena_builds_a_playable_simulation(int fieldType, int gameObjects) =>
        new SnowWarArena(Catalog().Get(fieldType), 1234)
            .GetFullStatus()
            .Should()
            .HaveCount(gameObjects);
}
