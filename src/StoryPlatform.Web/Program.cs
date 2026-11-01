using Microsoft.AspNetCore.HttpOverrides;
using StoryPlatform.Web;
using StoryPlatform.Web.Components;
using StoryPlatform.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var site = builder.Configuration.GetSection(SiteOptions.Section).Get<SiteOptions>() ?? new SiteOptions();
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.Section));

// Static server rendering only: every page is plain HTML, no WebSocket, no WebAssembly.
builder.Services.AddRazorComponents();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<StoryApiClient>(client =>
{
    client.BaseAddress = new Uri(site.ApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddOutputCache(o => o.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(30))));
builder.Services.AddHealthChecks();
// Anonymous page-view totals (same format as the Portfolio), read on demand by XKWorkstation.
builder.Services.Configure<VisitCounterOptions>(builder.Configuration.GetSection(VisitCounterOptions.Section));
builder.Services.AddSingleton<VisitCounter>();
builder.Services.AddHostedService<VisitCounterFlusher>();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // cloudflared runs on the same host and forwards to 127.0.0.1.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHttpsRedirect();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/fehler", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/nicht-gefunden", createScopeForStatusCodePages: true);
app.UseSecurityHeaders(site, app.Environment.IsDevelopment());
app.Services.GetRequiredService<VisitCounter>();

// Count successfully rendered HTML pages. Static assets, errors and 404s are not page views.
// Sits in front of the output cache so cached pages are counted too.
app.Use(async (context, next) =>
{
    await next();
    var response = context.Response;
    if (HttpMethods.IsGet(context.Request.Method)
        && response.StatusCode == StatusCodes.Status200OK
        && response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true
        && !VisitCounter.IsAutomatedAgent(context.Request.Headers.UserAgent))
    {
        context.RequestServices.GetRequiredService<VisitCounter>().RecordPageView(context.Request.Path);
    }
});
app.UseOutputCache();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapSeoEndpoints();
app.MapRazorComponents<App>();

app.Run();
