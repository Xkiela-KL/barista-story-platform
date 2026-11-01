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
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    // cloudflared runs on the same host and forwards to 127.0.0.1.
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/fehler", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/nicht-gefunden", createScopeForStatusCodePages: true);
app.UseSecurityHeaders(site, app.Environment.IsDevelopment());
app.UseOutputCache();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapSeoEndpoints();
app.MapRazorComponents<App>();

app.Run();
