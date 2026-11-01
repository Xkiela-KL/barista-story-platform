using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StoryPlatform.Contracts;

namespace StoryPlatform.Web.Services;

/// <summary>
/// Typed client for the XKStories REST API. Responses are cached briefly in memory so a page
/// that needs several resources stays fast, and a short API outage does not break every request.
/// Methods return <c>null</c> for unknown items and throw <see cref="ApiUnavailableException"/>
/// when the API cannot be reached; pages then show a friendly notice.
/// </summary>
public sealed class StoryApiClient(HttpClient http, IMemoryCache cache, ILogger<StoryApiClient> log)
{
    private static readonly TimeSpan Fresh = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ChannelView?> ChannelAsync(CancellationToken ct = default) => GetAsync<ChannelView>("v1/channel", ct);

    public Task<FeaturedView?> FeaturedAsync(CancellationToken ct = default) => GetAsync<FeaturedView>("v1/featured", ct);

    public Task<List<SeriesSummary>?> SeriesAsync(CancellationToken ct = default) => GetAsync<List<SeriesSummary>>("v1/series", ct);

    public Task<SeriesDetail?> SeriesAsync(string slug, CancellationToken ct = default) =>
        GetAsync<SeriesDetail>($"v1/series/{Uri.EscapeDataString(slug)}", ct);

    public Task<EpisodeView?> EpisodeAsync(string series, string episode, CancellationToken ct = default) =>
        GetAsync<EpisodeView>($"v1/series/{Uri.EscapeDataString(series)}/episodes/{Uri.EscapeDataString(episode)}", ct);

    public Task<List<EpisodeView>?> LatestAsync(int limit, CancellationToken ct = default) =>
        GetAsync<List<EpisodeView>>($"v1/episodes/latest?limit={limit}", ct);

    public Task<ScheduleView?> ScheduleAsync(DateOnly from, int days, CancellationToken ct = default) =>
        GetAsync<ScheduleView>($"v1/schedule?from={from:yyyy-MM-dd}&days={days}", ct);

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        if (cache.TryGetValue(path, out T? cached)) return cached;
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent) return null;
            response.EnsureSuccessStatusCode();
            var value = await response.Content.ReadFromJsonAsync<T>(Json, ct);
            if (value is not null) cache.Set(path, value, Fresh);
            return value;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            log.LogWarning(ex, "API nicht erreichbar: {Path}", path);
            throw new ApiUnavailableException(path, ex);
        }
    }
}

/// <summary>The API could not be reached or answered with an error (not 404).</summary>
public sealed class ApiUnavailableException(string path, Exception inner)
    : Exception($"Die Inhalte konnten nicht geladen werden ({path}).", inner);
