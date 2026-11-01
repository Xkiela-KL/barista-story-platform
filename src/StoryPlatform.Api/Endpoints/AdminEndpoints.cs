using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using StoryPlatform.Api.Content;
using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Endpoints;

/// <summary>
/// Content management. Every request needs the header <c>X-Api-Key</c>. Changes are validated as a whole
/// document and clear the output cache, so the website shows them within a minute.
/// </summary>
public static class AdminEndpoints
{
    public const string ApiKeyHeader = "X-Api-Key";

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin")
            .WithTags("Verwaltung")
            .AddEndpointFilter<ApiKeyFilter>()
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimits.Admin);

        // Full export / import of the programme (handy for backups and bulk edits).
        admin.MapGet("/content", (IContentStore store) => store.Current);
        admin.MapPut("/content", (ContentDocument doc, IContentStore store, IOutputCacheStore cache, CancellationToken ct) =>
            Save(store, cache, _ => doc, ct));

        admin.MapPut("/channel", (ChannelDocument channel, IContentStore store, IOutputCacheStore cache, CancellationToken ct) =>
            Save(store, cache, c => c with { Channel = channel }, ct));

        // Create or replace one series including its seasons and episodes.
        admin.MapPut("/series/{slug}", (string slug, SeriesDocument series, IContentStore store, IOutputCacheStore cache, CancellationToken ct) =>
        {
            var normalized = series with { Slug = slug };
            return Save(store, cache, c =>
            {
                var list = c.Series.ToList();
                var index = list.FindIndex(s => s.Slug == slug);
                if (index >= 0) list[index] = normalized; else list.Add(normalized);
                return c with { Series = list };
            }, ct);
        });

        admin.MapDelete("/series/{slug}", async (string slug, IContentStore store, IOutputCacheStore cache, CancellationToken ct) =>
        {
            if (store.Current.Series.All(s => s.Slug != slug))
                return Results.NotFound(new ApiProblem("Nicht gefunden", ["Diese Serie gibt es nicht."]));
            return await Save(store, cache, c => c with { Series = c.Series.Where(s => s.Slug != slug).ToList() }, ct);
        });

        admin.MapGet("/media", (IOptions<ApiOptions> options, ContentProjector content) =>
            Directory.EnumerateFiles(options.Value.MediaDirectory)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order()
                .Select(name => new MediaUploadResult(name, content.MediaUrl(name)!)));

        // Upload a cover, key art or thumbnail (jpg, png, webp). Field name: "file".
        admin.MapPost("/media", MediaEndpoints.UploadAsync);
    }

    private static async Task<IResult> Save(IContentStore store, IOutputCacheStore cache,
        Func<ContentDocument, ContentDocument> change, CancellationToken ct)
    {
        try
        {
            var saved = await store.UpdateAsync(change, ct);
            await cache.EvictByTagAsync(OutputCacheSetup.ContentTag, ct);
            return Results.Ok(new { saved = true, series = saved.Series.Count });
        }
        catch (ContentValidationException ex)
        {
            return Results.UnprocessableEntity(new ApiProblem("Inhalte ungültig", ex.Errors));
        }
    }

    private sealed class ApiKeyFilter(IOptions<ApiOptions> options) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var expected = options.Value.AdminApiKey;
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem("Die Verwaltung ist deaktiviert: StoryApi:AdminApiKey ist nicht gesetzt.", statusCode: 503);

            var given = context.HttpContext.Request.Headers[ApiKeyHeader].ToString();
            var ok = given.Length > 0 && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));
            return ok ? await next(context) : Results.Unauthorized();
        }
    }
}
