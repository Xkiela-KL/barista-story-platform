using System.Globalization;

namespace StoryPlatform.Contracts;

/// <summary>All dates on the platform are shown and planned in Europe/Berlin (CET/CEST).</summary>
public static class BerlinTime
{
    public const string TimeZoneId = "Europe/Berlin";

    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    public static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static DateTimeOffset ToBerlin(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    /// <summary>Calendar date in Berlin for an instant.</summary>
    public static DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(ToBerlin(instant).DateTime);

    /// <summary>The UTC instant at which the given Berlin date starts (handles DST).</summary>
    public static DateTimeOffset StartOfDay(DateOnly date) => At(date, new TimeOnly(0, 0));

    /// <summary>UTC instant for a Berlin wall-clock time. Times skipped by DST are moved forward one hour.</summary>
    public static DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (Zone.IsInvalidTime(local)) local = local.AddHours(1);
        var offset = Zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    /// <summary>"Mittwoch"</summary>
    public static string Weekday(DateTimeOffset instant) => ToBerlin(instant).ToString("dddd", German);

    /// <summary>"18:00"</summary>
    public static string Clock(DateTimeOffset instant) => ToBerlin(instant).ToString("HH:mm", German);

    /// <summary>"4. November"</summary>
    public static string DayMonth(DateTimeOffset instant) => ToBerlin(instant).ToString("d. MMMM", German);

    /// <summary>"Mittwoch, 4. November 2026"</summary>
    public static string LongDate(DateTimeOffset instant) => ToBerlin(instant).ToString("dddd, d. MMMM yyyy", German);

    /// <summary>"04.11.2026"</summary>
    public static string ShortDate(DateTimeOffset instant) => ToBerlin(instant).ToString("dd.MM.yyyy", German);

    /// <summary>"Heute", "Morgen" or the weekday, relative to <paramref name="now"/>.</summary>
    public static string RelativeDay(DateTimeOffset instant, DateTimeOffset now)
    {
        var days = DateOf(instant).DayNumber - DateOf(now).DayNumber;
        return days switch
        {
            0 => "Heute",
            1 => "Morgen",
            -1 => "Gestern",
            > 1 and < 7 => Weekday(instant),
            _ => $"{Weekday(instant)}, {DayMonth(instant)}",
        };
    }
}
