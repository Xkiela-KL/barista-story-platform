using System.Text.Json;
using Microsoft.Extensions.Options;
using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Content;

/// <summary>Keeps content.json in memory and writes changes atomically (temp file + rename).</summary>
public sealed class JsonContentStore : IContentStore
{
    private readonly ApiOptions _options;
    private readonly ILogger<JsonContentStore> _log;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private ContentDocument _current;

    public JsonContentStore(IOptions<ApiOptions> options, TimeProvider clock, ILogger<JsonContentStore> log)
    {
        _options = options.Value;
        _log = log;
        Directory.CreateDirectory(_options.DataDirectory);
        Directory.CreateDirectory(_options.MediaDirectory);
        _current = LoadOrSeed(clock);
    }

    public ContentDocument Current => Volatile.Read(ref _current);

    public async Task<ContentDocument> UpdateAsync(Func<ContentDocument, ContentDocument> change, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var next = change(Current);
            var errors = ContentValidator.Validate(next);
            if (errors.Count > 0) throw new ContentValidationException(errors);
            await WriteAsync(next, ct);
            Volatile.Write(ref _current, next);
            return next;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private ContentDocument LoadOrSeed(TimeProvider clock)
    {
        if (File.Exists(_options.ContentFile))
        {
            using var stream = File.OpenRead(_options.ContentFile);
            var doc = JsonSerializer.Deserialize<ContentDocument>(stream, ContentJson.Options)
                      ?? throw new InvalidOperationException($"{_options.ContentFile} ist leer.");
            var errors = ContentValidator.Validate(doc);
            if (errors.Count > 0)
                throw new InvalidOperationException($"{_options.ContentFile} ist ungültig: {string.Join(" ", errors)}");
            _log.LogInformation("Inhalte geladen: {Series} Serien aus {File}", doc.Series.Count, _options.ContentFile);
            return doc;
        }

        var seed = _options.SeedPlaceholderContent
            ? PlaceholderSeed.Create(clock.GetUtcNow())
            : new ContentDocument();
        if (_options.SeedPlaceholderContent) PlaceholderSeed.CopyMedia(_options.MediaDirectory, _log);
        WriteAsync(seed, CancellationToken.None).GetAwaiter().GetResult();
        _log.LogInformation("Neue content.json angelegt ({Mode})", _options.SeedPlaceholderContent ? "mit Beispielinhalten" : "leer");
        return seed;
    }

    private async Task WriteAsync(ContentDocument doc, CancellationToken ct)
    {
        var temp = _options.ContentFile + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, doc, ContentJson.Options, ct);
        }
        File.Move(temp, _options.ContentFile, overwrite: true);
    }
}

public static class ContentJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
