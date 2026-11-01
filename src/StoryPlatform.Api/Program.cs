using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Json;
using StoryPlatform.Api;
using StoryPlatform.Api.Content;
using StoryPlatform.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.Section));
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.WriteIndented = false);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IContentStore, JsonContentStore>();
builder.Services.AddSingleton<ContentProjector>();
builder.Services.AddPublicCors(builder.Configuration);
builder.Services.AddContentOutputCache();
builder.Services.AddApiRateLimits();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // cloudflared runs on the same host and forwards to 127.0.0.1.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// Load (or seed) content at startup so a broken content.json fails fast.
_ = app.Services.GetRequiredService<IContentStore>().Current;

app.UseForwardedHeaders();
app.UsePublicCacheHeaders();
app.UseMediaLibrary();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();

app.MapGet("/", () => Results.Ok(new
{
    name = "XKStories API",
    version = "v1",
    endpoints = new[] { "/v1/channel", "/v1/featured", "/v1/series", "/v1/series/{slug}", "/v1/series/{slug}/episodes/{episode}", "/v1/schedule?from=YYYY-MM-DD&days=7", "/v1/episodes/latest", "/media/{file}", "/health" },
}));
app.MapHealthChecks("/health");
app.MapPublicEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program;
