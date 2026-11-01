using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using StoryPlatform.Api.Endpoints;

namespace StoryPlatform.Api;

public static class CorsSetup
{
    public const string PublicPolicy = "public-read";

    public static IServiceCollection AddPublicCors(this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection($"{ApiOptions.Section}:CorsOrigins").Get<string[]>() ?? [];
        return services.AddCors(o => o.AddPolicy(PublicPolicy, p =>
        {
            // Only GET from the listed origins (e.g. https://tv.xkiela.dev). The website itself calls the API
            // server-side, so this matters only for future browser or app clients.
            if (origins.Length > 0) p.WithOrigins(origins).WithMethods("GET").WithHeaders("Accept");
        }));
    }
}

public static class OutputCacheSetup
{
    public const string ContentTag = "content";

    public static IServiceCollection AddContentOutputCache(this IServiceCollection services) =>
        services.AddOutputCache(o => o.AddPolicy(PublicEndpoints.CachePolicy, b => b
            .Expire(TimeSpan.FromSeconds(60))
            .SetVaryByQuery("from", "days", "limit")
            .Tag(ContentTag)));
}

public static class RateLimits
{
    public const string Admin = "admin";

    public static IServiceCollection AddApiRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(Admin, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
        });
}

public static class CacheHeaders
{
    /// <summary>Lets Cloudflare and browsers cache public GET responses for a minute.</summary>
    public static IApplicationBuilder UsePublicCacheHeaders(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        ctx.Response.OnStarting(() =>
        {
            var path = ctx.Request.Path;
            if (HttpMethods.IsGet(ctx.Request.Method) && path.StartsWithSegments("/v1") && !path.StartsWithSegments("/v1/admin")
                && ctx.Response.StatusCode == StatusCodes.Status200OK)
                ctx.Response.Headers.CacheControl = "public, max-age=60, stale-while-revalidate=300";
            else if (path.StartsWithSegments("/v1/admin"))
                ctx.Response.Headers.CacheControl = "no-store";
            ctx.Response.Headers.XContentTypeOptions = "nosniff";
            return Task.CompletedTask;
        });
        await next();
    });
}
