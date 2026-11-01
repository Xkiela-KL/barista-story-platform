using Microsoft.Extensions.Options;
using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Content;

/// <summary>Turns stored documents into public views: absolute image URLs and computed status.</summary>
public sealed class ContentProjector(IContentStore store, TimeProvider clock, IOptions<ApiOptions> options)
{
    private readonly string _mediaBase = options.Value.PublicBaseUrl.TrimEnd('/') + "/media/";

    public DateTimeOffset Now => clock.GetUtcNow();

    public ChannelView Channel()
    {
        var doc = store.Current;
        var hasPlaceholders = doc.Series.Any(s => s.IsPlaceholder || s.Seasons.Any(z => z.Episodes.Any(e => e.IsPlaceholder)));
        return new ChannelView(doc.Channel.Name, doc.Channel.Claim, doc.Channel.Description, doc.Channel.SocialLinks, hasPlaceholders);
    }

    public IReadOnlyList<SeriesSummary> AllSeries() =>
        store.Current.Series
            .Select(Summary)
            .OrderBy(s => store.Current.Series.First(x => x.Slug == s.Slug).FeaturedRank ?? int.MaxValue)
            .ThenBy(s => s.NextEpisode?.ReleaseAt ?? DateTimeOffset.MaxValue)
            .ThenBy(s => s.Title, StringComparer.Create(BerlinTime.German, ignoreCase: true))
            .ToList();

    public SeriesDetail? Series(string slug)
    {
        var doc = store.Current.Series.FirstOrDefault(s => s.Slug == slug);
        if (doc is null) return null;
        var seasons = doc.Seasons
            .OrderBy(s => s.Number)
            .Select(s => new SeasonView(s.Number, s.Title, s.Description,
                s.Episodes.OrderBy(e => e.Number).Select(e => Episode(doc, s, e)).ToList()))
            .ToList();
        return new SeriesDetail(Summary(doc), doc.Description, doc.SocialLinks, seasons);
    }

    public EpisodeView? Episode(string seriesSlug, string episodeSlug) =>
        AllEpisodes().FirstOrDefault(e => e.SeriesSlug == seriesSlug && e.Slug == episodeSlug);

    /// <summary>The series for the homepage hero: lowest featured rank, else the one with the next release.</summary>
    public FeaturedView? Featured()
    {
        var docs = store.Current.Series;
        if (docs.Count == 0) return null;
        var pick = docs.Where(s => s.FeaturedRank is not null).OrderBy(s => s.FeaturedRank).FirstOrDefault()
                   ?? docs.Select(s => (s, next: NextRelease(s))).OrderBy(x => x.next ?? DateTimeOffset.MaxValue).First().s;
        var summary = Summary(pick);
        return new FeaturedView(summary, summary.NextEpisode);
    }

    public ScheduleView Schedule(DateTimeOffset from, DateTimeOffset to)
    {
        var entries = AllEpisodes()
            .Where(e => e.ReleaseAt is { } at && at >= from && at < to)
            .OrderBy(e => e.ReleaseAt)
            .ThenBy(e => e.SeriesTitle)
            .ToList();
        return new ScheduleView(from, to, BerlinTime.TimeZoneId, entries);
    }

    public IReadOnlyList<EpisodeView> Latest(int limit) =>
        AllEpisodes()
            .Where(e => e.Status == EpisodeStatus.Published)
            .OrderByDescending(e => e.ReleaseAt)
            .Take(limit)
            .ToList();

    public string? MediaUrl(string? value) => value switch
    {
        null or "" => null,
        _ when value.StartsWith("https://", StringComparison.Ordinal) => value,
        _ => _mediaBase + Uri.EscapeDataString(value),
    };

    private IEnumerable<EpisodeView> AllEpisodes() =>
        store.Current.Series.SelectMany(s => s.Seasons.SelectMany(z => z.Episodes.Select(e => Episode(s, z, e))));

    private SeriesSummary Summary(SeriesDocument s)
    {
        var episodes = s.Seasons.SelectMany(z => z.Episodes.Select(e => Episode(s, z, e))).ToList();
        var next = episodes.Where(e => e.Status == EpisodeStatus.Scheduled).MinBy(e => e.ReleaseAt);
        var latest = episodes.Where(e => e.Status == EpisodeStatus.Published).MaxBy(e => e.ReleaseAt);
        return new SeriesSummary(
            s.Slug, s.Title, s.Tagline, s.Genres, s.State,
            MediaUrl(s.CoverImage), MediaUrl(s.HeroImage ?? s.CoverImage), MediaUrl(s.SocialImage ?? s.HeroImage ?? s.CoverImage),
            s.EpisodeTerm,
            s.IsPlaceholder,
            s.Seasons.Count,
            episodes.Count(e => e.Status == EpisodeStatus.Published),
            episodes.Count,
            next, latest);
    }

    private DateTimeOffset? NextRelease(SeriesDocument s) =>
        s.Seasons.SelectMany(z => z.Episodes).Select(e => e.ReleaseAt).Where(r => r > Now).Min();

    private EpisodeView Episode(SeriesDocument s, SeasonDocument season, EpisodeDocument e)
    {
        var status = e.ReleaseAt switch
        {
            null => EpisodeStatus.Unscheduled,
            { } at when at <= Now => EpisodeStatus.Published,
            _ => EpisodeStatus.Scheduled,
        };
        return new EpisodeView(
            s.Slug, s.Title, s.EpisodeTerm, e.Slug, season.Number, e.Number, e.Title, e.Synopsis, e.DurationSeconds,
            e.ReleaseAt, status, e.Label,
            MediaUrl(e.Thumbnail ?? s.CoverImage),
            status == EpisodeStatus.Published ? e.VideoSources : [],
            e.Access,
            e.IsPlaceholder || s.IsPlaceholder);
    }
}
