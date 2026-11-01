using System.Text.Json;
using System.Text.Json.Nodes;
using StoryPlatform.Contracts;

namespace StoryPlatform.Web;

/// <summary>schema.org data for search engines (TVSeries / TVEpisode), serialised safely for a script tag.</summary>
public static class SchemaOrg
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default, // escapes <, > and & so the JSON cannot close the tag
    };

    public static string Series(SeriesDetail s, string url)
    {
        var node = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "TVSeries",
            ["name"] = s.Summary.Title,
            ["description"] = s.Description,
            ["genre"] = new JsonArray(s.Summary.Genres.Select(g => (JsonNode)JsonValue.Create(g)!).ToArray()),
            ["inLanguage"] = "de",
            ["url"] = url,
            ["numberOfSeasons"] = s.Seasons.Count,
            ["numberOfEpisodes"] = s.Seasons.Sum(z => z.Episodes.Count),
        };
        if (s.Summary.CoverUrl is not null) node["image"] = s.Summary.CoverUrl;
        return node.ToJsonString(Options);
    }

    public static string Episode(EpisodeView e, string url, string seriesUrl)
    {
        var node = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "TVEpisode",
            ["name"] = e.Title,
            ["episodeNumber"] = e.Number,
            ["description"] = e.Synopsis,
            ["inLanguage"] = "de",
            ["url"] = url,
            ["partOfSeason"] = new JsonObject { ["@type"] = "TVSeason", ["seasonNumber"] = e.SeasonNumber },
            ["partOfSeries"] = new JsonObject { ["@type"] = "TVSeries", ["name"] = e.SeriesTitle, ["url"] = seriesUrl },
        };
        if (e.ThumbnailUrl is not null) node["image"] = e.ThumbnailUrl;
        if (e is { Status: EpisodeStatus.Published, ReleaseAt: { } at, IsPlaceholder: false })
            node["datePublished"] = at.ToString("yyyy-MM-ddTHH:mm:sszzz");
        return node.ToJsonString(Options);
    }
}
