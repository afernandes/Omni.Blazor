using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Context supplied to a model-aware DataForm lookup provider.</summary>
public sealed record DataFormLookupRequest<TModel>(
    TModel Model,
    OmniItemsRequest ItemsRequest,
    IReadOnlyDictionary<string, object?> Dependencies)
    where TModel : class;

/// <summary>Model-aware, cancellable and paged DataForm lookup provider.</summary>
public delegate ValueTask<OmniItemsPage<TItem>> DataFormLookupProvider<TModel, TItem>(
    DataFormLookupRequest<TModel> request,
    CancellationToken cancellationToken)
    where TModel : class;

/// <summary>Context supplied when resolving the item represented by a lookup value.</summary>
public sealed record DataFormLookupResolveRequest<TModel, TValue>(
    TModel Model,
    TValue? Value,
    IReadOnlyDictionary<string, object?> Dependencies)
    where TModel : class;

/// <summary>Resolves a lookup item from an initial or externally supplied bound value.</summary>
public delegate ValueTask<TItem?> DataFormLookupResolver<TModel, TItem, TValue>(
    DataFormLookupResolveRequest<TModel, TValue> request,
    CancellationToken cancellationToken)
    where TModel : class;

/// <summary>Context rendered when an async items provider fails.</summary>
public sealed record OmniItemsProviderErrorContext(
    Exception Exception,
    Func<Task> RetryAsync);

/// <summary>Strongly typed options for a DataForm lookup whose item and value types differ.</summary>
public sealed class DataFormLookupEditorBuilder<TModel, TItem, TValue>
    where TModel : class
{
    private readonly Action _ensureMutable;
    private readonly Func<TItem, TValue> _valueSelector;
    private readonly Func<TItem, string> _textSelector;
    private IReadOnlyList<TItem> _items = [];
    private DataFormLookupProvider<TModel, TItem>? _provider;
    private DataFormLookupResolver<TModel, TItem, TValue>? _resolver;
    private readonly List<DataFormPropertyPath> _dependencies = [];
    private bool _clearable;
    private bool _clearValueOnDependencyChange = true;
    private int _pageSize = 50;
    private int _maxItems = 500;
    private int _cacheEntries = 8;
    private string? _emptyText;
    private string? _loadingText;
    private string? _loadErrorText;
    private string? _retryText;
    private string? _loadMoreText;
    private RenderFragment? _emptyTemplate;
    private RenderFragment? _loadingTemplate;
    private RenderFragment<OmniItemsProviderErrorContext>? _errorTemplate;

    internal DataFormLookupEditorBuilder(
        Func<TItem, TValue> valueSelector,
        Func<TItem, string> textSelector,
        Action ensureMutable)
    {
        _valueSelector = valueSelector;
        _textSelector = textSelector;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Uses bounded in-memory items.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> Items(
        IEnumerable<TItem> items,
        int maxItems = 10_000)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(items);
        if (maxItems is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(maxItems));
        TItem[] materialized = items.Take(maxItems + 1).ToArray();
        if (materialized.Length > maxItems)
            throw new ArgumentException($"Lookup items exceed the configured limit of {maxItems}.", nameof(items));
        _items = Array.AsReadOnly(materialized);
        _provider = null;
        _maxItems = maxItems;
        return this;
    }

    /// <summary>Uses a model-aware, cancellable and paged provider.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> ItemsProvider(
        DataFormLookupProvider<TModel, TItem> provider,
        int pageSize = 50,
        int maxItems = 500)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(provider);
        if (pageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(pageSize));
        if (maxItems is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(maxItems));
        _provider = provider;
        _items = [];
        _pageSize = pageSize;
        _maxItems = maxItems;
        return this;
    }

    /// <summary>
    /// Resolves the item represented by an initial or externally supplied value,
    /// allowing the editor to display human-readable text before opening its list.
    /// </summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> ResolveItem(
        DataFormLookupResolver<TModel, TItem, TValue> resolver)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
        return this;
    }

    /// <summary>
    /// Invalidates provider items when another model property changes. The
    /// dependent value is cleared by default.
    /// </summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> DependsOn<TDependency>(
        Expression<Func<TModel, TDependency>> property)
    {
        _ensureMutable();
        DataFormPropertyPath path = DataFormSchemaBuilder<TModel>.ResolveProperty(property);
        if (_dependencies.All(existing => !StringComparer.Ordinal.Equals(existing.Path, path.Path)))
            _dependencies.Add(path);
        return this;
    }

    /// <summary>Controls whether a dependency change clears the bound value.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> ClearValueOnDependencyChange(bool value = true)
    {
        _ensureMutable();
        _clearValueOnDependencyChange = value;
        return this;
    }

    /// <summary>Shows or hides the clear action.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> Clearable(bool value = true)
    {
        _ensureMutable();
        _clearable = value;
        return this;
    }

    /// <summary>Sets the maximum LRU provider pages retained by one editor instance.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> CacheEntries(int value)
    {
        _ensureMutable();
        if (value is < 0 or > 64) throw new ArgumentOutOfRangeException(nameof(value));
        _cacheEntries = value;
        return this;
    }

    /// <summary>Sets localized provider status text.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> Texts(
        string? empty = null,
        string? loading = null,
        string? loadError = null,
        string? retry = null,
        string? loadMore = null)
    {
        _ensureMutable();
        _emptyText = empty;
        _loadingText = loading;
        _loadErrorText = loadError;
        _retryText = retry;
        _loadMoreText = loadMore;
        return this;
    }

    /// <summary>Sets loading, empty and retry-capable error templates.</summary>
    public DataFormLookupEditorBuilder<TModel, TItem, TValue> Templates(
        RenderFragment? loading = null,
        RenderFragment? empty = null,
        RenderFragment<OmniItemsProviderErrorContext>? error = null)
    {
        _ensureMutable();
        _loadingTemplate = loading;
        _emptyTemplate = empty;
        _errorTemplate = error;
        return this;
    }

    internal DataFormLookupDefinition<TModel, TItem, TValue> Build()
        => new(
            _valueSelector,
            _textSelector,
            _items,
            _provider,
            _resolver,
            Array.AsReadOnly(_dependencies.ToArray()),
            _clearable,
            _clearValueOnDependencyChange,
            _pageSize,
            _maxItems,
            _cacheEntries,
            _emptyText,
            _loadingText,
            _loadErrorText,
            _retryText,
            _loadMoreText,
            _emptyTemplate,
            _loadingTemplate,
            _errorTemplate);
}

