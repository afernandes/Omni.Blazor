using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>
/// Immutable CRUD composition schema shared by <c>OmniDataGridForm</c> instances.
/// It reuses a <see cref="DataFormSchema{TModel}"/> for editors and owns only
/// grid presentation and operation metadata.
/// </summary>
public sealed class DataGridFormSchema<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    internal DataGridFormSchema(
        Func<TItem, TKey> keySelector,
        DataFormSchema<TItem> formSchema,
        DataGridFormGridOptions<TItem> grid,
        DataGridFormCreateOptions<TItem>? create,
        DataGridFormEditOptions<TItem>? edit,
        DataGridFormDeleteOptions<TItem>? delete,
        DataGridFormActionsColumnOptions actionsColumn,
        DataGridFormBulkActionsOptions bulkActionsBar,
        IReadOnlyList<DataGridFormAction<TItem, TKey>> actions,
        IReadOnlyList<DataGridFormBulkAction<TItem, TKey>> bulkActions)
    {
        KeySelector = keySelector;
        FormSchema = formSchema;
        Grid = grid;
        CreateOptions = create;
        EditOptions = edit;
        DeleteOptions = delete;
        ActionsColumn = actionsColumn;
        BulkActionsBar = bulkActionsBar;
        Actions = actions;
        BulkActions = bulkActions;
    }

    /// <summary>Stable key selector used by row state and persistence.</summary>
    public Func<TItem, TKey> KeySelector { get; }

    /// <summary>DataForm schema used for create and edit drafts.</summary>
    public DataFormSchema<TItem> FormSchema { get; }

    /// <summary>Generated DataGrid columns and feature options.</summary>
    public DataGridFormGridOptions<TItem> Grid { get; }

    /// <summary>Create operation, or null when creation is disabled.</summary>
    public DataGridFormCreateOptions<TItem>? CreateOptions { get; }

    /// <summary>Edit operation, or null when editing is disabled.</summary>
    public DataGridFormEditOptions<TItem>? EditOptions { get; }

    /// <summary>Delete operation, or null when deletion is disabled.</summary>
    public DataGridFormDeleteOptions<TItem>? DeleteOptions { get; }

    /// <summary>Generated row-actions column presentation and overflow policy.</summary>
    public DataGridFormActionsColumnOptions ActionsColumn { get; }

    /// <summary>Selected-item actions presentation and overflow policy.</summary>
    public DataGridFormBulkActionsOptions BulkActionsBar { get; }

    /// <summary>Additional strongly typed row actions.</summary>
    public IReadOnlyList<DataGridFormAction<TItem, TKey>> Actions { get; }

    /// <summary>Additional strongly typed selected-item actions.</summary>
    public IReadOnlyList<DataGridFormBulkAction<TItem, TKey>> BulkActions { get; }

    /// <summary>Creates and builds an immutable CRUD schema.</summary>
    public static DataGridFormSchema<TItem, TKey> Create(
        Action<DataGridFormSchemaBuilder<TItem, TKey>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataGridFormSchemaBuilder<TItem, TKey> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable builder for conditional schema construction.</summary>
    public static DataGridFormSchemaBuilder<TItem, TKey> Builder() => new();

    /// <summary>Creates an immutable derived CRUD schema from this reusable base.</summary>
    public DataGridFormSchema<TItem, TKey> Extend(Action<DataGridFormSchemaBuilder<TItem, TKey>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataGridFormSchemaBuilder<TItem, TKey> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Strongly typed builder for a grid and DataForm CRUD composition.</summary>
public sealed class DataGridFormSchemaBuilder<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly DataGridFormGridBuilder<TItem> _grid;
    private readonly DataGridFormActionsColumnBuilder _actionsColumn;
    private readonly DataGridFormBulkActionsBuilder _bulkActionsBar;
    private readonly List<DataGridFormAction<TItem, TKey>> _actions = [];
    private readonly HashSet<string> _actionIds = new(StringComparer.Ordinal);
    private readonly List<DataGridFormBulkAction<TItem, TKey>> _bulkActions = [];
    private readonly HashSet<string> _bulkActionIds = new(StringComparer.Ordinal);
    private Func<TItem, TKey>? _keySelector;
    private DataFormSchema<TItem>? _formSchema;
    private DataGridFormCreateOptions<TItem>? _create;
    private DataGridFormEditOptions<TItem>? _edit;
    private DataGridFormDeleteOptions<TItem>? _delete;
    private bool _built;

    /// <summary>Creates an empty CRUD schema builder.</summary>
    public DataGridFormSchemaBuilder()
    {
        _grid = new DataGridFormGridBuilder<TItem>(EnsureMutable);
        _actionsColumn = new DataGridFormActionsColumnBuilder(EnsureMutable);
        _bulkActionsBar = new DataGridFormBulkActionsBuilder(EnsureMutable);
    }

    /// <summary>Includes form, grid and operation metadata from an immutable CRUD schema.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Include(DataGridFormSchema<TItem, TKey> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _keySelector = schema.KeySelector;
        _formSchema = schema.FormSchema;
        _create = schema.CreateOptions;
        _edit = schema.EditOptions;
        _delete = schema.DeleteOptions;
        _grid.UseOptions(schema.Grid);
        _actionsColumn.UseOptions(schema.ActionsColumn);
        _bulkActionsBar.UseOptions(schema.BulkActionsBar);
        foreach (DataGridFormAction<TItem, TKey> action in schema.Actions)
        {
            if (!_actionIds.Add(action.Id))
                throw new InvalidOperationException($"DataGridForm action '{action.Id}' was declared more than once.");
            _actions.Add(action);
        }
        foreach (DataGridFormBulkAction<TItem, TKey> action in schema.BulkActions)
        {
            if (!_bulkActionIds.Add(action.Id))
                throw new InvalidOperationException($"DataGridForm bulk action '{action.Id}' was declared more than once.");
            _bulkActions.Add(action);
        }
        return this;
    }

    /// <summary>Removes an inherited row action by stable id.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> RemoveAction(string id)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        int index = _actions.FindIndex(action => string.Equals(action.Id, id, StringComparison.Ordinal));
        if (index < 0) return this;
        _actions.RemoveAt(index);
        _actionIds.Remove(id);
        return this;
    }

    /// <summary>Removes an inherited selected-item action by stable id.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> RemoveBulkAction(string id)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        int index = _bulkActions.FindIndex(action => string.Equals(action.Id, id, StringComparison.Ordinal));
        if (index < 0) return this;
        _bulkActions.RemoveAt(index);
        _bulkActionIds.Remove(id);
        return this;
    }

    /// <summary>Sets the stable key used by persistence and per-row operation state.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Key(
        Expression<Func<TItem, TKey>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _keySelector = selector.Compile();
        return this;
    }

    /// <summary>Sets the reusable DataForm schema used for create and edit drafts.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Form(DataFormSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _formSchema = schema;
        return this;
    }

    /// <summary>Configures generated DataGrid columns and features.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Grid(
        Action<DataGridFormGridBuilder<TItem>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_grid);
        return this;
    }

    /// <summary>Reuses an immutable standalone DataGrid schema for the generated grid.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Grid(DataGridSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _grid.UseSchema(schema);
        return this;
    }

    /// <summary>Configures the generated row-actions column and its overflow menu.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> ActionsColumn(
        Action<DataGridFormActionsColumnBuilder> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_actionsColumn);
        return this;
    }

    /// <summary>Configures selected-item action overflow and its menu trigger.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> BulkActions(
        Action<DataGridFormBulkActionsBuilder> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_bulkActionsBar);
        return this;
    }

    /// <summary>Enables creation through a generated DataForm draft.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Create(
        Action<DataGridFormCreateBuilder<TItem>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        DataGridFormCreateBuilder<TItem> builder = new(EnsureMutable);
        configure(builder);
        _create = builder.Build();
        return this;
    }

    /// <summary>Enables safe edit-through-copy through a generated DataForm draft.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Edit(
        Action<DataGridFormEditBuilder<TItem>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        DataGridFormEditBuilder<TItem> builder = new(EnsureMutable);
        configure(builder);
        _edit = builder.Build();
        return this;
    }

    /// <summary>Enables confirmed deletion.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Delete(
        Action<DataGridFormDeleteBuilder<TItem>>? configure = null)
    {
        EnsureMutable();
        DataGridFormDeleteBuilder<TItem> builder = new(EnsureMutable);
        configure?.Invoke(builder);
        _delete = builder.Build();
        return this;
    }

    /// <summary>Adds an extra cancellable row action.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> Action(
        string text,
        string? icon,
        Func<DataGridFormActionContext<TItem, TKey>, CancellationToken, ValueTask> execute,
        Action<DataGridFormActionBuilder<TItem>>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(execute);
        DataGridFormActionBuilder<TItem> builder = new(text, icon, EnsureMutable);
        configure?.Invoke(builder);
        DataGridFormAction<TItem, TKey> action = builder.Build<TKey>(execute);
        if (!_actionIds.Add(action.Id))
            throw new InvalidOperationException($"DataGridForm action '{action.Id}' was declared more than once.");
        _actions.Add(action);
        return this;
    }

    /// <summary>Adds a cancellable action for the current selected-item snapshot.</summary>
    public DataGridFormSchemaBuilder<TItem, TKey> BulkAction(
        string text,
        string? icon,
        Func<DataGridFormBulkActionContext<TItem, TKey>, CancellationToken, ValueTask> execute,
        Action<DataGridFormBulkActionBuilder<TItem>>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(execute);
        DataGridFormBulkActionBuilder<TItem> builder = new(text, icon, EnsureMutable);
        configure?.Invoke(builder);
        DataGridFormBulkAction<TItem, TKey> action = builder.Build<TKey>(execute);
        if (!_bulkActionIds.Add(action.Id))
            throw new InvalidOperationException($"DataGridForm bulk action '{action.Id}' was declared more than once.");
        _bulkActions.Add(action);
        return this;
    }

    /// <summary>Builds the immutable schema. A key selector is required.</summary>
    public DataGridFormSchema<TItem, TKey> Build()
    {
        EnsureMutable();
        _built = true;
        if (_keySelector is null)
            throw new InvalidOperationException("DataGridForm requires a stable Key selector.");
        DataFormSchema<TItem> formSchema = _formSchema
            ?? DataFormSchema<TItem>.Create(static _ => { });
        return new DataGridFormSchema<TItem, TKey>(
            _keySelector,
            formSchema,
            _grid.Build(formSchema),
            _create,
            _edit,
            _delete,
            _actionsColumn.Build(),
            _bulkActionsBar.Build(),
            Array.AsReadOnly(_actions.OrderBy(static action => action.Order).ToArray()),
            Array.AsReadOnly(_bulkActions.OrderBy(static action => action.Order).ToArray()));
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("DataGridForm schema is immutable after Build().");
    }
}

