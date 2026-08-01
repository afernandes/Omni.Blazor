using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>Internal item-to-value lookup adapter used by OmniDataForm.</summary>
public partial class OmniDataFormLookupEditor<TModel, TItem, TValue>
    where TModel : class
{
    private readonly object _cacheSync = new();
    private readonly Dictionary<OmniItemsRequest, LinkedListNode<CacheEntry>> _cache = [];
    private readonly LinkedList<CacheEntry> _lru = [];
    private DataFormLookupDefinition<TModel, TItem, TValue>? _lastDefinition;
    private TModel? _lastModel;
    private long _lastDependencyVersion = long.MinValue;
    private IReadOnlyList<DataFormLookupOption<TValue>> _localOptions = [];
    private OmniItemsProvider<DataFormLookupOption<TValue>>? _provider;

    /// <summary>Current root model used by the lookup provider.</summary>
    [Parameter, EditorRequired] public TModel Model { get; set; } = default!;

    /// <summary>Immutable typed lookup definition.</summary>
    [Parameter, EditorRequired]
    public object LookupDefinition { get; set; } = default!;

    /// <summary>Version incremented whenever a declared dependency changes.</summary>
    [Parameter] public long DependencyVersion { get; set; }

    /// <summary>Placeholder shown before selection.</summary>
    [Parameter] public string? Placeholder { get; set; }

    private IReadOnlyList<DataFormLookupOption<TValue>> LocalOptions => _localOptions;
    private OmniItemsProvider<DataFormLookupOption<TValue>>? EffectiveProvider => _provider;
    private DataFormLookupDefinition<TModel, TItem, TValue> TypedDefinition
        => (DataFormLookupDefinition<TModel, TItem, TValue>)LookupDefinition;
    private string EffectiveEmptyText => TypedDefinition.EmptyText ?? "Sem opções";
    private string EffectiveLoadingText => TypedDefinition.LoadingText ?? "Carregando...";
    private string EffectiveLoadErrorText => TypedDefinition.LoadErrorText ?? "Não foi possível carregar as opções.";
    private string EffectiveRetryText => TypedDefinition.RetryText ?? "Tentar novamente";
    private string EffectiveLoadMoreText => TypedDefinition.LoadMoreText ?? "Carregar mais";

    private DataFormLookupOption<TValue>? SelectedOption
    {
        get
        {
            TValue? current = Value;
            foreach (DataFormLookupOption<TValue> option in _localOptions)
            {
                if (EqualityComparer<TValue?>.Default.Equals(option.Value, current)) return option;
            }

            return current is null
                ? null
                : new DataFormLookupOption<TValue>(current, current.ToString() ?? string.Empty);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        DataFormLookupDefinition<TModel, TItem, TValue> definition = TypedDefinition;
        if (ReferenceEquals(_lastDefinition, definition)
            && ReferenceEquals(_lastModel, Model)
            && _lastDependencyVersion == DependencyVersion)
            return;

        _lastDefinition = definition;
        _lastModel = Model;
        _lastDependencyVersion = DependencyVersion;
        ClearCache();

        if (definition.Provider is null)
        {
            DataFormLookupOption<TValue>[] options = new DataFormLookupOption<TValue>[definition.Items.Count];
            for (int index = 0; index < definition.Items.Count; index++)
            {
                TItem item = definition.Items[index];
                options[index] = CreateOption(item);
            }
            _localOptions = Array.AsReadOnly(options);
            _provider = null;
        }
        else
        {
            _localOptions = [];
            // A new delegate identity forces OmniSelect to release stale pages.
            _provider = LoadPageAsync;
        }
    }

    private Task OnSelectedAsync(DataFormLookupOption<TValue>? option)
        => SetValueAsync(option is null ? default : option.Value);

    private string OptionText(DataFormLookupOption<TValue>? option)
        => option?.Text ?? string.Empty;

    private async ValueTask<OmniItemsPage<DataFormLookupOption<TValue>>> LoadPageAsync(
        OmniItemsRequest request,
        CancellationToken cancellationToken)
    {
        if (TryGetCached(request, out OmniItemsPage<DataFormLookupOption<TValue>>? cached))
            return cached!;

        DataFormLookupDefinition<TModel, TItem, TValue> definition = TypedDefinition;
        DataFormLookupProvider<TModel, TItem> provider = definition.Provider!;
        Dictionary<string, object?> dependencies = new(definition.Dependencies.Count, StringComparer.Ordinal);
        foreach (DataFormPropertyPath dependency in definition.Dependencies)
            dependencies[dependency.Path] = dependency.GetValue(Model);

        DataFormLookupRequest<TModel> lookupRequest = new(
            Model,
            request,
            new ReadOnlyDictionary<string, object?>(dependencies));
        OmniItemsPage<TItem> page = await provider(lookupRequest, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        int count = Math.Min(page.Items.Count, Math.Min(request.Take, definition.MaxItems));
        DataFormLookupOption<TValue>[] options = new DataFormLookupOption<TValue>[count];
        for (int index = 0; index < count; index++) options[index] = CreateOption(page.Items[index]);
        OmniItemsPage<DataFormLookupOption<TValue>> result = new(
            Array.AsReadOnly(options),
            Math.Min(Math.Max(0, page.TotalCount), definition.MaxItems));
        AddCached(request, result);
        return result;
    }

    private DataFormLookupOption<TValue> CreateOption(TItem item)
        => new(TypedDefinition.ValueSelector(item), TypedDefinition.TextSelector(item));

    private bool TryGetCached(
        OmniItemsRequest request,
        out OmniItemsPage<DataFormLookupOption<TValue>>? page)
    {
        lock (_cacheSync)
        {
            if (!_cache.TryGetValue(request, out LinkedListNode<CacheEntry>? node))
            {
                page = null;
                return false;
            }

            _lru.Remove(node);
            _lru.AddFirst(node);
            page = node.Value.Page;
            return true;
        }
    }

    private void AddCached(
        OmniItemsRequest request,
        OmniItemsPage<DataFormLookupOption<TValue>> page)
    {
        int capacity = TypedDefinition.CacheEntries;
        if (capacity == 0) return;

        lock (_cacheSync)
        {
            if (_cache.Remove(request, out LinkedListNode<CacheEntry>? existing))
                _lru.Remove(existing);
            LinkedListNode<CacheEntry> node = _lru.AddFirst(new CacheEntry(request, page));
            _cache[request] = node;

            while (_cache.Count > capacity && _lru.Last is { } last)
            {
                _lru.RemoveLast();
                _cache.Remove(last.Value.Request);
            }
        }
    }

    private void ClearCache()
    {
        lock (_cacheSync)
        {
            _cache.Clear();
            _lru.Clear();
        }
    }

    private sealed record CacheEntry(
        OmniItemsRequest Request,
        OmniItemsPage<DataFormLookupOption<TValue>> Page);
}

/// <summary>Internal lookup option with value-based identity.</summary>
public sealed class DataFormLookupOption<TValue>(TValue? value, string text)
    : IEquatable<DataFormLookupOption<TValue>>
{
    public TValue? Value { get; } = value;
    public string Text { get; } = text;

    public bool Equals(DataFormLookupOption<TValue>? other)
        => other is not null
           && EqualityComparer<TValue?>.Default.Equals(Value, other.Value);

    public override bool Equals(object? obj) => Equals(obj as DataFormLookupOption<TValue>);

    public override int GetHashCode() => Value is null ? 0 : EqualityComparer<TValue?>.Default.GetHashCode(Value);
}
