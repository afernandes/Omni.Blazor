using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Context supplied to a collection item's custom heading.</summary>
public sealed record DataFormCollectionItemContext<TItem>(
    TItem Item,
    int Index,
    int Count)
    where TItem : class;

/// <summary>Strongly typed configuration for a bounded editable collection field.</summary>
public sealed class DataFormCollectionEditorBuilder<TModel, TCollection, TItem>
    where TModel : class
    where TItem : class
{
    private readonly Action _ensureMutable;
    private DataFormSchema<TItem>? _itemSchema;
    private Func<TItem>? _itemFactory;
    private Func<TCollection>? _collectionFactory;
    private Func<TItem, object>? _keySelector;
    private int _minItems;
    private int _maxItems = 100;
    private bool _allowRemove = true;
    private bool _allowReorder;
    private string? _addText;
    private string? _removeText;
    private string? _moveUpText;
    private string? _moveDownText;
    private string? _minimumItemsError;
    private string? _maximumItemsError;
    private RenderFragment? _emptyTemplate;
    private RenderFragment<DataFormCollectionItemContext<TItem>>? _itemHeader;

    internal DataFormCollectionEditorBuilder(Action ensureMutable)
        => _ensureMutable = ensureMutable;

    /// <summary>Sets the nested schema rendered for every item.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> ItemSchema(
        DataFormSchema<TItem> schema)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _itemSchema = schema;
        return this;
    }

    /// <summary>Enables adding items through a factory.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> CreateItem(
        Func<TItem> factory)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(factory);
        _itemFactory = factory;
        return this;
    }

    /// <summary>Creates a collection when the bound property is initially null.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> CreateCollection(
        Func<TCollection> factory)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(factory);
        _collectionFactory = factory;
        return this;
    }

    /// <summary>Sets a stable item key. Object identity is used by default.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> Key(
        Func<TItem, object> selector)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _keySelector = selector;
        return this;
    }

    /// <summary>Sets inclusive item-count bounds. Maximum is capped at 1,000.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> Bounds(
        int minimum = 0,
        int maximum = 100,
        string? minimumError = null,
        string? maximumError = null)
    {
        _ensureMutable();
        if (minimum is < 0 or > 1000) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (maximum is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (minimum > maximum) throw new ArgumentException("Collection minimum cannot exceed maximum.");
        _minItems = minimum;
        _maxItems = maximum;
        _minimumItemsError = minimumError;
        _maximumItemsError = maximumError;
        return this;
    }

    /// <summary>Allows or prevents removing items.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> Removable(bool value = true)
    {
        _ensureMutable();
        _allowRemove = value;
        return this;
    }

    /// <summary>Shows keyboard-accessible move-up and move-down actions.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> Reorderable(bool value = true)
    {
        _ensureMutable();
        _allowReorder = value;
        return this;
    }

    /// <summary>Sets localized collection action text.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> Texts(
        string? add = null,
        string? remove = null,
        string? moveUp = null,
        string? moveDown = null)
    {
        _ensureMutable();
        _addText = add;
        _removeText = remove;
        _moveUpText = moveUp;
        _moveDownText = moveDown;
        return this;
    }

    /// <summary>Sets custom empty-state and item-heading templates.</summary>
    public DataFormCollectionEditorBuilder<TModel, TCollection, TItem> Templates(
        RenderFragment? empty = null,
        RenderFragment<DataFormCollectionItemContext<TItem>>? itemHeader = null)
    {
        _ensureMutable();
        _emptyTemplate = empty;
        _itemHeader = itemHeader;
        return this;
    }

    internal DataFormCollectionDefinition<TModel, TCollection, TItem> Build()
        => new(
            _itemSchema,
            _itemFactory,
            _collectionFactory,
            _keySelector,
            _minItems,
            _maxItems,
            _allowRemove,
            _allowReorder,
            _addText,
            _removeText,
            _moveUpText,
            _moveDownText,
            _minimumItemsError,
            _maximumItemsError,
            _emptyTemplate,
            _itemHeader);
}

internal interface IDataFormCollectionDefinition<TModel> where TModel : class
{
    Type EditorType { get; }
    int MinimumItems { get; }
    int MaximumItems { get; }
    string? MinimumItemsError { get; }
    string? MaximumItemsError { get; }
    int GetCount(object? value);
}

internal sealed record DataFormCollectionDefinition<TModel, TCollection, TItem>(
    DataFormSchema<TItem>? ItemSchema,
    Func<TItem>? ItemFactory,
    Func<TCollection>? CollectionFactory,
    Func<TItem, object>? KeySelector,
    int MinimumItems,
    int MaximumItems,
    bool AllowRemove,
    bool AllowReorder,
    string? AddText,
    string? RemoveText,
    string? MoveUpText,
    string? MoveDownText,
    string? MinimumItemsError,
    string? MaximumItemsError,
    RenderFragment? EmptyTemplate,
    RenderFragment<DataFormCollectionItemContext<TItem>>? ItemHeader)
    : IDataFormCollectionDefinition<TModel>
    where TModel : class
    where TItem : class
{
    public Type EditorType { get; } = typeof(Omni.Blazor.Components.OmniDataFormCollectionEditor<,,>)
        .MakeGenericType(typeof(TModel), typeof(TCollection), typeof(TItem));

    public int GetCount(object? value) => value is ICollection<TItem> items ? items.Count : 0;
}