/// <summary>Fluent builder for the generated row-actions column.</summary>
public sealed class DataGridFormActionsColumnBuilder
{
    private readonly Action _ensureMutable;
    private string _width = "auto";
    private bool _resizable = true;
    private FrozenPosition? _frozen;
    private DataGridFormActionOverflow _overflow;
    private int _maximumInlineActions = int.MaxValue;
    private string? _menuText;
    private string _menuIcon = "more-horizontal";
    private string? _menuAriaLabel;
    private DataGridFormActionPlacement _reorderPlacement = DataGridFormActionPlacement.Auto;

    internal DataGridFormActionsColumnBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    internal void UseOptions(DataGridFormActionsColumnOptions options)
    {
        _ensureMutable();
        _width = options.Width;
        _resizable = options.Resizable;
        _frozen = options.Frozen;
        _overflow = options.Overflow;
        _maximumInlineActions = options.MaximumInlineActions;
        _menuText = options.MenuText;
        _menuIcon = options.MenuIcon;
        _menuAriaLabel = options.MenuAriaLabel;
        _reorderPlacement = options.ReorderPlacement;
    }

    /// <summary>Sets the generated column CSS width.</summary>
    public DataGridFormActionsColumnBuilder Width(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _width, value);
    }

    /// <summary>Enables or disables the native DataGrid column resizer.</summary>
    public DataGridFormActionsColumnBuilder Resizable(bool enabled = true)
        => Set(ref _resizable, enabled);

    /// <summary>Freezes the column at the requested physical edge, or clears freezing with null.</summary>
    public DataGridFormActionsColumnBuilder Frozen(FrozenPosition? position)
        => Set(ref _frozen, position);

    /// <summary>Freezes the actions column at the right edge.</summary>
    public DataGridFormActionsColumnBuilder FrozenRight()
        => Frozen(FrozenPosition.Right);

    /// <summary>Freezes the actions column at the left edge.</summary>
    public DataGridFormActionsColumnBuilder FrozenLeft()
        => Frozen(FrozenPosition.Left);

    /// <summary>Enables manual or priority-based automatic overflow.</summary>
    public DataGridFormActionsColumnBuilder Overflow(
        DataGridFormActionOverflow value,
        int maximumInlineActions = 2)
    {
        if (maximumInlineActions < 0) throw new ArgumentOutOfRangeException(nameof(maximumInlineActions));
        _ensureMutable();
        _overflow = value;
        _maximumInlineActions = value == DataGridFormActionOverflow.Automatic
            ? maximumInlineActions
            : int.MaxValue;
        return this;
    }

    /// <summary>Configures the overflow-menu trigger.</summary>
    public DataGridFormActionsColumnBuilder Menu(
        string? text = null,
        string icon = "more-horizontal",
        string? ariaLabel = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icon);
        _ensureMutable();
        _menuText = text;
        _menuIcon = icon;
        _menuAriaLabel = ariaLabel;
        return this;
    }

    /// <summary>Places generated move actions inline, in the menu, or under automatic overflow.</summary>
    public DataGridFormActionsColumnBuilder ReorderActions(DataGridFormActionPlacement placement)
        => Set(ref _reorderPlacement, placement);

    internal DataGridFormActionsColumnOptions Build()
        => new(
            _width,
            _resizable,
            _frozen,
            _overflow,
            _maximumInlineActions,
            _menuText,
            _menuIcon,
            _menuAriaLabel,
            _reorderPlacement);

    private DataGridFormActionsColumnBuilder Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Fluent builder for selected-item action overflow.</summary>
