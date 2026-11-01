using StoryPlatform.Contracts;

namespace StoryPlatform.Api.Content;

/// <summary>
/// Single source of truth for all programme content. Version 1 keeps one JSON document on disk;
/// a database implementation (EF Core + PostgreSQL) can replace it without touching the endpoints.
/// </summary>
public interface IContentStore
{
    /// <summary>Current, immutable snapshot. Cheap to call on every request.</summary>
    ContentDocument Current { get; }

    /// <summary>Validates and persists a change. Throws <see cref="ContentValidationException"/> on invalid content.</summary>
    Task<ContentDocument> UpdateAsync(Func<ContentDocument, ContentDocument> change, CancellationToken ct = default);
}

public sealed class ContentValidationException(IReadOnlyList<string> errors)
    : Exception("Die Inhalte sind ungültig: " + string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