internal interface IDataFormLookupDefinition<TModel> where TModel : class
{
    Type EditorType { get; }
    IReadOnlyList<DataFormPropertyPath> Dependencies { get; }
    bool ClearValueOnDependencyChange { get; }
}

// The editor is chosen by Type at runtime, so nothing references the closed generic
// statically and a trimmed WebAssembly publish drops the members Blazor activates it with.
// Rooting hangs off the primary constructor so it costs nothing until a consumer actually
// declares a lookup field. See ServiceCollectionExtensions for why it is not central.
[method: System.Diagnostics.CodeAnalysis.DynamicDependency(
    System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors
        | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties
        | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties,
    typeof(Omni.Blazor.Components.OmniDataFormLookupEditor<,,>))]
internal sealed record DataFormLookupDefinition<TModel, TItem, TValue>(
    Func<TItem, TValue> ValueSelector,
    Func<TItem, string> TextSelector,
    IReadOnlyList<TItem> Items,
    DataFormLookupProvider<TModel, TItem>? Provider,
    DataFormLookupResolver<TModel, TItem, TValue>? Resolver,
    IReadOnlyList<DataFormPropertyPath> Dependencies,
    bool Clearable,
    bool ClearValueOnDependencyChange,
    int PageSize,
    int MaxItems,
    int CacheEntries,
    string? EmptyText,
    string? LoadingText,
    string? LoadErrorText,
    string? RetryText,
    string? LoadMoreText,
    RenderFragment? EmptyTemplate,
    RenderFragment? LoadingTemplate,
    RenderFragment<OmniItemsProviderErrorContext>? ErrorTemplate)
    : IDataFormLookupDefinition<TModel>
    where TModel : class
{
    public Type EditorType { get; } = typeof(Omni.Blazor.Components.OmniDataFormLookupEditor<TModel, TItem, TValue>);
}