public sealed class DataGridFormBulkActionsBuilder
{
    private readonly Action _ensureMutable;
    private DataGridFormActionOverflow _overflow;
    private int _maximumInlineActions = int.MaxValue;
    private string? _menuText;
    private string _menuIcon = "more-horizontal";
    private string? _menuAriaLabel;

    internal DataGridFormBulkActionsBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    internal void UseOptions(DataGridFormBulkActionsOptions options)
    {
        _ensureMutable();
        _overflow = options.Overflow;
        _maximumInlineActions = options.MaximumInlineActions;
        _menuText = options.MenuText;
        _menuIcon = options.MenuIcon;
        _menuAriaLabel = options.MenuAriaLabel;
    }

    /// <summary>Enables manual or priority-based automatic overflow.</summary>
    public DataGridFormBulkActionsBuilder Overflow(
        DataGridFormActionOverflow value,
        int maximumInlineActions = 2)
    {
        if (maximumInlineActions < 0) throw new ArgumentOutOfRangeException(nameof(maximumInlineActions));
        _ensureMutable();
        _overflow = value;
        _maximumInlineActions = value == DataGridFormActionOverflow.Automatic
            ? maximumInlineActions
            : int.MaxValue;
        return this;
    }

    /// <summary>Configures the selected-item overflow-menu trigger.</summary>
    public DataGridFormBulkActionsBuilder Menu(
        string? text = null,
        string icon = "more-horizontal",
        string? ariaLabel = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icon);
        _ensureMutable();
        _menuText = text;
        _menuIcon = icon;
        _menuAriaLabel = ariaLabel;
        return this;
    }

    internal DataGridFormBulkActionsOptions Build()
        => new(_overflow, _maximumInlineActions, _menuText, _menuIcon, _menuAriaLabel);
}

/// <summary>Strongly typed DataGrid presentation builder.</summary>
public sealed class DataGridFormGridBuilder<TItem> where TItem : class
{
    private readonly Action _ensureMutable;
    private readonly List<IDataGridFormColumnBuilder<TItem>> _columns = [];
    private readonly HashSet<string> _properties = new(StringComparer.Ordinal);
    private bool _autoColumnsFromForm = true;
    private bool _allowSearch = true;
    private bool _allowPaging = true;
    private int _pageSize = 10;
    private bool _allowSorting = true;
    private bool _allowColumnFilter;
    private bool _allowColumnResize;
    private bool _allowColumnVisibility;
    private bool _allowGrouping;
    private bool _allowExport;
    private bool _virtualize;
    private string? _height;
    private float _rowHeight = 44;
    private string? _searchPlaceholder;
    private string? _emptyText;
    private DataGridSchema<TItem>? _schema;

    internal DataGridFormGridBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    internal void UseOptions(DataGridFormGridOptions<TItem> options)
    {
        _ensureMutable();
        _schema = null;
        _columns.Clear();
        _properties.Clear();
        foreach (DataGridFormColumn<TItem> column in options.Columns)
        {
            _columns.Add(new ExistingDataGridFormColumnBuilder<TItem>(column));
            _properties.Add(column.PropertyName);
        }
        _autoColumnsFromForm = false;
        _allowSearch = options.AllowSearch;
        _allowPaging = options.AllowPaging;
        _pageSize = options.PageSize;
        _allowSorting = options.AllowSorting;
        _allowColumnFilter = options.AllowColumnFilter;
        _allowColumnResize = options.AllowColumnResize;
        _allowColumnVisibility = options.AllowColumnVisibility;
        _allowGrouping = options.AllowGrouping;
        _allowExport = options.AllowExport;
        _virtualize = options.Virtualize;
        _height = options.Height;
        _rowHeight = options.RowHeight;
        _searchPlaceholder = options.SearchPlaceholder;
        _emptyText = options.EmptyText;
    }

