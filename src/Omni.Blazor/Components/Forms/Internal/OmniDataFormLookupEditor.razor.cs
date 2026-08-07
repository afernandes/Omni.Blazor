using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>Internal item-to-value lookup adapter used by OmniDataForm.</summary>
public partial class OmniDataFormLookupEditor<TModel, TItem, TValue>
    where TModel : class
{
    private readonly object _cacheSync = new();
    private readonly object _resolveSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<OmniItemsRequest, LinkedListNode<CacheEntry>> _cache = [];
    private readonly LinkedList<CacheEntry> _lru = [];
    private CancellationTokenSource? _resolveOperation;
    private DataFormLookupDefinition<TModel, TItem, TValue>? _lastDefinition;
    private TModel? _lastModel;
    private long _lastDependencyVersion = long.MinValue;
    private IReadOnlyList<DataFormLookupOption<TValue>> _localOptions = [];
    private OmniItemsProvider<DataFormLookupOption<TValue>>? _provider;
    private DataFormLookupOption<TValue>? _displayOption;
    private TValue? _lastSynchronizedValue;
    private bool _hasSynchronizedValue;
    private long _resolveVersion;
    private int _lookupDisposeState;

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
    private string EffectiveUnresolvedText => Placeholder ?? "Opção selecionada";
    private bool IsLookupDisposed => Volatile.Read(ref _lookupDisposeState) != 0;

    private DataFormLookupOption<TValue>? SelectedOption
    {
        get
        {
            TValue? current = Value;
            foreach (DataFormLookupOption<TValue> option in _localOptions)
            {
                if (EqualityComparer<TValue?>.Default.Equals(option.Value, current)) return option;
            }

            lock (_resolveSync)
            {
                return _displayOption is not null
                       && EqualityComparer<TValue?>.Default.Equals(_displayOption.Value, current)
                    ? _displayOption
                    : null;
            }
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        DataFormLookupDefinition<TModel, TItem, TValue> definition = TypedDefinition;
        bool sourceChanged = !ReferenceEquals(_lastDefinition, definition)
            || !ReferenceEquals(_lastModel, Model)
            || _lastDependencyVersion != DependencyVersion;
        if (sourceChanged)
        {
            _lastDefinition = definition;
            _lastModel = Model;
            _lastDependencyVersion = DependencyVersion;
            _hasSynchronizedValue = false;
            CancelResolveOperation();
            SetDisplayOption(null);
            ClearCache();

            if (definition.Provider is null)
            {
                DataFormLookupOption<TValue>[] options = new DataFormLookupOption<TValue>[definition.Items.Count];
                for (int index = 0; index < definition.Items.Count; index++)
                {
                    TItem item = definition.Items[index];
                    options[index] = CreateOption(definition, item);
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

        SynchronizeSelectedOption(definition);
    }

    private async Task OnSelectedAsync(DataFormLookupOption<TValue>? option)
    {
        CancelResolveOperation();
        SetDisplayOption(option);
        _lastSynchronizedValue = option is null ? default : option.Value;
        _hasSynchronizedValue = true;
        await SetValueAsync(option is null ? default : option.Value);
    }

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
        DataFormLookupRequest<TModel> lookupRequest = new(
            Model,
            request,
            CreateDependencySnapshot(definition, Model));
        OmniItemsPage<TItem> page = await provider(lookupRequest, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        int count = Math.Min(page.Items.Count, Math.Min(request.Take, definition.MaxItems));
        DataFormLookupOption<TValue>[] options = new DataFormLookupOption<TValue>[count];
        for (int index = 0; index < count; index++) options[index] = CreateOption(definition, page.Items[index]);
        OmniItemsPage<DataFormLookupOption<TValue>> result = new(
            Array.AsReadOnly(options),
            Math.Min(Math.Max(0, page.TotalCount), definition.MaxItems));
        AddCached(request, result);
        return result;
    }

    private void SynchronizeSelectedOption(DataFormLookupDefinition<TModel, TItem, TValue> definition)
    {
        TValue? current = Value;
        bool sameValue = _hasSynchronizedValue
            && EqualityComparer<TValue?>.Default.Equals(_lastSynchronizedValue, current);
        if (sameValue) return;

        _hasSynchronizedValue = true;
        _lastSynchronizedValue = current;
        CancelResolveOperation();

        if (current is null)
        {
            SetDisplayOption(null);
            return;
        }

        foreach (DataFormLookupOption<TValue> option in _localOptions)
        {
            if (!EqualityComparer<TValue?>.Default.Equals(option.Value, current)) continue;
            SetDisplayOption(option);
            return;
        }

        if (definition.Resolver is null)
        {
            SetDisplayOption(new DataFormLookupOption<TValue>(current, EffectiveUnresolvedText));
            return;
        }

        StartResolve(definition, current);
    }

    private void StartResolve(
        DataFormLookupDefinition<TModel, TItem, TValue> definition,
        TValue? value)
    {
        DataFormLookupResolveRequest<TModel, TValue> request = new(
            Model,
            value,
            CreateDependencySnapshot(definition, Model));
        CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationTokenSource? previous;
        long version;
        lock (_resolveSync)
        {
            previous = _resolveOperation;
            _resolveOperation = operation;
            version = ++_resolveVersion;
            _displayOption = new DataFormLookupOption<TValue>(value, EffectiveLoadingText);
        }
        CancelAndDispose(previous);
        ObserveTask(
            ResolveSelectedItemAsync(definition, request, operation, version),
            "OmniDataFormLookupEditor.ResolveItem");
    }

    private async Task ResolveSelectedItemAsync(
        DataFormLookupDefinition<TModel, TItem, TValue> definition,
        DataFormLookupResolveRequest<TModel, TValue> request,
        CancellationTokenSource operation,
        long version)
    {
        try
        {
            TItem? item = await definition.Resolver!(request, operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            DataFormLookupOption<TValue> option = item is null
                ? new DataFormLookupOption<TValue>(request.Value, EffectiveUnresolvedText)
                : CreateOption(definition, item);
            if (!EqualityComparer<TValue?>.Default.Equals(option.Value, request.Value))
            {
                throw new InvalidOperationException(
                    "The DataForm lookup resolver returned an item with a different value.");
            }

            lock (_resolveSync)
            {
                if (ReferenceEquals(_resolveOperation, operation) && _resolveVersion == version)
                    _displayOption = option;
            }
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch
        {
            lock (_resolveSync)
            {
                if (ReferenceEquals(_resolveOperation, operation) && _resolveVersion == version)
                    _displayOption = new DataFormLookupOption<TValue>(request.Value, EffectiveUnresolvedText);
            }
            throw;
        }
        finally
        {
            bool render = false;
            lock (_resolveSync)
            {
                if (ReferenceEquals(_resolveOperation, operation))
                {
                    _resolveOperation = null;
                    render = true;
                }
            }
            operation.Dispose();
            if (render && !IsLookupDisposed)
                await InvokeAsync(StateHasChanged);
        }
    }

    private static DataFormLookupOption<TValue> CreateOption(
        DataFormLookupDefinition<TModel, TItem, TValue> definition,
        TItem item)
        => new(definition.ValueSelector(item), definition.TextSelector(item));

    private static IReadOnlyDictionary<string, object?> CreateDependencySnapshot(
        DataFormLookupDefinition<TModel, TItem, TValue> definition,
        TModel model)
    {
        Dictionary<string, object?> dependencies = new(definition.Dependencies.Count, StringComparer.Ordinal);
        foreach (DataFormPropertyPath dependency in definition.Dependencies)
            dependencies[dependency.Path] = dependency.GetValue(model);
        return new ReadOnlyDictionary<string, object?>(dependencies);
    }

    private void SetDisplayOption(DataFormLookupOption<TValue>? option)
    {
        lock (_resolveSync) _displayOption = option;
    }

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

    private void CancelResolveOperation()
    {
        CancellationTokenSource? operation;
        lock (_resolveSync)
        {
            operation = _resolveOperation;
            _resolveOperation = null;
            ++_resolveVersion;
        }
        CancelAndDispose(operation);
    }

    private static void CancelAndDispose(CancellationTokenSource? operation)
    {
        if (operation is null) return;
        try { operation.Cancel(); }
        catch (ObjectDisposedException) { }
        operation.Dispose();
    }

    /// <summary>Cancels item resolution and releases retained lookup state.</summary>
    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _lookupDisposeState, 1) != 0) return;
        _lifetime.Cancel();
        CancelResolveOperation();
        _lifetime.Dispose();
        ClearCache();
        base.Dispose();
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
