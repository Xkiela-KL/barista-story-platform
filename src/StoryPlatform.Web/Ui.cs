using StoryPlatform.Contracts;

namespace StoryPlatform.Web;

/// <summary>German display texts shared by all pages.</summary>
public static class Ui
{
    public static string SeriesUrl(string slug) => $"/serien/{slug}";

    public static string EpisodeUrl(EpisodeView e) => $"/serien/{e.SeriesSlug}/{e.Slug}";

    /// <summary>"Staffel 1, Part 4" (the series decides whether episodes are called "Folge" or "Part").</summary>
    public static string EpisodeCode(EpisodeView e) => $"Staffel {e.SeasonNumber}, {e.EpisodeTerm} {e.Number}";

    /// <summary>"Part 4" for tight spaces.</summary>
    public static string EpisodeCodeShort(EpisodeView e) => $"{e.EpisodeTerm} {e.Number}";

    /// <summary>"3 Parts", "1 Folge", "5 Folgen".</summary>
    public static string Count(int n, string? term)
    {
        term = string.IsNullOrWhiteSpace(term) ? "Folge" : term;
        return
        n == 1 ? $"1 {term}" : $"{n} {(term.EndsWith('e') ? term + "n" : term + "s")}";
    }

    /// <summary>"Krimiserie", "Mysteryserie", or "Serie".</summary>
    public static string GenreNoun(IReadOnlyList<string> genres) => genres.FirstOrDefault() switch
    {
        null or "" or "Story" => "Serie",
        var g => $"{g}serie",
    };

    public static string HeroEyebrow(SeriesSummary s) => s.State == SeriesState.Announced
        ? $"Bald neu: {GenreNoun(s.Genres)}"
        : $"Die aktuelle {GenreNoun(s.Genres)}";

    public static string Genres(IReadOnlyList<string> genres) => string.Join(" · ", genres);

    public static string Duration(int? seconds) => seconds switch
    {
        null => "",
        < 60 => $"{seconds} Sek.",
        _ => $"{seconds / 60}:{seconds % 60:00} Min.",
    };

    public static string StateText(SeriesState state) => state switch
    {
        SeriesState.Announced => "Angekündigt",
        SeriesState.Running => "Läuft",
        SeriesState.Paused => "Pausiert",
        SeriesState.Finished => "Abgeschlossen",
        _ => "",
    };

    public static string StatusText(EpisodeView e, DateTimeOffset now) => e.Status switch
    {
        EpisodeStatus.Published when e.ReleaseAt is { } published => $"Veröffentlicht am {BerlinTime.ShortDate(published)}",
        EpisodeStatus.Published => "Veröffentlicht",
        EpisodeStatus.Scheduled when e.ReleaseAt is { } at => $"{BerlinTime.RelativeDay(at, now)}, {BerlinTime.Clock(at)} Uhr",
        _ => "Termin folgt",
    };

    /// <summary>"Mittwoch | 18:00 Uhr" for the hero.</summary>
    public static string NextSlot(DateTimeOffset at, DateTimeOffset now) =>
        $"{BerlinTime.RelativeDay(at, now)} | {BerlinTime.Clock(at)} Uhr";

    /// <summary>"in 2 Tagen, 21 Std." — same wording as wwwroot/js/site.js.</summary>
    public static string Countdown(DateTimeOffset at, DateTimeOffset now)
    {
        var left = at - now;
        if (left <= TimeSpan.Zero) return "Jetzt verfügbar";
        if (left.TotalDays >= 1)
            return $"in {(int)left.TotalDays} {((int)left.TotalDays == 1 ? "Tag" : "Tagen")}, {left.Hours} Std.";
        if (left.TotalHours >= 1) return $"in {(int)left.TotalHours} Std. {left.Minutes} Min.";
        return $"in {Math.Max(1, left.Minutes)} Min.";
    }

    public static string PlatformName(Platform p) => p switch
    {
        Platform.Instagram => "Instagram",
        Platform.TikTok => "TikTok",
        Platform.YouTube => "YouTube",
        Platform.Facebook => "Facebook",
        Platform.Hosted => "hier",
        _ => "Website",
    };

    public static string Iso(DateTimeOffset at) => at.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
}
