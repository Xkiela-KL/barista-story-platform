using System.Text.RegularExpressions;
using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Content;

public static partial class ContentValidator
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public static partial Regex SlugPattern();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]*\.(jpg|jpeg|png|webp)$")]
    public static partial Regex MediaFilePattern();

    public static IReadOnlyList<string> Validate(ContentDocument doc)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(doc.Channel.Name)) errors.Add("Der Sender braucht einen Namen.");
        foreach (var link in doc.Channel.SocialLinks) CheckUrl(link.Url, "Sender-Link", errors, optional: true);

        var slugs = new HashSet<string>();
        foreach (var series in doc.Series)
        {
            var where = $"Serie „{series.Slug}“";
            if (!SlugPattern().IsMatch(series.Slug)) errors.Add($"{where}: Kürzel nur aus a–z, 0–9 und Bindestrichen.");
            if (!slugs.Add(series.Slug)) errors.Add($"{where}: Kürzel ist doppelt vergeben.");
            if (string.IsNullOrWhiteSpace(series.Title)) errors.Add($"{where}: Titel fehlt.");
            CheckImage(series.CoverImage, $"{where}, Cover", errors);
            CheckImage(series.HeroImage, $"{where}, Titelbild", errors);
            CheckImage(series.SocialImage, $"{where}, Vorschaubild für Social Media", errors);
            if (string.IsNullOrWhiteSpace(series.EpisodeTerm)) errors.Add($"{where}: Bezeichnung für Folgen fehlt (z. B. „Folge“ oder „Part“).");
            foreach (var link in series.SocialLinks) CheckUrl(link.Url, $"{where}, Link", errors, optional: true);

            var seasonNumbers = new HashSet<int>();
            var episodeSlugs = new HashSet<string>();
            foreach (var season in series.Seasons)
            {
                if (season.Number < 1) errors.Add($"{where}: Staffelnummern beginnen bei 1.");
                if (!seasonNumbers.Add(season.Number)) errors.Add($"{where}: Staffel {season.Number} ist doppelt.");

                var episodeNumbers = new HashSet<int>();
                foreach (var episode in season.Episodes)
                {
                    var ep = $"{where}, Staffel {season.Number}, Folge {episode.Number}";
                    if (episode.Number < 1) errors.Add($"{ep}: Folgennummern beginnen bei 1.");
                    if (!episodeNumbers.Add(episode.Number)) errors.Add($"{ep}: Nummer ist doppelt.");
                    if (!SlugPattern().IsMatch(episode.Slug)) errors.Add($"{ep}: Kürzel nur aus a–z, 0–9 und Bindestrichen.");
                    if (!episodeSlugs.Add(episode.Slug)) errors.Add($"{ep}: Kürzel „{episode.Slug}“ ist in der Serie doppelt.");
                    if (string.IsNullOrWhiteSpace(episode.Title)) errors.Add($"{ep}: Titel fehlt.");
                    if (episode.DurationSeconds is <= 0) errors.Add($"{ep}: Dauer muss positiv sein.");
                    CheckImage(episode.Thumbnail, $"{ep}, Vorschaubild", errors);
                    foreach (var source in episode.VideoSources) CheckUrl(source.Url, $"{ep}, Video-Link", errors, optional: false);
                }
            }
        }
        return errors;
    }

    private static void CheckImage(string? value, string where, List<string> errors)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (value.StartsWith("https://", StringComparison.Ordinal)) return;
        if (!MediaFilePattern().IsMatch(value))
            errors.Add($"{where}: „{value}“ ist weder ein Dateiname aus der Mediathek noch eine https-Adresse.");
    }

    private static void CheckUrl(string? value, string where, List<string> errors, bool optional)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (!optional) errors.Add($"{where}: Adresse fehlt.");
            return;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            errors.Add($"{where}: „{value}“ ist keine https-Adresse.");
    }
}
