namespace StoryPlatform.Web;

public sealed class SiteOptions
{
    public const string Section = "StoryWeb";

    /// <summary>Where the web app reaches the API. In Docker: http://story-api:8080 (internal network).</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5087";

    /// <summary>Public URL of this website, used for canonical links, sitemap and social previews.</summary>
    public string PublicBaseUrl { get; set; } = "https://stories.xkiela.dev";

    /// <summary>Origin that serves images (the API's public URL). Added to the Content-Security-Policy.</summary>
    public string MediaOrigin { get; set; } = "https://api.xkiela.dev";

    /// <summary>Allow search engines to index the site. Keep false until the placeholder content is replaced.</summary>
    public bool AllowIndexing { get; set; }

    public LegalOptions Legal { get; set; } = new();
}

/// <summary>Data for Impressum and Datenschutzerklärung. Empty values are shown as visible gaps to fill in.</summary>
public sealed class LegalOptions
{
    public string Name { get; set; } = "";
    public string Street { get; set; } = "";
    public string PostalCodeAndCity { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    /// <summary>Person responsible for content (§ 18 Abs. 2 MStV), usually the same person.</summary>
    public string ResponsibleForContent { get; set; } = "";
}
