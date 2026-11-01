namespace StoryPlatform.Api;

public sealed class ApiOptions
{
    public const string Section = "StoryApi";

    /// <summary>Public base URL of this API, used for absolute image URLs (https://api.xkiela.dev).</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5087";

    /// <summary>Folder for content.json and the media library. Mounted as a Docker volume in production.</summary>
    public string DataDirectory { get; set; } = "data";

    /// <summary>Key for /v1/admin/*. Empty = admin endpoints are disabled.</summary>
    public string AdminApiKey { get; set; } = "";

    /// <summary>Browser origins allowed to call the public GET endpoints from JavaScript.</summary>
    public string[] CorsOrigins { get; set; } = [];

    /// <summary>Create example content (clearly marked as placeholder) when no content.json exists yet.</summary>
    public bool SeedPlaceholderContent { get; set; } = true;

    /// <summary>Largest accepted image upload in bytes.</summary>
    public long MaxUploadBytes { get; set; } = 8 * 1024 * 1024;

    public string ContentFile => Path.Combine(DataDirectory, "content.json");
    public string MediaDirectory => Path.Combine(DataDirectory, "media");
}
