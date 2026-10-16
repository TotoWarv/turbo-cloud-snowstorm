using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Turbo.Games.Configuration;
using Turbo.Tests.Support;
using Xunit;

namespace Turbo.Tests.Games.SnowWar;

public sealed class SnowWarConfigTests : IDisposable
{
    private const string BACKDROP_FOLDER = "/cms/flash-files/c_images/snowstorm_client/official/";

    private readonly SqliteDb _db = new();
    private readonly List<SettingsHarness> _harnesses = [];

    public void Dispose()
    {
        foreach (var harness in _harnesses)
            harness.Dispose();

        _db.Dispose();
    }

    [Fact]
    public void without_a_section_the_game_runs_with_the_planned_defaults() =>
        ShouldBePlannedDefaults(Harness("{}").Running<SnowWarConfig>());

    [Fact]
    public void the_hotel_appsettings_section_matches_the_defaults()
    {
        var section = HotelSection();

        section.Should().NotBeNull("appsettings.json declares Turbo:Games:SnowWar");

        var appSettings = new JsonObject
        {
            ["Turbo"] = new JsonObject
            {
                ["Games"] = new JsonObject { ["SnowWar"] = section!.DeepClone() },
            },
        };

        ShouldBePlannedDefaults(Harness(appSettings.ToJsonString()).Running<SnowWarConfig>());
    }

    [Fact]
    public void every_snowwar_setting_is_listed_for_the_panel()
    {
        var settings = Harness("{}").Settings;

        settings.Get("Turbo:Games:SnowWar:MinPlayers").Should().NotBeNull();
        settings.Get("Turbo:Games:SnowWar:MaxConcurrentGames").Should().NotBeNull();
        settings.Get("Turbo:Games:SnowWar:Arenas").Should().NotBeNull();
        settings.Get("Turbo:Games:SnowWar:RayGunsEnabled").Should().NotBeNull();
    }

    private static void ShouldBePlannedDefaults(SnowWarConfig config)
    {
        config.MinPlayers.Should().Be(2);
        config.MaxPlayers.Should().Be(8);
        config.MaxConcurrentGames.Should().Be(4);
        config.LobbyCountdownSeconds.Should().Be(15);
        config.GameLengthSeconds.Should().Be(180);
        config.PreparingSeconds.Should().Be(5);
        config.RematchSeconds.Should().Be(30);
        config.LeaveBlockSeconds.Should().Be(180);
        config.StageLoadTimeoutSeconds.Should().Be(10);
        config.Arenas.Should().Equal(8, 9, 11);
        config.RayGunsEnabled.Should().BeTrue();
        config
            .Backdrops.Should()
            .BeEquivalentTo(
                new Dictionary<int, string>
                {
                    [8] = BACKDROP_FOLDER + "snst_bg_1_a_big.png",
                    [9] = BACKDROP_FOLDER + "snst_bg_2_big.png",
                    [11] = BACKDROP_FOLDER + "snst_bg_3_noscale.png",
                }
            );
    }

    private SettingsHarness Harness(string appSettings)
    {
        var harness = new SettingsHarness(
            _db,
            appSettings,
            (services, configuration) =>
                services.Configure<SnowWarConfig>(
                    configuration.GetSection(SnowWarConfig.SECTION_NAME)
                )
        );

        _harnesses.Add(harness);

        return harness;
    }

    private static JsonNode? HotelSection()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (
            directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "appsettings.json"))
        )
            directory = directory.Parent;

        directory.Should().NotBeNull();

        var root = JsonNode.Parse(
            File.ReadAllText(Path.Combine(directory!.FullName, "appsettings.json"))
        );

        return root?["Turbo"]?["Games"]?["SnowWar"];
    }
}
