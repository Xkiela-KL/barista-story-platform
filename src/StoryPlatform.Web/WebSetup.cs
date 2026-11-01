using System.Text;
using System.Xml;
using Microsoft.Extensions.Options;
using StoryPlatform.Web.Services;

namespace StoryPlatform.Web;

public static class WebSetup
{
    /// <summary>Strict defaults: only own scripts, styles and fonts; images also from the API's media origin.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, SiteOptions site, bool development) =>
        app.Use(async (ctx, next) =>
        {
            var h = ctx.Response.Headers;
            var media = site.MediaOrigin.TrimEnd('/');
            h.ContentSecurityPolicy =
                $"default-src 'self'; img-src 'self' data: {media}; style-src 'self'; script-src 'self'; font-src 'self'; " +
                "connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'" +
                (development ? "" : "; upgrade-insecure-requests");
            h.XContentTypeOptions = "nosniff";
            h.XFrameOptions = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), interest-cohort=()";
            ctx.Response.OnStarting(() =>
            {
                // Short public caching for pages; Cloudflare may keep them for a minute.
                if (ctx.Response.ContentType?.StartsWith("text/html") == true && ctx.Response.StatusCode == 200)
                    h.CacheControl = "public, max-age=60, stale-while-revalidate=300";
                return Task.CompletedTask;
            });
            await next();
        });

    public static void MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/robots.txt", (IOptions<SiteOptions> options) =>
        {
            var site = options.Value;
            var body = site.AllowIndexing
                ? $"User-agent: *\nAllow: /\nSitemap: {site.PublicBaseUrl.TrimEnd('/')}/sitemap.xml\n"
                : "User-agent: *\nDisallow: /\n";
            return Results.Text(body, "text/plain", Encoding.UTF8);
        });

        app.MapGet("/sitemap.xml", async (StoryApiClient api, IOptions<SiteOptions> options, CancellationToken ct) =>
        {
            var root = options.Value.PublicBaseUrl.TrimEnd('/');
            var urls = new List<string> { "/", "/programm", "/serien", "/impressum", "/datenschutz" };
            try
            {
                foreach (var s in await api.SeriesAsync(ct) ?? [])
                {
                    urls.Add(Ui.SeriesUrl(s.Slug));
                    var detail = await api.SeriesAsync(s.Slug, ct);
                    urls.AddRange(detail?.Seasons.SelectMany(z => z.Episodes).Select(Ui.EpisodeUrl) ?? []);
                }
            }
            catch (ApiUnavailableException)
            {
                // Static pages only.
            }

            var sb = new StringBuilder();
            using (var xml = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
            {
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
                foreach (var url in urls)
                {
                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", root + url);
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            return Results.Text(sb.ToString().Replace("utf-16", "utf-8"), "application/xml", Encoding.UTF8);
        });
    }
}
