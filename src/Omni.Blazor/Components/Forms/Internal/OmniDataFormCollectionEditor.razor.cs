using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>Internal bounded collection and embedded-subform editor.</summary>
public partial class OmniDataFormCollectionEditor<TModel, TCollection, TItem>
    where TModel : class
    where TItem : class
{
    private readonly List<ItemRuntime> _items = [];
    private EditContext? _subscribedParent;
    private ValidationMessageStore? _parentStore;
    private TCollection? _workingValue;
    private TCollection? _factoryValue;
    private bool _validatingParent;
    private bool _initializedFromFactory;
    private int _disposeState;

    [CascadingParameter] private EditContext? ParentEditContext { get; set; }

    /// <summary>Root model owning the collection property.</summary>
    [Parameter, EditorRequired] public TModel Model { get; set; } = default!;

    /// <summary>Immutable collection definition supplied by the schema.</summary>
    [Parameter, EditorRequired] public object CollectionDefinition { get; set; } = default!;

    /// <summary>Current collection value.</summary>
    [Parameter] public TCollection? Value { get; set; }

    /// <summary>Writes a collection mutation through the generated binding.</summary>
    [Parameter] public EventCallback<TCollection?> ValueChanged { get; set; }

    /// <summary>Field identifier used by the parent EditContext.</summary>
    [Parameter] public FieldIdentifier FieldIdentifier { get; set; }

    /// <summary>Full collection property path.</summary>
    [Parameter, EditorRequired] public string PropertyPath { get; set; } = string.Empty;

    /// <summary>Stable collection container id.</summary>
    [Parameter, EditorRequired] public string FieldId { get; set; } = string.Empty;

    /// <summary>Effective field label.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Disables collection and embedded item editors.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Makes collection and embedded item editors read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    private DataFormCollectionDefinition<TModel, TCollection, TItem> TypedDefinition
        => (DataFormCollectionDefinition<TModel, TCollection, TItem>)CollectionDefinition;
    private string AddText => TypedDefinition.AddText ?? Texts.Add;
    private string RemoveText => TypedDefinition.RemoveText ?? Texts.Remove;
    private string MoveUpText => TypedDefinition.MoveUpText ?? Texts.MoveUp;
    private string MoveDownText => TypedDefinition.MoveDownText ?? Texts.MoveDown;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Value is not null)
        {
            _factoryValue = default;
            _workingValue = Value;
        }
        else
        {
            _workingValue = _factoryValue;
        }

        if (_workingValue is null
            && !_initializedFromFactory
            && TypedDefinition.CollectionFactory is not null)
        {
            _factoryValue = TypedDefinition.CollectionFactory()
                ?? throw new InvalidOperationException(
                    $"The collection factory for '{PropertyPath}' returned null.");
            _workingValue = _factoryValue;
            _initializedFromFactory = true;
            ObserveTask(
                InvokeAsync(PublishFactoryValueAsync),
                "OmniDataFormCollection.Initialize");
        }
        if (_workingValue is null && TypedDefinition.ItemFactory is not null)
        {
            throw new InvalidOperationException(
                $"DataForm collection '{PropertyPath}' is null. Configure CreateCollection before enabling item creation.");
        }

        SynchronizeItems();
    }

    private Task PublishFactoryValueAsync()
        => Volatile.Read(ref _disposeState) != 0
            ? Task.CompletedTask
            : ValueChanged.InvokeAsync(_workingValue);

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (Volatile.Read(ref _disposeState) != 0) return;
        if (ReferenceEquals(_subscribedParent, ParentEditContext)) return;
        DetachParent();
        _subscribedParent = ParentEditContext;
        if (_subscribedParent is not null)
        {
            _parentStore = new ValidationMessageStore(_subscribedParent);
            _subscribedParent.OnValidationRequested += OnParentValidationRequested;
        }
    }

    private IList<TItem> GetCollection()
        => _workingValue is IList<TItem> list
            ? list
            : throw new InvalidOperationException(
                $"DataForm collection '{PropertyPath}' must be initialized and implement IList<{typeof(TItem).Name}>.");

    private bool CollectionIsReadOnly
        => _workingValue is not IList<TItem> { IsReadOnly: false };

    private async Task AddAsync()
    {
        if (Disabled || ReadOnly || TypedDefinition.ItemFactory is null) return;
        IList<TItem> collection = GetCollection();
        if (collection.IsReadOnly) return;
        if (collection.Count >= TypedDefinition.MaximumItems) return;
        TItem item = TypedDefinition.ItemFactory()
            ?? throw new InvalidOperationException($"The item factory for '{PropertyPath}' returned null.");
        collection.Add(item);
        SynchronizeItems();
        await NotifyCollectionChangedAsync();
    }

    private async Task RemoveAsync(int index)
    {
        if (Disabled || ReadOnly || !TypedDefinition.AllowRemove) return;
        IList<TItem> collection = GetCollection();
        if (collection.IsReadOnly) return;
        if ((uint)index >= (uint)collection.Count || collection.Count <= TypedDefinition.MinimumItems) return;
        collection.RemoveAt(index);
        SynchronizeItems();
        await NotifyCollectionChangedAsync();
    }

    private async Task MoveAsync(int index, int offset)
    {
        if (Disabled || ReadOnly || !TypedDefinition.AllowReorder) return;
        IList<TItem> collection = GetCollection();
        if (collection.IsReadOnly) return;
        int target = index + offset;
        if ((uint)index >= (uint)collection.Count || (uint)target >= (uint)collection.Count) return;
        TItem item = collection[index];
        collection.RemoveAt(index);
        collection.Insert(target, item);
        SynchronizeItems();
        await NotifyCollectionChangedAsync();
    }

    private async Task OnItemChangedAsync(
        ItemRuntime runtime,
        DataFormFieldChangedEventArgs<TItem> args)
    {
        if (!_items.Contains(runtime)) return;
        await NotifyCollectionChangedAsync();
    }

    private async Task NotifyCollectionChangedAsync()
    {
        if (ValueChanged.HasDelegate) await ValueChanged.InvokeAsync(_workingValue);
        ParentEditContext?.NotifyFieldChanged(FieldIdentifier);
        RefreshParentValidation(validateChildren: false);
    }

    private void SynchronizeItems()
    {
        IList<TItem>? collection = _workingValue as IList<TItem>;
        if (collection is null)
        {
            ReleaseItems([]);
            return;
        }
        if (collection.Count > TypedDefinition.MaximumItems)
            throw new InvalidOperationException(
                $"DataForm collection '{PropertyPath}' contains {collection.Count} items; the configured maximum is {TypedDefinition.MaximumItems}.");

        List<ItemRuntime> next = new(collection.Count);
        HashSet<object> keys = [];
        for (int index = 0; index < collection.Count; index++)
        {
            TItem item = collection[index] ?? throw new InvalidOperationException(
                $"DataForm collection '{PropertyPath}' contains a null item at index {index}.");
            ItemRuntime? runtime;
            object key;
            if (TypedDefinition.KeySelector is null)
            {
                runtime = _items.FirstOrDefault(existing => ReferenceEquals(existing.Item, item));
                key = runtime?.Key ?? new object();
            }
            else
            {
                key = TypedDefinition.KeySelector(item)
                    ?? throw new InvalidOperationException(
                        $"DataForm collection '{PropertyPath}' produced a null item key at index {index}.");
                runtime = _items.FirstOrDefault(existing => Equals(existing.Key, key));
            }

            if (!keys.Add(key))
                throw new InvalidOperationException($"DataForm collection '{PropertyPath}' produced duplicate item key '{key}'.");

            if (runtime is null || !ReferenceEquals(runtime.Item, item))
                runtime = CreateRuntime(item, key);
            runtime.Index = index;
            next.Add(runtime);
        }

        ReleaseItems(next);
        _items.Clear();
        _items.AddRange(next);
    }

    private ItemRuntime CreateRuntime(TItem item, object key)
    {
        EditContext context = new(item);
        ItemRuntime runtime = new(item, key, context);
        context.OnValidationStateChanged += OnItemValidationStateChanged;
        return runtime;
    }

    private void ReleaseItems(IReadOnlyCollection<ItemRuntime> retained)
    {
        foreach (ItemRuntime runtime in _items)
        {
            if (retained.Contains(runtime)) continue;
            runtime.Context.OnValidationStateChanged -= OnItemValidationStateChanged;
        }
    }

    private void OnParentValidationRequested(object? sender, ValidationRequestedEventArgs args)
        => RefreshParentValidation(validateChildren: true);

    private void OnItemValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        if (_validatingParent) return;
        RefreshParentValidation(validateChildren: false);
    }

    private void RefreshParentValidation(bool validateChildren)
    {
        EditContext? parent = _subscribedParent;
        ValidationMessageStore? store = _parentStore;
        if (parent is null || store is null) return;

        _validatingParent = true;
        try
        {
            store.Clear();
            int count = _items.Count;
            if (count < TypedDefinition.MinimumItems)
            {
                store.Add(
                    FieldIdentifier,
                    TypedDefinition.MinimumItemsError
                    ?? string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        Texts.DataFormMinimumItems,
                        TypedDefinition.MinimumItems));
            }
            if (count > TypedDefinition.MaximumItems)
            {
                store.Add(
                    FieldIdentifier,
                    TypedDefinition.MaximumItemsError
                    ?? string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        Texts.DataFormMaximumItems,
                        TypedDefinition.MaximumItems));
            }

            foreach (ItemRuntime runtime in _items)
            {
                if (validateChildren) runtime.Context.Validate();
                foreach (string message in runtime.Context.GetValidationMessages())
                    store.Add(FieldIdentifier, $"Item {runtime.Index + 1}: {message}");
            }
            parent.NotifyValidationStateChanged();
        }
        finally
        {
            _validatingParent = false;
        }
    }

    private void DetachParent()
    {
        EditContext? parent = _subscribedParent;
        if (parent is not null)
            parent.OnValidationRequested -= OnParentValidationRequested;
        _parentStore?.Clear();
        _parentStore = null;
        _subscribedParent = null;
        parent?.NotifyValidationStateChanged();
    }

    /// <summary>Releases parent and item EditContext event handlers.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        DetachParent();
        ReleaseItems([]);
        _items.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed class ItemRuntime(TItem item, object key, EditContext context)
    {
        public TItem Item { get; } = item;
        public object Key { get; } = key;
        public EditContext Context { get; } = context;
        public int Index { get; set; }
    }
}
