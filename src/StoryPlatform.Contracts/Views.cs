namespace StoryPlatform.Contracts;

// Views are what the public read endpoints return. Image URLs are absolute,
// status values are computed by the API from the release time.

public sealed record ChannelView(
    string Name,
    string Claim,
    string Description,
    IReadOnlyList<SocialLink> SocialLinks,
    bool HasPlaceholderContent);

public sealed record SeriesSummary(
    string Slug,
    string Title,
    string Tagline,
    IReadOnlyList<string> Genres,
    SeriesState State,
    string? CoverUrl,
    string? HeroUrl,
    string? SocialImageUrl,
    string EpisodeTerm,
    bool IsPlaceholder,
    int SeasonCount,
    int PublishedEpisodeCount,
    int EpisodeCount,
    EpisodeView? NextEpisode,
    EpisodeView? LatestEpisode);

public sealed record SeriesDetail(
    SeriesSummary Summary,
    string Description,
    IReadOnlyList<SocialLink> SocialLinks,
    IReadOnlyList<SeasonView> Seasons);

public sealed record SeasonView(
    int Number,
    string? Title,
    string? Description,
    IReadOnlyList<EpisodeView> Episodes);

public sealed record EpisodeView(
    string SeriesSlug,
    string SeriesTitle,
    string EpisodeTerm,
    string Slug,
    int SeasonNumber,
    int Number,
    string Title,
    string Synopsis,
    int? DurationSeconds,
    DateTimeOffset? ReleaseAt,
    EpisodeStatus Status,
    string? Label,
    string? ThumbnailUrl,
    /// <summary>Only filled once the episode is published.</summary>
    IReadOnlyList<VideoSource> VideoSources,
    AccessLevel Access,
    /// <summary>Release date and links are examples, not a real release.</summary>
    bool IsPlaceholder);

public sealed record FeaturedView(SeriesSummary Series, EpisodeView? NextEpisode);

public sealed record ScheduleView(
    DateTimeOffset From,
    DateTimeOffset To,
    string TimeZone,
    IReadOnlyList<EpisodeView> Entries);

public sealed record MediaUploadResult(string FileName, string Url);

public sealed record ApiProblem(string Title, IReadOnlyList<string> Errors);
