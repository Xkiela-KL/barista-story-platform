using System.Text.Json;
using Microsoft.Extensions.Options;

namespace StoryPlatform.Web.Services;

/// Aggregated, anonymous visit counts. No IP addresses, cookies or per-visitor identifiers are
/// stored — only totals per day and per page, so the file can be read without privacy concerns.
public sealed class VisitStatistics
{
    public DateTimeOffset? UpdatedUtc { get; set; }

    public long PageViews { get; set; }

    public long QrScans { get; set; }

    public SortedDictionary<string, DailyVisits> Days { get; set; } = new();
}

public sealed class DailyVisits
{
    public long PageViews { get; set; }

    public long QrScans { get; set; }

    public SortedDictionary<string, long> Paths { get; set; } = new();
}

/// Counts page views and QR scans in memory and persists them as JSON.
/// <see cref="VisitCounterFlusher"/> writes pending changes periodically and on shutdown.
public sealed class VisitCounter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    // Crawlers, uptime monitors and link-preview fetchers are not visitors.
    private static readonly string[] AutomatedAgentMarkers =
    [
        "bot", "crawl", "spider", "slurp", "facebookexternalhit", "whatsapp", "preview",
        "curl", "wget", "python", "httpclient", "okhttp", "go-http", "headless", "monitor", "uptime",
    ];

    private const int MaxPathLength = 200;

    private readonly object _sync = new();
    private readonly object _fileSync = new();
    private readonly string _path;
    private readonly ILogger<VisitCounter> _logger;
    private readonly VisitStatistics _stats;
    private bool _dirty;

    public VisitCounter(IOptions<VisitCounterOptions> options, IHostEnvironment environment, ILogger<VisitCounter> logger)
    {
        _logger = logger;
        _path = Path.GetFullPath(options.Value.DataPath, environment.ContentRootPath);

        // Fail at startup if the data directory is not writable, not on the first flush.
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        _stats = Load();
        _logger.LogInformation("Visit statistics at {Path}: {PageViews} page views, {QrScans} QR scans",
            _path, _stats.PageViews, _stats.QrScans);
    }

    public static bool IsAutomatedAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return true;
        }

        return AutomatedAgentMarkers.Any(marker => userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public void RecordPageView(PathString requestPath)
    {
        var path = NormalizePath(requestPath);

        lock (_sync)
        {
            var day = Today();
            _stats.PageViews++;
            day.PageViews++;
            day.Paths[path] = day.Paths.GetValueOrDefault(path) + 1;
            _dirty = true;
        }
    }

    public long RecordQrScan()
    {
        lock (_sync)
        {
            _stats.QrScans++;
            Today().QrScans++;
            _dirty = true;
            return _stats.QrScans;
        }
    }

    public void Flush()
    {
        string json;

        lock (_sync)
        {
            if (!_dirty)
            {
                return;
            }

            _stats.UpdatedUtc = DateTimeOffset.UtcNow;
            json = JsonSerializer.Serialize(_stats, JsonOptions);
            _dirty = false;
        }

        try
        {
            lock (_fileSync)
            {
                // Write to a temporary file first so a crash never leaves a half-written file behind.
                var temporaryPath = _path + ".tmp";
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, _path, overwrite: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            lock (_sync)
            {
                _dirty = true;
            }

            _logger.LogError(exception, "Could not write visit statistics to {Path}", _path);
        }
    }

    private DailyVisits Today()
    {
        var key = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (!_stats.Days.TryGetValue(key, out var day))
        {
            day = new DailyVisits();
            _stats.Days[key] = day;
        }

        return day;
    }

    private static string NormalizePath(PathString requestPath)
    {
        var path = requestPath.Value;
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return "/";
        }

        path = path.TrimEnd('/').ToLowerInvariant();
        return path.Length > MaxPathLength ? path[..MaxPathLength] : path;
    }

    private VisitStatistics Load()
    {
        if (!File.Exists(_path))
        {
            return new VisitStatistics();
        }

        try
        {
            var stats = JsonSerializer.Deserialize<VisitStatistics>(File.ReadAllText(_path), JsonOptions);
            if (stats is not null)
            {
                return stats;
            }
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Visit statistics at {Path} are unreadable", _path);
        }

        // Keep the broken file for inspection instead of silently overwriting it.
        var backupPath = $"{_path}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
        File.Move(_path, backupPath);
        _logger.LogWarning("Moved unreadable visit statistics to {BackupPath} and started from zero", backupPath);
        return new VisitStatistics();
    }
}

/// Periodically persists <see cref="VisitCounter"/> and writes a final time on shutdown.
public sealed class VisitCounterFlusher(VisitCounter counter, IOptions<VisitCounterOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.FlushIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                counter.Flush();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown; StopAsync writes the remaining counts.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        counter.Flush();
    }
}