    /// <summary>Adds or overrides one DataGrid column with a typed selector.</summary>
    public DataGridFormGridBuilder<TItem> Column<TValue>(
        Expression<Func<TItem, TValue>> property,
        Action<DataGridFormColumnBuilder<TItem, TValue>>? configure = null)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(property);
        if (_schema is not null)
            throw new InvalidOperationException("DataGridForm cannot add columns after adopting a DataGrid schema.");
        DataFormPropertyPath path = DataFormSchemaBuilder<TItem>.ResolveProperty(property);
        if (!_properties.Add(path.Path))
            throw new InvalidOperationException($"DataGridForm column '{path.Path}' was declared more than once.");
        DataGridFormColumnBuilder<TItem, TValue> builder = new(path, property.Compile(), _ensureMutable);
        configure?.Invoke(builder);
        _columns.Add(builder);
        return this;
    }

    /// <summary>Includes DataForm fields that were not explicitly configured as columns. Default true.</summary>
    public DataGridFormGridBuilder<TItem> AutoColumnsFromForm(bool enabled = true)
        => Set(ref _autoColumnsFromForm, enabled);

    /// <summary>Shows or hides the grid search box. Default true.</summary>
    public DataGridFormGridBuilder<TItem> AllowSearch(bool enabled = true)
        => Set(ref _allowSearch, enabled);

    /// <summary>Enables paging and sets its page size.</summary>
    public DataGridFormGridBuilder<TItem> AllowPaging(bool enabled = true, int pageSize = 10)
    {
        _ensureMutable();
        if (pageSize is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(pageSize));
        _allowPaging = enabled;
        _pageSize = pageSize;
        return this;
    }

    /// <summary>Enables or disables sorting. Default true.</summary>
    public DataGridFormGridBuilder<TItem> AllowSorting(bool enabled = true)
        => Set(ref _allowSorting, enabled);

    /// <summary>Enables per-column filters.</summary>
    public DataGridFormGridBuilder<TItem> AllowColumnFilter(bool enabled = true)
        => Set(ref _allowColumnFilter, enabled);

    /// <summary>Enables column resize.</summary>
    public DataGridFormGridBuilder<TItem> AllowColumnResize(bool enabled = true)
        => Set(ref _allowColumnResize, enabled);

    /// <summary>Enables the column visibility menu.</summary>
    public DataGridFormGridBuilder<TItem> AllowColumnVisibility(bool enabled = true)
        => Set(ref _allowColumnVisibility, enabled);

    /// <summary>Enables grouping for columns marked groupable.</summary>
    public DataGridFormGridBuilder<TItem> AllowGrouping(bool enabled = true)
        => Set(ref _allowGrouping, enabled);

    /// <summary>Enables bounded CSV export.</summary>
    public DataGridFormGridBuilder<TItem> AllowExport(bool enabled = true)
        => Set(ref _allowExport, enabled);

    /// <summary>Enables row virtualization with a fixed height.</summary>
    public DataGridFormGridBuilder<TItem> Virtualize(
        string height = "520px",
        float rowHeight = 44)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(height);
        if (!float.IsFinite(rowHeight) || rowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        _virtualize = true;
        _height = height;
        _rowHeight = rowHeight;
        _allowPaging = false;
        return this;
    }

    /// <summary>Overrides search and empty-state text.</summary>
    public DataGridFormGridBuilder<TItem> Texts(
        string? searchPlaceholder = null,
        string? empty = null)
    {
        _ensureMutable();
        _searchPlaceholder = searchPlaceholder;
        _emptyText = empty;
        return this;
    }

    internal DataGridFormGridOptions<TItem> Build(DataFormSchema<TItem> formSchema)
    {
        if (_schema is not null) return FromSchema(_schema);
        List<DataGridFormColumn<TItem>> columns = new(_columns.Count + formSchema.Count);
        foreach (IDataGridFormColumnBuilder<TItem> column in _columns)
            columns.Add(column.Build());
        if (_autoColumnsFromForm) AddFormColumns(formSchema, columns);
        if (columns.Count == 0)
            throw new InvalidOperationException(
                "DataGridForm requires at least one typed FormSchema field, Grid column or DataGridSchema column.");

        return new DataGridFormGridOptions<TItem>(
            Array.AsReadOnly(columns.ToArray()),
            _allowSearch,
            _allowPaging,
            _pageSize,
            _allowSorting,
            _allowColumnFilter,
            _allowColumnResize,
            _allowColumnVisibility,
            _allowGrouping,
            _allowExport,
            _virtualize,
            _height,
            _rowHeight,
            _searchPlaceholder,
            _emptyText);
    }

    internal void UseSchema(DataGridSchema<TItem> schema)
    {
        _ensureMutable();
        if (_columns.Count != 0)
            throw new InvalidOperationException("DataGridForm cannot adopt a DataGrid schema after declaring columns.");
        _schema = schema;
        _autoColumnsFromForm = false;
    }

    private static DataGridFormGridOptions<TItem> FromSchema(DataGridSchema<TItem> schema)
    {
        DataGridFormColumn<TItem>[] columns = new DataGridFormColumn<TItem>[schema.Columns.Count];
        for (int index = 0; index < columns.Length; index++)
        {
            DataGridColumnDefinition<TItem> column = schema.Columns[index];
            columns[index] = new DataGridFormColumn<TItem>(
                column.PropertyName,
                column.Title,
                column.Property,
                column.TextSelector,
                column.Template,
                column.Width,
                column.Sortable,
                column.Resizable,
                column.Filterable,
                column.FilterType,
                column.Groupable,
                column.CanHide,
                column.Visible);
        }
        return new DataGridFormGridOptions<TItem>(
            Array.AsReadOnly(columns),
            schema.AllowSearch,
            schema.AllowPaging,
            schema.PageSize,
            schema.AllowSorting,
            schema.AllowColumnFilter,
            schema.AllowColumnResize,
            schema.AllowColumnVisibility,
            schema.AllowGrouping,
            schema.AllowExport,
            schema.Virtualize,
            schema.Height,
            schema.RowHeight,
            schema.SearchPlaceholder,
            schema.EmptyText);
    }

    private void AddFormColumns(
        DataFormSchema<TItem> formSchema,
        List<DataGridFormColumn<TItem>> target)
    {
        foreach (DataFormField<TItem> field in formSchema.Fields
                     .OrderBy(static field => field.Order ?? int.MaxValue))
        {
            if (_properties.Contains(field.Property)
                || !field.Visible
                || field.Editor is DataFormEditor.Password or DataFormEditor.Collection)
                continue;
            DataFormPropertyPath path = field.PropertyPath;
            target.Add(CreateColumn(
                path.Path,
                field.Label ?? GetDisplayName(path.Leaf),
                path.GetValue,
                path.Leaf.PropertyType));
            _properties.Add(path.Path);
        }
    }

    private static DataGridFormColumn<TItem> CreateColumn(
        string propertyName,
        string title,
        Func<object, object?> getter,
        Type valueType)
        => new(
            propertyName,
            title,
            item => getter(item),
            item => Convert.ToString(getter(item), CultureInfo.CurrentCulture),
            null,
            null,
            true,
            true,
            false,
            InferFilterType(valueType),
            false,
            true,
            true);

    private static string GetDisplayName(PropertyInfo property)
        => property.GetCustomAttribute<DisplayAttribute>()?.GetName()
           ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
           ?? property.Name;

    private static ColumnFilterType InferFilterType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(bool)) return ColumnFilterType.Boolean;
        if (type.IsEnum) return ColumnFilterType.Select;
        if (type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return ColumnFilterType.Date;
        if (Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte
            or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
            or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double
            or TypeCode.Decimal)
            return ColumnFilterType.Number;
        return ColumnFilterType.Text;
    }

    private DataGridFormGridBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

