using StoryPlatform.Api.Content;
using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Endpoints;

public static class PublicEndpoints
{
    public const string CachePolicy = "public-60s";

    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1")
            .WithTags("Öffentlich")
            .RequireCors(CorsSetup.PublicPolicy)
            .CacheOutput(CachePolicy);

        v1.MapGet("/channel", (ContentProjector content) => content.Channel());

        v1.MapGet("/featured", (ContentProjector content) =>
            content.Featured() is { } featured ? Results.Ok(featured) : Results.NoContent());

        v1.MapGet("/series", (ContentProjector content) => content.AllSeries());

        v1.MapGet("/series/{slug}", (string slug, ContentProjector content) =>
            content.Series(slug) is { } series ? Results.Ok(series) : NotFound("Diese Serie gibt es nicht."));

        v1.MapGet("/series/{slug}/episodes/{episode}", (string slug, string episode, ContentProjector content) =>
            content.Episode(slug, episode) is { } view ? Results.Ok(view) : NotFound("Diese Folge gibt es nicht."));

        v1.MapGet("/episodes/latest", (ContentProjector content, int? limit) =>
            content.Latest(Math.Clamp(limit ?? 12, 1, 50)));

        // Schedule window in Berlin calendar days: from (yyyy-MM-dd, default today) for `days` days (1–60).
        v1.MapGet("/schedule", (ContentProjector content, string? from, int? days) =>
        {
            var start = BerlinTime.DateOf(content.Now);
            if (from is not null && !DateOnly.TryParseExact(from, "yyyy-MM-dd", out start))
                return Results.BadRequest(new ApiProblem("Ungültiges Datum", ["from muss das Format JJJJ-MM-TT haben."]));
            var span = Math.Clamp(days ?? 7, 1, 60);
            return Results.Ok(content.Schedule(BerlinTime.StartOfDay(start), BerlinTime.StartOfDay(start.AddDays(span))));
        });
    }

    private static IResult NotFound(string message) =>
        Results.NotFound(new ApiProblem("Nicht gefunden", [message]));
}
