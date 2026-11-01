namespace StoryPlatform.Contracts;

// Documents are what the content store keeps and what the admin endpoints accept.
// Image fields hold either a file name from the media library ("hafenlichter-cover.jpg")
// or an absolute https URL. All times are UTC instants (DateTimeOffset).

public sealed record ContentDocument
{
    public int SchemaVersion { get; init; } = 1;
    public ChannelDocument Channel { get; init; } = new();
    public IReadOnlyList<SeriesDocument> Series { get; init; } = [];
}

public sealed record ChannelDocument
{
    public string Name { get; init; } = "XKStories";
    public string Claim { get; init; } = "";
    public string Description { get; init; } = "";
    public IReadOnlyList<SocialLink> SocialLinks { get; init; } = [];
}

public sealed record SocialLink
{
    public Platform Platform { get; init; }
    /// <summary>Profile URL. <c>null</c> means "not yet available" and is not rendered as a link.</summary>
    public string? Url { get; init; }
    public string? Handle { get; init; }
}

public sealed record SeriesDocument
{
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public string Tagline { get; init; } = "";
    public string Description { get; init; } = "";
    /// <summary>Main genre first, e.g. ["Krimi", "Drama"].</summary>
    public IReadOnlyList<string> Genres { get; init; } = [];
    public SeriesState State { get; init; } = SeriesState.Running;
    /// <summary>Portrait cover (2:3).</summary>
    public string? CoverImage { get; init; }
    /// <summary>Key art for the homepage hero. Portrait images (2:3, like the episode stills) work best.</summary>
    public string? HeroImage { get; init; }
    /// <summary>Preview image for links shared in messengers and social media (1200×630).</summary>
    public string? SocialImage { get; init; }
    /// <summary>What an episode is called in this series: "Folge" (default) or e.g. "Part".</summary>
    public string EpisodeTerm { get; init; } = "Folge";
    /// <summary>Lowest rank is shown in the homepage hero; <c>null</c> = not featured.</summary>
    public int? FeaturedRank { get; init; }
    /// <summary>True for an example series. The website labels it as "Beispiel".</summary>
    public bool IsPlaceholder { get; init; }
    public IReadOnlyList<SocialLink> SocialLinks { get; init; } = [];
    public IReadOnlyList<SeasonDocument> Seasons { get; init; } = [];
}

public sealed record SeasonDocument
{
    public int Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<EpisodeDocument> Episodes { get; init; } = [];
}

public sealed record EpisodeDocument
{
    /// <summary>Unique within the series, used in URLs, e.g. "s01e04-die-zeugin".</summary>
    public string Slug { get; init; } = "";
    public int Number { get; init; }
    public string Title { get; init; } = "";
    public string Synopsis { get; init; } = "";
    public int? DurationSeconds { get; init; }
    /// <summary>Release instant (UTC). <c>null</c> = no date planned yet.</summary>
    public DateTimeOffset? ReleaseAt { get; init; }
    /// <summary>Optional highlight in the schedule, e.g. "Staffelfinale" or "Premiere".</summary>
    public string? Label { get; init; }
    /// <summary>Vertical thumbnail (9:16).</summary>
    public string? Thumbnail { get; init; }
    public IReadOnlyList<VideoSource> VideoSources { get; init; } = [];
    public AccessLevel Access { get; init; } = AccessLevel.Free;
    /// <summary>True when the release date (and missing links) are examples. Shown as "Beispieltermin".</summary>
    public bool IsPlaceholder { get; init; }
}

public sealed record VideoSource
{
    public Platform Platform { get; init; }
    public string Url { get; init; } = "";
}