internal interface IDataGridFormColumnBuilder<TItem> where TItem : class
{
    DataGridFormColumn<TItem> Build();
}

internal sealed class ExistingDataGridFormColumnBuilder<TItem>(DataGridFormColumn<TItem> column)
    : IDataGridFormColumnBuilder<TItem>
    where TItem : class
{
    public DataGridFormColumn<TItem> Build() => column;
}

/// <summary>Strongly typed options for one generated DataGrid column.</summary>
public sealed class DataGridFormColumnBuilder<TItem, TValue> : IDataGridFormColumnBuilder<TItem>
    where TItem : class
{
    private readonly DataFormPropertyPath _path;
    private readonly Func<TItem, TValue> _property;
    private readonly Action _ensureMutable;
    private string? _title;
    private Func<TItem, string?>? _textSelector;
    private RenderFragment<TItem>? _template;
    private string? _width;
    private bool _sortable = true;
    private bool _resizable = true;
    private bool _filterable;
    private ColumnFilterType? _filterType;
    private bool _groupable;
    private bool _canHide = true;
    private bool _visible = true;

    internal DataGridFormColumnBuilder(
        DataFormPropertyPath path,
        Func<TItem, TValue> property,
        Action ensureMutable)
    {
        _path = path;
        _property = property;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Overrides the generated column title.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Title(string? value) => Set(ref _title, value);

    /// <summary>Sets the CSS column width.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Width(string? value) => Set(ref _width, value);

    /// <summary>Sets a typed display-text selector.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Text(Func<TItem, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return Set(ref _textSelector, selector);
    }

    /// <summary>Sets a typed custom cell template.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Template(RenderFragment<TItem> template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Set(ref _template, template);
    }

    /// <summary>Enables or disables sorting.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Sortable(bool value = true) => Set(ref _sortable, value);

    /// <summary>Enables or disables resizing.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Resizable(bool value = true) => Set(ref _resizable, value);

    /// <summary>Enables filtering with an optional explicit filter family.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Filterable(
        bool value = true,
        ColumnFilterType? type = null)
    {
        _ensureMutable();
        _filterable = value;
        _filterType = type;
        return this;
    }

    /// <summary>Enables or disables grouping.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Groupable(bool value = true) => Set(ref _groupable, value);

    /// <summary>Controls the column visibility menu and initial visibility.</summary>
    public DataGridFormColumnBuilder<TItem, TValue> Visibility(
        bool visible = true,
        bool canHide = true)
    {
        _ensureMutable();
        _visible = visible;
        _canHide = canHide;
        return this;
    }

    DataGridFormColumn<TItem> IDataGridFormColumnBuilder<TItem>.Build()
        => new(
            _path.Path,
            _title ?? _path.Leaf.GetCustomAttribute<DisplayAttribute>()?.GetName()
                ?? _path.Leaf.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                ?? _path.Leaf.Name,
            item => _property(item),
            _textSelector,
            _template,
            _width,
            _sortable,
            _resizable,
            _filterable,
            _filterType ?? InferFilterType(),
            _groupable,
            _canHide,
            _visible);

    private ColumnFilterType InferFilterType()
    {
        Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if (type == typeof(bool)) return ColumnFilterType.Boolean;
        if (type.IsEnum) return ColumnFilterType.Select;
        if (type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return ColumnFilterType.Date;
        if (Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte
            or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
            or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double
            or TypeCode.Decimal)
            return ColumnFilterType.Number;
        return ColumnFilterType.Text;
    }

    private DataGridFormColumnBuilder<TItem, TValue> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Builder for the generated create workflow.</summary>
public sealed class DataGridFormCreateBuilder<TItem> where TItem : class
{
    private readonly Action _ensureMutable;
    private Func<TItem>? _factory;
    private string? _text;
    private string? _icon = "plus";
    private string? _title;
    private DataGridFormPresentation _presentation = DataGridFormPresentation.Dialog;
    private string? _width = "720px";
    private string? _authorizationPolicy;
    private DataGridFormUnauthorizedBehavior _unauthorizedBehavior;
    private Func<bool>? _visibleWhen;
    private Func<bool>? _disabledWhen;

    internal DataGridFormCreateBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    /// <summary>Sets the draft factory. Required.</summary>
    public DataGridFormCreateBuilder<TItem> Factory(Func<TItem> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Set(ref _factory, factory);
    }

    /// <summary>Sets button text and icon.</summary>
    public DataGridFormCreateBuilder<TItem> Button(string? text = null, string? icon = "plus")
    {
        _ensureMutable();
        _text = text;
        _icon = icon;
        return this;
    }

    /// <summary>Sets the editor title.</summary>
    public DataGridFormCreateBuilder<TItem> Title(string? value) => Set(ref _title, value);

    /// <summary>Sets editor placement and width.</summary>
    public DataGridFormCreateBuilder<TItem> Presentation(
        DataGridFormPresentation value,
        string? width = "720px")
    {
        _ensureMutable();
        _presentation = value;
        _width = width;
        return this;
    }

    /// <summary>Requires a named authorization policy before creation is exposed.</summary>
    public DataGridFormCreateBuilder<TItem> RequiresPolicy(
        string policy,
        DataGridFormUnauthorizedBehavior unauthorizedBehavior = DataGridFormUnauthorizedBehavior.Hide)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        _ensureMutable();
        _authorizationPolicy = policy;
        _unauthorizedBehavior = unauthorizedBehavior;
        return this;
    }

    /// <summary>Shows creation only when the policy returns true.</summary>
    public DataGridFormCreateBuilder<TItem> VisibleWhen(Func<bool> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Set(ref _visibleWhen, policy);
    }

    /// <summary>Disables creation when the policy returns true.</summary>
    public DataGridFormCreateBuilder<TItem> DisabledWhen(Func<bool> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Set(ref _disabledWhen, policy);
    }

    internal DataGridFormCreateOptions<TItem> Build()
        => new(
            _factory ?? throw new InvalidOperationException("DataGridForm create requires a draft Factory."),
            _text,
            _icon,
            _title,
            _presentation,
            _width,
            _authorizationPolicy,
            _unauthorizedBehavior,
            _visibleWhen,
            _disabledWhen);

    private DataGridFormCreateBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Builder for safe copy-based editing.</summary>
public sealed class DataGridFormEditBuilder<TItem> where TItem : class
{
    private readonly Action _ensureMutable;
    private Func<TItem, TItem>? _clone;
    private Func<TItem, string?>? _title;
    private string? _text;
    private string? _icon = "edit";
    private DataGridFormPresentation _presentation = DataGridFormPresentation.Dialog;
    private string? _width = "720px";
    private DataGridFormActionPlacement _placement = DataGridFormActionPlacement.Auto;
    private int _priority = 100;
    private string? _group;
    private string? _shortcut;
    private string? _description;
    private string? _authorizationPolicy;
    private DataGridFormUnauthorizedBehavior _unauthorizedBehavior;
    private Func<TItem, bool>? _visibleWhen;
    private Func<TItem, bool>? _disabledWhen;

    internal DataGridFormEditBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    /// <summary>Sets the required detached-draft factory used to make cancellation safe.</summary>
    public DataGridFormEditBuilder<TItem> Clone(Func<TItem, TItem> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Set(ref _clone, factory);
    }

    /// <summary>Sets a title derived from the selected item.</summary>
    public DataGridFormEditBuilder<TItem> Title(Func<TItem, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return Set(ref _title, selector);
    }

    /// <summary>Sets button text and icon.</summary>
    public DataGridFormEditBuilder<TItem> Button(string? text = null, string? icon = "edit")
    {
        _ensureMutable();
        _text = text;
        _icon = icon;
        return this;
    }

    /// <summary>Sets editor placement and width.</summary>
    public DataGridFormEditBuilder<TItem> Presentation(
        DataGridFormPresentation value,
        string? width = "720px")
    {
        _ensureMutable();
        _presentation = value;
        _width = width;
        return this;
    }

    /// <summary>Sets whether the edit action is rendered inline or in the row overflow menu.</summary>
    public DataGridFormEditBuilder<TItem> Placement(DataGridFormActionPlacement value)
        => Set(ref _placement, value);

    /// <summary>Moves the edit action to the row overflow menu when enabled.</summary>
    public DataGridFormEditBuilder<TItem> InMenu(bool enabled = true)
        => Placement(enabled ? DataGridFormActionPlacement.Menu : DataGridFormActionPlacement.Auto);

    /// <summary>Pins the edit action inline even when automatic overflow is enabled.</summary>
    public DataGridFormEditBuilder<TItem> KeepInline()
        => Placement(DataGridFormActionPlacement.Inline);

    /// <summary>Lets the actions-column overflow policy place the edit action.</summary>
    public DataGridFormEditBuilder<TItem> Auto()
        => Placement(DataGridFormActionPlacement.Auto);

    /// <summary>Sets automatic-overflow priority; larger values stay inline first.</summary>
    public DataGridFormEditBuilder<TItem> Priority(int value) => Set(ref _priority, value);

    /// <summary>Sets optional overflow-menu grouping, shortcut and description metadata.</summary>
    public DataGridFormEditBuilder<TItem> MenuMetadata(
        string? group = null,
        string? shortcut = null,
        string? description = null)
    {
        _ensureMutable();
        _group = group;
        _shortcut = shortcut;
        _description = description;
        return this;
    }

    /// <summary>Requires a named authorization policy before editing is exposed.</summary>
    public DataGridFormEditBuilder<TItem> RequiresPolicy(
        string policy,
        DataGridFormUnauthorizedBehavior unauthorizedBehavior = DataGridFormUnauthorizedBehavior.Hide)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        _ensureMutable();
        _authorizationPolicy = policy;
        _unauthorizedBehavior = unauthorizedBehavior;
        return this;
    }

    /// <summary>Shows edit only for rows accepted by the policy.</summary>
    public DataGridFormEditBuilder<TItem> VisibleWhen(Func<TItem, bool> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Set(ref _visibleWhen, policy);
    }

    /// <summary>Disables edit for rows accepted by the policy.</summary>
    public DataGridFormEditBuilder<TItem> DisabledWhen(Func<TItem, bool> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Set(ref _disabledWhen, policy);
    }

    internal DataGridFormEditOptions<TItem> Build()
        => new(
            _clone ?? throw new InvalidOperationException(
                "DataGridForm edit requires Clone so cancellation never mutates the live row."),
            _title,
            _text,
            _icon,
            _presentation,
            _width,
            _placement,
            _priority,
            _group,
            _shortcut,
            _description,
            _authorizationPolicy,
            _unauthorizedBehavior,
            _visibleWhen,
            _disabledWhen);

    private DataGridFormEditBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Builder for confirmed deletion.</summary>
public sealed class DataGridFormDeleteBuilder<TItem> where TItem : class
{
    private readonly Action _ensureMutable;
    private Func<TItem, string?>? _confirmation;
    private Func<TItem, string?>? _title;
    private string? _text;
    private string? _icon = "trash";
    private string? _confirmText;
    private string? _cancelText;
    private DataGridFormActionPlacement _placement = DataGridFormActionPlacement.Auto;
    private int _priority;
    private string? _group;
    private string? _shortcut;
    private string? _description;
    private string? _authorizationPolicy;
    private DataGridFormUnauthorizedBehavior _unauthorizedBehavior;
    private Func<TItem, bool>? _visibleWhen;
    private Func<TItem, bool>? _disabledWhen;

    internal DataGridFormDeleteBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    /// <summary>Sets confirmation message and optional title selectors.</summary>
    public DataGridFormDeleteBuilder<TItem> Confirm(
        Func<TItem, string?> message,
        Func<TItem, string?>? title = null)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(message);
        _confirmation = message;
        _title = title;
        return this;
    }

    /// <summary>Sets button text and icon.</summary>
    public DataGridFormDeleteBuilder<TItem> Button(string? text = null, string? icon = "trash")
    {
        _ensureMutable();
        _text = text;
        _icon = icon;
        return this;
    }

    /// <summary>Sets confirmation and cancellation button labels.</summary>
    public DataGridFormDeleteBuilder<TItem> Texts(string? confirm = null, string? cancel = null)
    {
        _ensureMutable();
        _confirmText = confirm;
        _cancelText = cancel;
        return this;
    }

    /// <summary>Sets whether the delete action is rendered inline or in the row overflow menu.</summary>
    public DataGridFormDeleteBuilder<TItem> Placement(DataGridFormActionPlacement value)
        => Set(ref _placement, value);

    /// <summary>Moves the delete action to the row overflow menu when enabled.</summary>
    public DataGridFormDeleteBuilder<TItem> InMenu(bool enabled = true)
        => Placement(enabled ? DataGridFormActionPlacement.Menu : DataGridFormActionPlacement.Auto);

    /// <summary>Pins the delete action inline even when automatic overflow is enabled.</summary>
    public DataGridFormDeleteBuilder<TItem> KeepInline()
        => Placement(DataGridFormActionPlacement.Inline);

    /// <summary>Lets the actions-column overflow policy place the delete action.</summary>
    public DataGridFormDeleteBuilder<TItem> Auto()
        => Placement(DataGridFormActionPlacement.Auto);

    /// <summary>Sets automatic-overflow priority; larger values stay inline first.</summary>
    public DataGridFormDeleteBuilder<TItem> Priority(int value) => Set(ref _priority, value);

    /// <summary>Sets optional overflow-menu grouping, shortcut and description metadata.</summary>
    public DataGridFormDeleteBuilder<TItem> MenuMetadata(
        string? group = null,
        string? shortcut = null,
        string? description = null)
    {
        _ensureMutable();
        _group = group;
        _shortcut = shortcut;
        _description = description;
        return this;
    }

    /// <summary>Requires a named authorization policy before deletion is exposed.</summary>
    public DataGridFormDeleteBuilder<TItem> RequiresPolicy(
        string policy,
        DataGridFormUnauthorizedBehavior unauthorizedBehavior = DataGridFormUnauthorizedBehavior.Hide)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        _ensureMutable();
        _authorizationPolicy = policy;
        _unauthorizedBehavior = unauthorizedBehavior;
        return this;
    }

    /// <summary>Shows delete only for rows accepted by the policy.</summary>
    public DataGridFormDeleteBuilder<TItem> VisibleWhen(Func<TItem, bool> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Set(ref _visibleWhen, policy);
    }

    /// <summary>Disables delete for rows accepted by the policy.</summary>
    public DataGridFormDeleteBuilder<TItem> DisabledWhen(Func<TItem, bool> policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return Set(ref _disabledWhen, policy);
    }

    internal DataGridFormDeleteOptions<TItem> Build()
        => new(
            _confirmation,
            _title,
            _text,
            _icon,
            _confirmText,
            _cancelText,
            _placement,
            _priority,
            _group,
            _shortcut,
            _description,
            _authorizationPolicy,
            _unauthorizedBehavior,
            _visibleWhen,
            _disabledWhen);

    private DataGridFormDeleteBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Builder for a custom row action.</summary>
