namespace StoryPlatform.Web.Services;

/// Where the aggregated visit statistics are persisted.
public sealed class VisitCounterOptions
{
    public const string Section = "VisitCounter";

    /// Absolute path, or relative to the content root. The directory must be writable.
    public string DataPath { get; set; } = "data/visits.json";

    /// How often pending counts are written to disk. Shutdown always writes once more.
    public int FlushIntervalSeconds { get; set; } = 60;
}
