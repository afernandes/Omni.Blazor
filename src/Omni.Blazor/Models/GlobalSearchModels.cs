namespace Omni.Blazor.Models;

/// <summary>One searchable destination displayed by <c>OmniGlobalSearch</c>.</summary>
public sealed record GlobalSearchResult(string Id, string Title)
{
    /// <summary>Optional supporting text.</summary>
    public string? Description { get; init; }

    /// <summary>Optional visual grouping label.</summary>
    public string? Category { get; init; }

    /// <summary>Optional Omni icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>Optional application-relative or absolute destination URL.</summary>
    public string? Url { get; init; }

    /// <summary>Additional terms considered by in-memory matching.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
}

/// <summary>Immutable request passed to a global-search provider.</summary>
public sealed record GlobalSearchRequest(string Query, int MaxResults);

/// <summary>
/// Asynchronous, cancellable result source used by <c>OmniGlobalSearch</c>.
/// Implementations should honor the supplied cancellation token.
/// </summary>
public delegate ValueTask<IReadOnlyList<GlobalSearchResult>> GlobalSearchProvider(
    GlobalSearchRequest request,
    CancellationToken cancellationToken);

