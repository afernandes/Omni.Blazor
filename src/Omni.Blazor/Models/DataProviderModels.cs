namespace Omni.Blazor.Models;

/// <summary>Bounded request issued by an async option source.</summary>
/// <param name="Search">Optional search term already trimmed by the component.</param>
/// <param name="Skip">Zero-based number of matching items to skip.</param>
/// <param name="Take">Maximum number of items requested.</param>
public readonly record struct OmniItemsRequest(string? Search, int Skip, int Take);

/// <summary>One bounded page returned by an async option source.</summary>
/// <param name="Items">Requested page. Providers should never return more than <c>Take</c>.</param>
/// <param name="TotalCount">Total matching item count before paging.</param>
public sealed record OmniItemsPage<TItem>(IReadOnlyList<TItem> Items, int TotalCount);

/// <summary>
/// Asynchronous, cancellable option source used by selection components.
/// </summary>
public delegate ValueTask<OmniItemsPage<TItem>> OmniItemsProvider<TItem>(
    OmniItemsRequest request,
    CancellationToken cancellationToken);