public sealed class DataGridFormActionBuilder<TItem> where TItem : class
{
    private readonly Action _ensureMutable;
    private string _id;
    private string _text;
    private string? _icon;
    private ButtonVariant _variant = ButtonVariant.Ghost;
    private DataGridFormActionPlacement _placement = DataGridFormActionPlacement.Auto;
    private int _priority = 50;
    private int _order = 100;
    private string? _group;
    private string? _shortcut;
    private string? _description;
    private string? _authorizationPolicy;
    private DataGridFormUnauthorizedBehavior _unauthorizedBehavior;
    private Func<TItem, bool>? _visibleWhen;
    private Func<TItem, bool>? _disabledWhen;
    private Func<TItem, string?>? _confirmation;

    internal DataGridFormActionBuilder(string text, string? icon, Action ensureMutable)
    {
        _id = text;
        _text = text;
        _icon = icon;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the stable action id used by operation events and busy state.</summary>
    public DataGridFormActionBuilder<TItem> Id(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _id, value);
    }

    /// <summary>Overrides display text and icon.</summary>
    public DataGridFormActionBuilder<TItem> Display(string text, string? icon = null)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _text = text;
        _icon = icon;
        return this;
    }

    /// <summary>Sets the button visual variant.</summary>
    public DataGridFormActionBuilder<TItem> Variant(ButtonVariant value) => Set(ref _variant, value);

    /// <summary>Sets whether the action is rendered inline or in the row overflow menu.</summary>
    public DataGridFormActionBuilder<TItem> Placement(DataGridFormActionPlacement value)
        => Set(ref _placement, value);

    /// <summary>Moves the action to the row overflow menu when enabled.</summary>
    public DataGridFormActionBuilder<TItem> InMenu(bool enabled = true)
        => Placement(enabled ? DataGridFormActionPlacement.Menu : DataGridFormActionPlacement.Auto);

    /// <summary>Pins the action inline even when automatic overflow is enabled.</summary>
    public DataGridFormActionBuilder<TItem> KeepInline()
        => Placement(DataGridFormActionPlacement.Inline);

    /// <summary>Lets the actions-column overflow policy place the action.</summary>
    public DataGridFormActionBuilder<TItem> Auto()
        => Placement(DataGridFormActionPlacement.Auto);

    /// <summary>Sets automatic-overflow priority; larger values stay inline first.</summary>
    public DataGridFormActionBuilder<TItem> Priority(int value) => Set(ref _priority, value);

    /// <summary>Sets deterministic display order among custom row actions.</summary>
    public DataGridFormActionBuilder<TItem> Order(int value) => Set(ref _order, value);

    /// <summary>Sets optional overflow-menu grouping, shortcut and description metadata.</summary>
    public DataGridFormActionBuilder<TItem> MenuMetadata(
        string? group = null,
        string? shortcut = null,
        string? description = null)
    {
        _ensureMutable();
        _group = group;
        _shortcut = shortcut;
        _description = description;
        return this;
    }

    /// <summary>Requires a named authorization policy before the action is exposed.</summary>
    public DataGridFormActionBuilder<TItem> RequiresPolicy(
        string policy,
        DataGridFormUnauthorizedBehavior unauthorizedBehavior = DataGridFormUnauthorizedBehavior.Hide)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        _ensureMutable();
        _authorizationPolicy = policy;
        _unauthorizedBehavior = unauthorizedBehavior;
        return this;
    }

    /// <summary>Shows the action only when the predicate returns true.</summary>
    public DataGridFormActionBuilder<TItem> VisibleWhen(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _visibleWhen, predicate);
    }

    /// <summary>Disables the action when the predicate returns true.</summary>
    public DataGridFormActionBuilder<TItem> DisabledWhen(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _disabledWhen, predicate);
    }

    /// <summary>Requires confirmation using a message derived from the row.</summary>
    public DataGridFormActionBuilder<TItem> Confirm(Func<TItem, string?> message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Set(ref _confirmation, message);
    }

    internal DataGridFormAction<TItem, TKey> Build<TKey>(
        Func<DataGridFormActionContext<TItem, TKey>, CancellationToken, ValueTask> execute)
        where TKey : notnull
        => new(
            _id,
            _text,
            _icon,
            _variant,
            _placement,
            _priority,
            _order,
            _group,
            _shortcut,
            _description,
            _authorizationPolicy,
            _unauthorizedBehavior,
            _visibleWhen,
            _disabledWhen,
            _confirmation,
            execute);

    private DataGridFormActionBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Builder for a custom selected-item action.</summary>
