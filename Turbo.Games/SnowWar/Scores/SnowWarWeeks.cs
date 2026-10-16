using System;
using System.Globalization;

namespace Turbo.Games.SnowWar.Scores;

internal static class SnowWarWeeks
{
    private const int DAYS_PER_WEEK = 7;

    public static DateOnly StartOf(DateTimeOffset now)
    {
        var day = DateOnly.FromDateTime(now.UtcDateTime);
        var sinceMonday =
            ((int)day.DayOfWeek - (int)DayOfWeek.Monday + DAYS_PER_WEEK) % DAYS_PER_WEEK;

        return day.AddDays(-sinceMonday);
    }

    public static DateOnly Back(DateOnly weekStart, int weeks) =>
        weekStart.AddDays(-weeks * DAYS_PER_WEEK);

    public static int Between(DateOnly older, DateOnly newer) =>
        Math.Max(0, (newer.DayNumber - older.DayNumber) / DAYS_PER_WEEK);

    public static int MinutesUntilNext(DateOnly weekStart, DateTimeOffset now)
    {
        var next = new DateTimeOffset(
            weekStart.AddDays(DAYS_PER_WEEK).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );

        return (int)Math.Ceiling((next - now).TotalMinutes);
    }

    public static int IsoYear(DateOnly weekStart) =>
        ISOWeek.GetYear(weekStart.ToDateTime(TimeOnly.MinValue));

    public static int IsoWeek(DateOnly weekStart) =>
        ISOWeek.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue));
}
