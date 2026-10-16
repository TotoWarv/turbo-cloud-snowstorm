using System.Collections.Generic;

namespace Turbo.Games.Configuration;

public sealed class SnowWarConfig
{
    public const string SECTION_NAME = "Turbo:Games:SnowWar";

    private const string BACKDROP_FOLDER = "/cms/flash-files/c_images/snowstorm_client/official/";

    public int MinPlayers { get; init; } = 2;

    public int MaxPlayers { get; init; } = 8;

    public int MaxConcurrentGames { get; init; } = 4;

    public int LobbyCountdownSeconds { get; init; } = 15;

    public int GameLengthSeconds { get; init; } = 180;

    public int PreparingSeconds { get; init; } = 5;

    public int RematchSeconds { get; init; } = 30;

    public int LeaveBlockSeconds { get; init; } = 180;

    public int[] Arenas { get; init; } = [8, 9, 11];

    public bool RayGunsEnabled { get; init; } = true;

    public Dictionary<int, string> Backdrops { get; init; } =
        new()
        {
            [8] = BACKDROP_FOLDER + "snst_bg_1_a_big.png",
            [9] = BACKDROP_FOLDER + "snst_bg_2_big.png",
            [11] = BACKDROP_FOLDER + "snst_bg_3_noscale.png",
        };
}
