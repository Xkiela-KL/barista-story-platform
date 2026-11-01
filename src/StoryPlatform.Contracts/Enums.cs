using System.Text.Json.Serialization;

namespace StoryPlatform.Contracts;

/// <summary>Computed from <c>ReleaseAt</c> and the current time; never stored.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EpisodeStatus>))]
public enum EpisodeStatus
{
    /// <summary>No release date planned yet.</summary>
    Unscheduled,
    /// <summary>Release date lies in the future.</summary>
    Scheduled,
    /// <summary>Release date has passed.</summary>
    Published,
}

[JsonConverter(typeof(JsonStringEnumConverter<SeriesState>))]
public enum SeriesState
{
    Announced,
    Running,
    Paused,
    Finished,
}

/// <summary>Where a video or a channel lives. <see cref="Hosted"/> is reserved for own video hosting later.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Platform>))]
public enum Platform
{
    Instagram,
    TikTok,
    YouTube,
    Facebook,
    Website,
    Hosted,
}

/// <summary>
/// Who may watch an episode. Version 1 only uses <see cref="Free"/>; <see cref="Members"/> is reserved
/// for subscriptions so that the data model does not have to change when accounts arrive.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccessLevel>))]
public enum AccessLevel
{
    Free,
    Members,
}