public sealed class DataGridFormBulkActionBuilder<TItem> where TItem : class
{
    private readonly Action _ensureMutable;
    private string _id;
    private string _text;
    private string? _icon;
    private ButtonVariant _variant = ButtonVariant.Default;
    private DataGridFormActionPlacement _placement = DataGridFormActionPlacement.Auto;
    private int _priority = 50;
    private int _order = 100;
    private string? _group;
    private string? _shortcut;
    private string? _description;
    private string? _authorizationPolicy;
    private DataGridFormUnauthorizedBehavior _unauthorizedBehavior;
    private Func<IReadOnlyList<TItem>, bool>? _visibleWhen;
    private Func<IReadOnlyList<TItem>, bool>? _disabledWhen;
    private Func<IReadOnlyList<TItem>, string?>? _confirmation;

    internal DataGridFormBulkActionBuilder(string text, string? icon, Action ensureMutable)
    {
        _id = text;
        _text = text;
        _icon = icon;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the stable action id used by operation events and busy state.</summary>
    public DataGridFormBulkActionBuilder<TItem> Id(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _id, value);
    }

    /// <summary>Overrides display text and icon.</summary>
    public DataGridFormBulkActionBuilder<TItem> Display(string text, string? icon = null)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _text = text;
        _icon = icon;
        return this;
    }

    /// <summary>Sets the button visual variant.</summary>
    public DataGridFormBulkActionBuilder<TItem> Variant(ButtonVariant value) => Set(ref _variant, value);

    /// <summary>Sets whether the selected-item action is inline, in-menu or automatic.</summary>
    public DataGridFormBulkActionBuilder<TItem> Placement(DataGridFormActionPlacement value)
        => Set(ref _placement, value);

    /// <summary>Moves the selected-item action to the overflow menu when enabled.</summary>
    public DataGridFormBulkActionBuilder<TItem> InMenu(bool enabled = true)
        => Placement(enabled ? DataGridFormActionPlacement.Menu : DataGridFormActionPlacement.Auto);

    /// <summary>Pins the selected-item action inline.</summary>
    public DataGridFormBulkActionBuilder<TItem> KeepInline()
        => Placement(DataGridFormActionPlacement.Inline);

    /// <summary>Lets the selected-item overflow policy place the action.</summary>
    public DataGridFormBulkActionBuilder<TItem> Auto()
        => Placement(DataGridFormActionPlacement.Auto);

    /// <summary>Sets automatic-overflow priority; larger values stay inline first.</summary>
    public DataGridFormBulkActionBuilder<TItem> Priority(int value) => Set(ref _priority, value);

    /// <summary>Sets deterministic display order among selected-item actions.</summary>
    public DataGridFormBulkActionBuilder<TItem> Order(int value) => Set(ref _order, value);

    /// <summary>Sets optional overflow-menu grouping, shortcut and description metadata.</summary>
    public DataGridFormBulkActionBuilder<TItem> MenuMetadata(
        string? group = null,
        string? shortcut = null,
        string? description = null)
    {
        _ensureMutable();
        _group = group;
        _shortcut = shortcut;
        _description = description;
        return this;
    }

    /// <summary>Requires a named authorization policy before the action is exposed.</summary>
    public DataGridFormBulkActionBuilder<TItem> RequiresPolicy(
        string policy,
        DataGridFormUnauthorizedBehavior unauthorizedBehavior = DataGridFormUnauthorizedBehavior.Hide)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        _ensureMutable();
        _authorizationPolicy = policy;
        _unauthorizedBehavior = unauthorizedBehavior;
        return this;
    }

    /// <summary>Shows the action only when the selected snapshot is accepted.</summary>
    public DataGridFormBulkActionBuilder<TItem> VisibleWhen(Func<IReadOnlyList<TItem>, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _visibleWhen, predicate);
    }

    /// <summary>Disables the action when the selected snapshot is rejected.</summary>
    public DataGridFormBulkActionBuilder<TItem> DisabledWhen(Func<IReadOnlyList<TItem>, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _disabledWhen, predicate);
    }

    /// <summary>Requires confirmation using a message derived from the selected snapshot.</summary>
    public DataGridFormBulkActionBuilder<TItem> Confirm(Func<IReadOnlyList<TItem>, string?> message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Set(ref _confirmation, message);
    }

    internal DataGridFormBulkAction<TItem, TKey> Build<TKey>(
        Func<DataGridFormBulkActionContext<TItem, TKey>, CancellationToken, ValueTask> execute)
        where TKey : notnull
        => new(
            _id,
            _text,
            _icon,
            _variant,
            _placement,
            _priority,
            _order,
            _group,
            _shortcut,
            _description,
            _authorizationPolicy,
            _unauthorizedBehavior,
            _visibleWhen,
            _disabledWhen,
            _confirmation,
            execute);

    private DataGridFormBulkActionBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}
