using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Internal bounded collection and embedded-subform editor.</summary>
public partial class OmniDataFormCollectionEditor<TModel, TCollection,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>
    where TModel : class
    where TItem : class
{
    private readonly List<ItemRuntime> _items = [];
    private readonly CancellationTokenSource _lifetime = new();
    private EditContext? _subscribedParent;
    private IOmniFormValidationParticipantRegistry? _subscribedValidationRegistry;
    private ValidationMessageStore? _parentStore;
    private TCollection? _workingValue;
    private TCollection? _factoryValue;
    private bool _validatingParent;
    private bool _initializedFromFactory;
    private int _disposeState;
    private IDictionary<string, object>? _gridParameters;
    private IDataFormCollectionGridDefinition<TItem>? _gridParameterDefinition;

    [CascadingParameter] private EditContext? ParentEditContext { get; set; }
    [CascadingParameter] private IOmniFormValidationParticipantRegistry? ValidationParticipantRegistry { get; set; }

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
        if (_workingValue is null && (TypedDefinition.ItemFactory is not null || TypedDefinition.Grid is not null))
        {
            throw new InvalidOperationException(
                $"DataForm collection '{PropertyPath}' is null. Configure CreateCollection before enabling item creation or grid rendering.");
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
        if (!ReferenceEquals(_subscribedValidationRegistry, ValidationParticipantRegistry))
        {
            _subscribedValidationRegistry?.UnregisterValidationParticipant(this);
            _subscribedValidationRegistry = ValidationParticipantRegistry;
            _subscribedValidationRegistry?.RegisterValidationParticipant(this);
        }
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

    private IDictionary<string, object> GridParameters
    {
        get
        {
            IDataFormCollectionGridDefinition<TItem> grid = TypedDefinition.Grid
                ?? throw new InvalidOperationException("Collection grid definition is unavailable.");
            if (!ReferenceEquals(_gridParameterDefinition, grid))
            {
                _gridParameters = BuildGridParameters(grid);
                _gridParameterDefinition = grid;
            }
            IDictionary<string, object> parameters = _gridParameters!;
            parameters["Items"] = GetCollection();
            parameters["Disabled"] = Disabled;
            parameters["ReadOnly"] = ReadOnly || CollectionIsReadOnly;
            parameters["MinimumItems"] = TypedDefinition.MinimumItems;
            parameters["MaximumItems"] = TypedDefinition.MaximumItems;
            parameters["AllowDelete"] = TypedDefinition.AllowRemove;
            parameters["AllowReorder"] = TypedDefinition.AllowReorder;
            return parameters;
        }
    }

    private IDictionary<string, object> BuildGridParameters(IDataFormCollectionGridDefinition<TItem> grid)
    {
        Dictionary<string, object> parameters = new(11, StringComparer.Ordinal)
        {
            ["Schema"] = grid.Schema,
            ["Items"] = GetCollection(),
            ["ItemsChanged"] = EventCallback.Factory.Create<IList<TItem>>(this, OnGridItemsChangedAsync),
            ["Disabled"] = Disabled,
            ["ReadOnly"] = ReadOnly || CollectionIsReadOnly,
            ["MinimumItems"] = TypedDefinition.MinimumItems,
            ["MaximumItems"] = TypedDefinition.MaximumItems,
            ["AllowDelete"] = TypedDefinition.AllowRemove,
            ["AllowReorder"] = TypedDefinition.AllowReorder,
            ["RenderEditorFormElement"] = false
        };
        if (TypedDefinition.EmptyTemplate is not null)
            parameters["EmptyTemplate"] = TypedDefinition.EmptyTemplate;
        return parameters;
    }

    private async Task OnGridItemsChangedAsync(IList<TItem> items)
    {
        if (!ReferenceEquals(items, _workingValue))
            throw new InvalidOperationException("DataForm collection grid replaced the collection instance unexpectedly.");
        SynchronizeItems();
        await NotifyCollectionChangedAsync();
    }

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
                IEnumerable<string> messages;
                if (TypedDefinition.Grid is not null)
                {
                    if (validateChildren) ValidateGridItem(runtime);
                    messages = runtime.GridMessages;
                }
                else
                {
                    if (validateChildren) runtime.Context.Validate();
                    messages = runtime.Context.GetValidationMessages();
                }
                foreach (string message in messages)
                    store.Add(FieldIdentifier, $"Item {runtime.Index + 1}: {message}");
            }
            parent.NotifyValidationStateChanged();
        }
        finally
        {
            _validatingParent = false;
        }
    }

    private void ValidateGridItem(ItemRuntime runtime)
    {
        runtime.GridMessages.Clear();
        DataFormSchema<TItem> schema = TypedDefinition.Grid!.FormSchema;
        foreach (DataFormField<TItem> field in schema.Fields)
        {
            if (!field.Visible || (field.VisibleWhen is not null && !field.VisibleWhen(runtime.Item)))
                continue;
            object? value = field.PropertyPath.GetValue(runtime.Item);
            List<ValidationResult> annotations = [];
            DataAnnotationsValidation.ValidateProperty(
                field.PropertyPath.GetOwner(runtime.Item),
                field.PropertyPath.Leaf,
                value,
                annotations);
            foreach (ValidationResult result in annotations)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    AddGridMessage(runtime, result.ErrorMessage);
            }
            if (field.IsRequired(runtime.Item) && IsMissing(value))
                AddGridMessage(runtime, field.RequiredError ?? Texts.Required);
            foreach (IDataFormFieldValidator<TItem> validator in field.Validators)
            {
                if (!validator.IsSynchronous) continue;
                string? message = validator.Validate(runtime.Item, value);
                if (!string.IsNullOrWhiteSpace(message)) AddGridMessage(runtime, message);
            }
        }

        if (runtime.Item is IValidatableObject validatable)
        {
            ValidationContext context = new(runtime.Item, typeof(TItem).Name, serviceProvider: null, items: null);
            foreach (ValidationResult result in validatable.Validate(context))
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    AddGridMessage(runtime, result.ErrorMessage);
            }
        }
    }

    async ValueTask IOmniFormValidationParticipant.ValidateAsync(
        EditContext context,
        ValidationMessageStore store,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposeState) != 0 || !ReferenceEquals(context, _subscribedParent)) return;
        using CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetime.Token);
        CancellationToken token = operation.Token;
        DataFormSchema<TItem>? schema = TypedDefinition.Grid?.FormSchema ?? TypedDefinition.ItemSchema;
        if (schema is null) return;
        try
        {
            foreach (ItemRuntime runtime in _items)
            {
                token.ThrowIfCancellationRequested();
                foreach (DataFormField<TItem> field in schema.Fields)
                {
                    if (!field.Visible || (field.VisibleWhen is not null && !field.VisibleWhen(runtime.Item)))
                        continue;
                    object? value = field.PropertyPath.GetValue(runtime.Item);
                    foreach (IDataFormFieldValidator<TItem> validator in field.Validators)
                    {
                        if (validator.IsSynchronous) continue;
                        token.ThrowIfCancellationRequested();
                        string? message = await validator.ValidateAsync(runtime.Item, value, token);
                        token.ThrowIfCancellationRequested();
                        if (!string.IsNullOrWhiteSpace(message))
                            store.Add(FieldIdentifier, $"Item {runtime.Index + 1}: {message}");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer parent validation or disposal superseded this participant.
        }
    }

    private static bool IsMissing(object? value)
        => value is null || value is string text && string.IsNullOrWhiteSpace(text);

    private static void AddGridMessage(ItemRuntime runtime, string message)
    {
        if (!runtime.GridMessages.Contains(message, StringComparer.Ordinal))
            runtime.GridMessages.Add(message);
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
        _lifetime.Cancel();
        _subscribedValidationRegistry?.UnregisterValidationParticipant(this);
        _subscribedValidationRegistry = null;
        DetachParent();
        ReleaseItems([]);
        _items.Clear();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class ItemRuntime(TItem item, object key, EditContext context)
    {
        public TItem Item { get; } = item;
        public object Key { get; } = key;
        public EditContext Context { get; } = context;
        public List<string> GridMessages { get; } = [];
        public int Index { get; set; }
    }
}
