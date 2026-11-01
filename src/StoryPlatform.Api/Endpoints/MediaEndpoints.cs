using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using StoryPlatform.Api.Content;
using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Endpoints;

public static partial class MediaEndpoints
{
    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]],
    };

    [GeneratedRegex("[^a-z0-9.-]+")]
    private static partial Regex Unsafe();

    /// <summary>Serves the media library at /media/* with a one-day browser/CDN cache.</summary>
    public static void UseMediaLibrary(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<ApiOptions>>().Value;
        Directory.CreateDirectory(options.MediaDirectory);
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/media",
            FileProvider = new PhysicalFileProvider(Path.GetFullPath(options.MediaDirectory)),
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
            ServeUnknownFileTypes = false,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=86400";
                ctx.Context.Response.Headers.AccessControlAllowOrigin = "*";
                ctx.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
            },
        });
    }

    public static async Task<IResult> UploadAsync(IFormFile file, IOptions<ApiOptions> options, ContentProjector content, CancellationToken ct)
    {
        var opts = options.Value;
        if (file.Length == 0 || file.Length > opts.MaxUploadBytes)
            return Results.BadRequest(new ApiProblem("Ungültige Datei", [$"Erlaubt sind Bilder bis {opts.MaxUploadBytes / 1024 / 1024} MB."]));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext == ".jpeg") ext = ".jpg";
        if (!Signatures.TryGetValue(ext, out var signatures))
            return Results.BadRequest(new ApiProblem("Ungültige Datei", ["Erlaubt sind .jpg, .png und .webp."]));

        await using var input = file.OpenReadStream();
        var head = new byte[4];
        var read = await input.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);
        if (!signatures.Any(sig => read >= sig.Length && head.AsSpan(0, sig.Length).SequenceEqual(sig)))
            return Results.BadRequest(new ApiProblem("Ungültige Datei", ["Der Dateiinhalt passt nicht zur Endung."]));
        input.Position = 0;

        var stem = Unsafe().Replace(Path.GetFileNameWithoutExtension(file.FileName).ToLowerInvariant(), "-").Trim('-', '.');
        if (stem.Length == 0) stem = "bild";
        var name = $"{stem}{ext}";
        for (var i = 2; File.Exists(Path.Combine(opts.MediaDirectory, name)); i++) name = $"{stem}-{i}{ext}";
        if (!ContentValidator.MediaFilePattern().IsMatch(name))
            return Results.BadRequest(new ApiProblem("Ungültiger Dateiname", [name]));

        await using (var output = File.Create(Path.Combine(opts.MediaDirectory, name)))
        {
            await input.CopyToAsync(output, ct);
        }
        return Results.Ok(new MediaUploadResult(name, content.MediaUrl(name)!));
    }
}
