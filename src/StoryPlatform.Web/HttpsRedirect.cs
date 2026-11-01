namespace StoryPlatform.Web;

public static class HttpsRedirect
{
    /// <summary>
    /// Behind Cloudflare Tunnel the app only speaks HTTP; Cloudflare reports the visitor's scheme in X-Forwarded-Proto.
    /// Visitors who arrive via http:// get a 308 to https://. Requests from the host itself (health checks) are left alone.
    /// </summary>
    public static IApplicationBuilder UseForwardedHttpsRedirect(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var forwarded = ctx.Request.Headers["X-Forwarded-Proto"].ToString();
        var host = ctx.Request.Host.Host;
        if (forwarded.Equals("http", StringComparison.OrdinalIgnoreCase) && host is not ("localhost" or "127.0.0.1"))
        {
            ctx.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
            ctx.Response.Headers.Location = $"https://{ctx.Request.Host}{ctx.Request.PathBase}{ctx.Request.Path}{ctx.Request.QueryString}";
            return;
        }
        await next();
    });
}
