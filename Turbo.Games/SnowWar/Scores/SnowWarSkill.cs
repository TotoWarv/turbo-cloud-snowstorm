using System;

namespace Turbo.Games.SnowWar.Scores;

internal static class SnowWarSkill
{
    private const int MAX_LEVEL = 30;
    private const int POINTS_PER_STEP = 50;

    public static int LevelOf(long totalScore)
    {
        var steps = Math.Max(0, totalScore) / POINTS_PER_STEP;

        return (int)Math.Min(MAX_LEVEL, 1 + (long)Math.Floor(Math.Sqrt(steps)));
    }

    public static int ScoreToNextLevel(long totalScore)
    {
        var level = LevelOf(totalScore);

        if (level >= MAX_LEVEL)
            return 0;

        return (int)
            Math.Clamp(
                (long)POINTS_PER_STEP * level * level - Math.Max(0, totalScore),
                0,
                int.MaxValue
            );
    }
}
