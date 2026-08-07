using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Immutable, strongly typed structural configuration for an OmniDataGrid.</summary>
public sealed class DataGridSchema<TItem>
{
    internal DataGridSchema(
        IReadOnlyList<DataGridColumnDefinition<TItem>> columns,
        Func<TItem, object>? keySelector,
        DataGridHierarchySchema<TItem>? hierarchy,
        bool allowSearch,
        bool allowSorting,
        bool allowPaging,
        int pageSize,
        bool allowColumnFilter,
        bool allowColumnResize,
        bool allowColumnVisibility,
        bool allowGrouping,
        bool allowExport,
        bool allowMultiSelection,
        bool virtualize,
        string? height,
        float rowHeight,
        int overscanCount,
        string? searchPlaceholder,
        string? emptyText)
    {
        Columns = columns;
        KeySelector = keySelector;
        Hierarchy = hierarchy;
        AllowSearch = allowSearch;
        AllowSorting = allowSorting;
        AllowPaging = allowPaging;
        PageSize = pageSize;
        AllowColumnFilter = allowColumnFilter;
        AllowColumnResize = allowColumnResize;
        AllowColumnVisibility = allowColumnVisibility;
        AllowGrouping = allowGrouping;
        AllowExport = allowExport;
        AllowMultiSelection = allowMultiSelection;
        Virtualize = virtualize;
        Height = height;
        RowHeight = rowHeight;
        OverscanCount = overscanCount;
        SearchPlaceholder = searchPlaceholder;
        EmptyText = emptyText;
    }

    /// <summary>Immutable generated column definitions.</summary>
    public IReadOnlyList<DataGridColumnDefinition<TItem>> Columns { get; }

    /// <summary>Stable item key used by hierarchy state.</summary>
    public Func<TItem, object>? KeySelector { get; }

    /// <summary>Optional bounded hierarchy configuration.</summary>
    public DataGridHierarchySchema<TItem>? Hierarchy { get; }

    /// <summary>Whether search is enabled.</summary>
    public bool AllowSearch { get; }

    /// <summary>Whether sorting is enabled.</summary>
    public bool AllowSorting { get; }

    /// <summary>Whether paging is enabled.</summary>
    public bool AllowPaging { get; }

    /// <summary>Default page size.</summary>
    public int PageSize { get; }

    /// <summary>Whether per-column filters are enabled.</summary>
    public bool AllowColumnFilter { get; }

    /// <summary>Whether columns can be resized.</summary>
    public bool AllowColumnResize { get; }

    /// <summary>Whether column visibility can be customized.</summary>
    public bool AllowColumnVisibility { get; }

    /// <summary>Whether grouping is enabled.</summary>
    public bool AllowGrouping { get; }

    /// <summary>Whether bounded CSV export is enabled.</summary>
    public bool AllowExport { get; }

    /// <summary>Whether multi-selection is enabled.</summary>
    public bool AllowMultiSelection { get; }

    /// <summary>Whether row virtualization is enabled.</summary>
    public bool Virtualize { get; }

    /// <summary>Virtualized scroller height.</summary>
    public string? Height { get; }

    /// <summary>Fixed virtualized row height.</summary>
    public float RowHeight { get; }

    /// <summary>Virtualization overscan row count.</summary>
    public int OverscanCount { get; }

    /// <summary>Optional search placeholder.</summary>
    public string? SearchPlaceholder { get; }

    /// <summary>Optional empty-state text.</summary>
    public string? EmptyText { get; }

    /// <summary>Creates an immutable schema through a strongly typed builder.</summary>
    public static DataGridSchema<TItem> Create(Action<DataGridSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataGridSchemaBuilder<TItem> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot schema builder.</summary>
    public static DataGridSchemaBuilder<TItem> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public DataGridSchema<TItem> Extend(Action<DataGridSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataGridSchemaBuilder<TItem> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Strongly typed one-shot builder for DataGrid structure and behavior defaults.</summary>
public sealed class DataGridSchemaBuilder<TItem>
{
    private readonly List<IDataGridColumnDefinitionBuilder<TItem>> _columns = [];
    private readonly HashSet<string> _properties = new(StringComparer.Ordinal);
    private Func<TItem, object>? _keySelector;
    private DataGridHierarchySchema<TItem>? _hierarchy;
    private bool _allowSearch;
    private bool _allowSorting = true;
    private bool _allowPaging = true;
    private int _pageSize = 10;
    private bool _allowColumnFilter;
    private bool _allowColumnResize;
    private bool _allowColumnVisibility;
    private bool _allowGrouping;
    private bool _allowExport;
    private bool _allowMultiSelection;
    private bool _virtualize;
    private string? _height;
    private float _rowHeight = 44;
    private int _overscanCount = 4;
    private string? _searchPlaceholder;
    private string? _emptyText;
    private bool _built;

    /// <summary>Includes columns and defaults from an immutable schema.</summary>
    public DataGridSchemaBuilder<TItem> Include(DataGridSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        foreach (DataGridColumnDefinition<TItem> column in schema.Columns)
        {
            if (!_properties.Add(column.PropertyName))
                throw new InvalidOperationException($"DataGrid column '{column.PropertyName}' was declared more than once.");
            _columns.Add(new ExistingDataGridColumnDefinitionBuilder<TItem>(column));
        }
        _keySelector = schema.KeySelector;
        _hierarchy = schema.Hierarchy;
        _allowSearch = schema.AllowSearch;
        _allowSorting = schema.AllowSorting;
        _allowPaging = schema.AllowPaging;
        _pageSize = schema.PageSize;
        _allowColumnFilter = schema.AllowColumnFilter;
        _allowColumnResize = schema.AllowColumnResize;
        _allowColumnVisibility = schema.AllowColumnVisibility;
        _allowGrouping = schema.AllowGrouping;
        _allowExport = schema.AllowExport;
        _allowMultiSelection = schema.AllowMultiSelection;
        _virtualize = schema.Virtualize;
        _height = schema.Height;
        _rowHeight = schema.RowHeight;
        _overscanCount = schema.OverscanCount;
        _searchPlaceholder = schema.SearchPlaceholder;
        _emptyText = schema.EmptyText;
        return this;
    }

    /// <summary>Adds a compile-time checked column.</summary>
    public DataGridSchemaBuilder<TItem> Column<TValue>(
        Expression<Func<TItem, TValue>> property,
        Action<DataGridColumnBuilder<TItem, TValue>>? configure = null)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(property);
        DataGridMemberPath path = DataGridMemberPath.Resolve(property);
        if (!_properties.Add(path.Path))
            throw new InvalidOperationException($"DataGrid column '{path.Path}' was declared more than once.");
        DataGridColumnBuilder<TItem, TValue> builder = new(path, property.Compile(), EnsureMutable);
        configure?.Invoke(builder);
        _columns.Add(builder);
        return this;
    }

    /// <summary>Replaces a previously included or declared column while preserving its position.</summary>
    public DataGridSchemaBuilder<TItem> OverrideColumn<TValue>(
        Expression<Func<TItem, TValue>> property,
        Action<DataGridColumnBuilder<TItem, TValue>>? configure = null)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(property);
        DataGridMemberPath path = DataGridMemberPath.Resolve(property);
        int index = _columns.FindIndex(column => string.Equals(column.PropertyName, path.Path, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"DataGrid column '{path.Path}' cannot be overridden because it is not declared.");
        DataGridColumnBuilder<TItem, TValue> builder = new(path, property.Compile(), EnsureMutable);
        configure?.Invoke(builder);
        _columns[index] = builder;
        return this;
    }

    /// <summary>Removes all generated columns so a derived schema can declare a new projection.</summary>
    public DataGridSchemaBuilder<TItem> ClearColumns()
    {
        EnsureMutable();
        _columns.Clear();
        _properties.Clear();
        return this;
    }

    /// <summary>Sets the stable key used by hierarchy and expansion state.</summary>
    public DataGridSchemaBuilder<TItem> Key<TKey>(Expression<Func<TItem, TKey>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        Func<TItem, TKey> compiled = selector.Compile();
        _keySelector = item => compiled(item)!;
        return this;
    }

    /// <summary>Enables an in-memory hierarchy with bounded flattening and caching defaults.</summary>
    public DataGridSchemaBuilder<TItem> Hierarchy<TKey>(
        Expression<Func<TItem, TKey>> key,
        Func<TItem, IEnumerable<TItem>?> children,
        Action<DataGridHierarchyBuilder<TItem>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(children);
        SetKey(key);
        DataGridHierarchyBuilder<TItem> builder = new DataGridHierarchyBuilder<TItem>(EnsureMutable).Children(children);
        configure?.Invoke(builder);
        _hierarchy = builder.Build();
        return this;
    }

    /// <summary>Enables a cancellable lazy hierarchy with bounded concurrency and caches.</summary>
    public DataGridSchemaBuilder<TItem> LazyHierarchy<TKey>(
        Expression<Func<TItem, TKey>> key,
        HierarchyChildrenProvider<TItem> provider,
        Action<DataGridHierarchyBuilder<TItem>>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        SetKey(key);
        DataGridHierarchyBuilder<TItem> builder = new DataGridHierarchyBuilder<TItem>(EnsureMutable).Provider(provider);
        configure?.Invoke(builder);
        _hierarchy = builder.Build();
        return this;
    }

    /// <summary>Removes inherited hierarchy behavior while preserving the stable key.</summary>
    public DataGridSchemaBuilder<TItem> ClearHierarchy()
    {
        EnsureMutable();
        _hierarchy = null;
        return this;
    }

    /// <summary>Enables or disables search.</summary>
    public DataGridSchemaBuilder<TItem> Search(bool enabled = true, string? placeholder = null)
    {
        EnsureMutable();
        _allowSearch = enabled;
        _searchPlaceholder = placeholder;
        return this;
    }

    /// <summary>Enables or disables sorting.</summary>
    public DataGridSchemaBuilder<TItem> Sorting(bool enabled = true) => Set(ref _allowSorting, enabled);

    /// <summary>Enables paging and sets its bounded page size.</summary>
    public DataGridSchemaBuilder<TItem> Paging(int pageSize = 10, bool enabled = true)
    {
        EnsureMutable();
        if (pageSize is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(pageSize));
        _allowPaging = enabled;
        _pageSize = pageSize;
        if (enabled) _virtualize = false;
        return this;
    }

    /// <summary>Enables or disables the per-column filter row.</summary>
    public DataGridSchemaBuilder<TItem> ColumnFilter(bool enabled = true) => Set(ref _allowColumnFilter, enabled);

    /// <summary>Enables or disables column resizing.</summary>
    public DataGridSchemaBuilder<TItem> ColumnResize(bool enabled = true) => Set(ref _allowColumnResize, enabled);

    /// <summary>Enables or disables the column visibility menu.</summary>
    public DataGridSchemaBuilder<TItem> ColumnVisibility(bool enabled = true) => Set(ref _allowColumnVisibility, enabled);

    /// <summary>Enables or disables row grouping.</summary>
    public DataGridSchemaBuilder<TItem> Grouping(bool enabled = true) => Set(ref _allowGrouping, enabled);

    /// <summary>Enables or disables bounded CSV export.</summary>
    public DataGridSchemaBuilder<TItem> Export(bool enabled = true) => Set(ref _allowExport, enabled);

    /// <summary>Enables or disables multi-selection.</summary>
    public DataGridSchemaBuilder<TItem> MultiSelection(bool enabled = true) => Set(ref _allowMultiSelection, enabled);

    /// <summary>Enables fixed-row virtualization and disables paging.</summary>
    public DataGridSchemaBuilder<TItem> Virtualization(
        string height = "520px",
        float rowHeight = 44,
        int overscanCount = 4)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(height);
        if (!float.IsFinite(rowHeight) || rowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        if (overscanCount is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(overscanCount));
        _virtualize = true;
        _allowPaging = false;
        _height = height;
        _rowHeight = rowHeight;
        _overscanCount = overscanCount;
        return this;
    }

    /// <summary>Overrides empty-state text.</summary>
    public DataGridSchemaBuilder<TItem> EmptyText(string? text) => Set(ref _emptyText, text);

    /// <summary>Builds the immutable schema.</summary>
    public DataGridSchema<TItem> Build()
    {
        EnsureMutable();
        _built = true;
        if (_columns.Count == 0) throw new InvalidOperationException("DataGrid schema requires at least one column.");
        if (_hierarchy is not null && _keySelector is null)
            throw new InvalidOperationException("DataGrid hierarchy requires a stable key selector.");
        DataGridColumnDefinition<TItem>[] columns = new DataGridColumnDefinition<TItem>[_columns.Count];
        for (int index = 0; index < columns.Length; index++) columns[index] = _columns[index].Build();
        return new DataGridSchema<TItem>(
            Array.AsReadOnly(columns), _keySelector, _hierarchy,
            _allowSearch, _allowSorting, _allowPaging, _pageSize,
            _allowColumnFilter, _allowColumnResize, _allowColumnVisibility,
            _allowGrouping, _allowExport, _allowMultiSelection,
            _virtualize, _height, _rowHeight, _overscanCount,
            _searchPlaceholder, _emptyText);
    }

    private void SetKey<TKey>(Expression<Func<TItem, TKey>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        Func<TItem, TKey> compiled = selector.Compile();
        _keySelector = item => compiled(item)!;
    }

    private DataGridSchemaBuilder<TItem> Set<T>(ref T target, T value)
    {
        EnsureMutable();
        target = value;
        return this;
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("DataGrid schema is immutable after Build().");
    }
}

/// <summary>Fluent builder for bounded DataGrid hierarchy behavior.</summary>
public sealed class DataGridHierarchyBuilder<TItem>
{
    private readonly Action _ensureMutable;
    private Func<TItem, IEnumerable<TItem>?>? _children;
    private Func<TItem, bool>? _hasChildren;
    private HierarchyChildrenProvider<TItem>? _provider;
    private Func<TItem, bool>? _initiallyExpanded;
    private int _indentSize = 20;
    private int _maxChildrenPerNode = 1_000;
    private int _maxCachedNodes = 500;
    private int _maxCachedItems = 10_000;
    private int _maxVisibleRows = 5_000;
    private int _maxDepth = 64;
    private int _maxConcurrentLoads = 4;

    internal DataGridHierarchyBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    /// <summary>Sets an optional cheap child-existence predicate.</summary>
    public DataGridHierarchyBuilder<TItem> HasChildren(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _hasChildren, predicate);
    }

    /// <summary>Sets an optional initial expansion predicate.</summary>
    public DataGridHierarchyBuilder<TItem> InitiallyExpanded(Func<TItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Set(ref _initiallyExpanded, predicate);
    }

    /// <summary>Sets indentation in CSS pixels.</summary>
    public DataGridHierarchyBuilder<TItem> Indent(int pixels)
    {
        if (pixels is < 0 or > 256) throw new ArgumentOutOfRangeException(nameof(pixels));
        return Set(ref _indentSize, pixels);
    }

    /// <summary>Sets hard safety bounds for traversal, caches and concurrent lazy loads.</summary>
    public DataGridHierarchyBuilder<TItem> Limits(
        int maximumDepth = 64,
        int maximumVisibleRows = 5_000,
        int maximumChildrenPerNode = 1_000,
        int maximumCachedNodes = 500,
        int maximumCachedItems = 10_000,
        int maximumConcurrentLoads = 4)
    {
        _ensureMutable();
        if (maximumDepth is < 1 or > 512) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        if (maximumVisibleRows < 1) throw new ArgumentOutOfRangeException(nameof(maximumVisibleRows));
        if (maximumChildrenPerNode < 1) throw new ArgumentOutOfRangeException(nameof(maximumChildrenPerNode));
        if (maximumCachedNodes < 1) throw new ArgumentOutOfRangeException(nameof(maximumCachedNodes));
        if (maximumCachedItems < maximumChildrenPerNode) throw new ArgumentOutOfRangeException(nameof(maximumCachedItems));
        if (maximumConcurrentLoads is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(maximumConcurrentLoads));
        _maxDepth = maximumDepth;
        _maxVisibleRows = maximumVisibleRows;
        _maxChildrenPerNode = maximumChildrenPerNode;
        _maxCachedNodes = maximumCachedNodes;
        _maxCachedItems = maximumCachedItems;
        _maxConcurrentLoads = maximumConcurrentLoads;
        return this;
    }

    internal DataGridHierarchyBuilder<TItem> Children(Func<TItem, IEnumerable<TItem>?> selector)
        => Set(ref _children, selector);

    internal DataGridHierarchyBuilder<TItem> Provider(HierarchyChildrenProvider<TItem> provider)
        => Set(ref _provider, provider);

    internal DataGridHierarchySchema<TItem> Build()
        => new(
            _children, _hasChildren, _provider, _initiallyExpanded,
            _indentSize, _maxChildrenPerNode, _maxCachedNodes, _maxCachedItems,
            _maxVisibleRows, _maxDepth, _maxConcurrentLoads);

    private DataGridHierarchyBuilder<TItem> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

/// <summary>Immutable hierarchy portion of a DataGrid schema.</summary>
public sealed record DataGridHierarchySchema<TItem>(
    Func<TItem, IEnumerable<TItem>?>? Children,
    Func<TItem, bool>? HasChildren,
    HierarchyChildrenProvider<TItem>? ChildrenProvider,
    Func<TItem, bool>? InitiallyExpanded,
    int IndentSize,
    int MaxChildrenPerNode,
    int MaxCachedNodes,
    int MaxCachedItems,
    int MaxVisibleRows,
    int MaxDepth,
    int MaxConcurrentLoads);

/// <summary>Immutable runtime definition for one generated DataGrid column.</summary>
public sealed class DataGridColumnDefinition<TItem>
{
    internal DataGridColumnDefinition(
        string propertyName,
        string title,
        Func<TItem, object?> property,
        Func<TItem, string?>? textSelector,
        RenderFragment<TItem>? template,
        RenderFragment<TItem>? editTemplate,
        string? width,
        bool sortable,
        bool resizable,
        bool canHide,
        bool visible,
        bool filterable,
        ColumnFilterType filterType,
        IReadOnlyList<FilterOperator>? filterOperators,
        IReadOnlyList<object>? filterSelectOptions,
        AggregateFunction? aggregate,
        Func<TItem, decimal>? aggregateProperty,
        Func<object?, string>? aggregateFormat,
        bool groupable,
        IReadOnlyList<DateGroupInterval>? groupHierarchy,
        FrozenPosition? frozen,
        bool isHierarchyAnchor)
    {
        PropertyName = propertyName;
        Title = title;
        Property = property;
        TextSelector = textSelector;
        Template = template;
        EditTemplate = editTemplate;
        Width = width;
        Sortable = sortable;
        Resizable = resizable;
        CanHide = canHide;
        Visible = visible;
        Filterable = filterable;
        FilterType = filterType;
        FilterOperators = filterOperators;
        FilterSelectOptions = filterSelectOptions;
        Aggregate = aggregate;
        AggregateProperty = aggregateProperty;
        AggregateFormat = aggregateFormat;
        Groupable = groupable;
        GroupHierarchy = groupHierarchy;
        Frozen = frozen;
        IsHierarchyAnchor = isHierarchyAnchor;
    }

    /// <summary>Stable member path used by sorting, filtering and persisted view state.</summary>
    public string PropertyName { get; }

    /// <summary>Column header text.</summary>
    public string Title { get; }

    /// <summary>Compiled boxed value selector.</summary>
    public Func<TItem, object?> Property { get; }

    /// <summary>Optional display text selector.</summary>
    public Func<TItem, string?>? TextSelector { get; }

    /// <summary>Optional cell template.</summary>
    public RenderFragment<TItem>? Template { get; }

    /// <summary>Optional inline edit template.</summary>
    public RenderFragment<TItem>? EditTemplate { get; }

    /// <summary>CSS width.</summary>
    public string? Width { get; }

    /// <summary>Whether sorting is allowed.</summary>
    public bool Sortable { get; }

    /// <summary>Whether resizing is allowed.</summary>
    public bool Resizable { get; }

    /// <summary>Whether the column can be hidden.</summary>
    public bool CanHide { get; }

    /// <summary>Initial visibility.</summary>
    public bool Visible { get; }

    /// <summary>Whether filtering is allowed.</summary>
    public bool Filterable { get; }

    /// <summary>Filter editor family.</summary>
    public ColumnFilterType FilterType { get; }

    /// <summary>Optional allowed filter operators.</summary>
    public IReadOnlyList<FilterOperator>? FilterOperators { get; }

    /// <summary>Optional select filter options.</summary>
    public IReadOnlyList<object>? FilterSelectOptions { get; }

    /// <summary>Optional aggregate function.</summary>
    public AggregateFunction? Aggregate { get; }

    /// <summary>Optional aggregate numeric selector.</summary>
    public Func<TItem, decimal>? AggregateProperty { get; }

    /// <summary>Optional aggregate formatter.</summary>
    public Func<object?, string>? AggregateFormat { get; }

    /// <summary>Whether grouping is allowed.</summary>
    public bool Groupable { get; }

    /// <summary>Optional date grouping hierarchy.</summary>
    public IReadOnlyList<DateGroupInterval>? GroupHierarchy { get; }

    /// <summary>Optional frozen edge.</summary>
    public FrozenPosition? Frozen { get; }

    /// <summary>Whether this is the hierarchy expander anchor.</summary>
    public bool IsHierarchyAnchor { get; }
}

/// <summary>Strongly typed builder for one generated DataGrid column.</summary>
public sealed class DataGridColumnBuilder<TItem, TValue> : IDataGridColumnDefinitionBuilder<TItem>
{
    private readonly DataGridMemberPath _path;
    private readonly Func<TItem, TValue> _property;
    private readonly Action _ensureMutable;
    private string? _title;
    private Func<TItem, string?>? _textSelector;
    private RenderFragment<TItem>? _template;
    private RenderFragment<TItem>? _editTemplate;
    private string? _width;
    private bool _sortable = true;
    private bool _resizable = true;
    private bool _canHide = true;
    private bool _visible = true;
    private bool _filterable;
    private ColumnFilterType? _filterType;
    private IReadOnlyList<FilterOperator>? _filterOperators;
    private IReadOnlyList<object>? _filterSelectOptions;
    private AggregateFunction? _aggregate;
    private Func<TItem, decimal>? _aggregateProperty;
    private Func<object?, string>? _aggregateFormat;
    private bool _groupable;
    private IReadOnlyList<DateGroupInterval>? _groupHierarchy;
    private FrozenPosition? _frozen;
    private bool _isHierarchyAnchor;

    internal DataGridColumnBuilder(DataGridMemberPath path, Func<TItem, TValue> property, Action ensureMutable)
    {
        _path = path;
        _property = property;
        _ensureMutable = ensureMutable;
    }

    string IDataGridColumnDefinitionBuilder<TItem>.PropertyName => _path.Path;

    /// <summary>Overrides the inferred title.</summary>
    public DataGridColumnBuilder<TItem, TValue> Title(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _title, value);
    }

    /// <summary>Sets the CSS width.</summary>
    public DataGridColumnBuilder<TItem, TValue> Width(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _width, value);
    }

    /// <summary>Formats values using the current culture.</summary>
    public DataGridColumnBuilder<TItem, TValue> Format(string format, IFormatProvider? provider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        provider ??= CultureInfo.CurrentCulture;
        return Text(item => _property(item) is IFormattable value
            ? value.ToString(format, provider)
            : Convert.ToString(_property(item), provider));
    }

    /// <summary>Sets an explicit display text selector.</summary>
    public DataGridColumnBuilder<TItem, TValue> Text(Func<TItem, string?> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return Set(ref _textSelector, selector);
    }

    /// <summary>Sets a typed cell template.</summary>
    public DataGridColumnBuilder<TItem, TValue> Template(RenderFragment<TItem> template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Set(ref _template, template);
    }

    /// <summary>Sets a typed inline edit template.</summary>
    public DataGridColumnBuilder<TItem, TValue> EditTemplate(RenderFragment<TItem> template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return Set(ref _editTemplate, template);
    }

    /// <summary>Enables or disables sorting.</summary>
    public DataGridColumnBuilder<TItem, TValue> Sortable(bool enabled = true) => Set(ref _sortable, enabled);

    /// <summary>Enables or disables resizing.</summary>
    public DataGridColumnBuilder<TItem, TValue> Resizable(bool enabled = true) => Set(ref _resizable, enabled);

    /// <summary>Enables filtering with inferred or explicit editor type.</summary>
    public DataGridColumnBuilder<TItem, TValue> Filterable(
        bool enabled = true,
        ColumnFilterType? type = null,
        params FilterOperator[] operators)
    {
        _ensureMutable();
        _filterable = enabled;
        _filterType = type;
        _filterOperators = operators.Length == 0 ? null : Array.AsReadOnly(operators.ToArray());
        return this;
    }

    /// <summary>Configures select options for a filterable column.</summary>
    public DataGridColumnBuilder<TItem, TValue> FilterOptions(IEnumerable<TValue> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        object[] snapshot = options.Cast<object>().ToArray();
        _filterSelectOptions = Array.AsReadOnly(snapshot);
        _filterType = ColumnFilterType.Select;
        _filterable = true;
        return this;
    }

    /// <summary>Enables grouping with an optional date hierarchy.</summary>
    public DataGridColumnBuilder<TItem, TValue> Groupable(
        bool enabled = true,
        params DateGroupInterval[] hierarchy)
    {
        _ensureMutable();
        _groupable = enabled;
        _groupHierarchy = hierarchy.Length == 0 ? null : Array.AsReadOnly(hierarchy.ToArray());
        return this;
    }

    /// <summary>Configures an aggregate and optional formatter.</summary>
    public DataGridColumnBuilder<TItem, TValue> Aggregate(
        AggregateFunction function,
        Func<TItem, decimal>? selector = null,
        Func<object?, string>? format = null)
    {
        _ensureMutable();
        _aggregate = function;
        _aggregateProperty = selector;
        _aggregateFormat = format;
        return this;
    }

    /// <summary>Freezes the column at an edge.</summary>
    public DataGridColumnBuilder<TItem, TValue> Frozen(FrozenPosition? position) => Set(ref _frozen, position);

    /// <summary>Sets visibility behavior.</summary>
    public DataGridColumnBuilder<TItem, TValue> Visibility(bool visible = true, bool canHide = true)
    {
        _ensureMutable();
        _visible = visible;
        _canHide = canHide;
        return this;
    }

    /// <summary>Marks this column as the hierarchy expansion anchor.</summary>
    public DataGridColumnBuilder<TItem, TValue> HierarchyAnchor(bool enabled = true)
        => Set(ref _isHierarchyAnchor, enabled);

    DataGridColumnDefinition<TItem> IDataGridColumnDefinitionBuilder<TItem>.Build()
    {
        Type valueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        string title = _title
            ?? _path.Leaf.GetCustomAttribute<DisplayAttribute>()?.GetName()
            ?? _path.Leaf.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
            ?? _path.Leaf.Name;
        return new DataGridColumnDefinition<TItem>(
            _path.Path,
            title,
            item => _property(item),
            _textSelector,
            _template,
            _editTemplate,
            _width,
            _sortable,
            _resizable,
            _canHide,
            _visible,
            _filterable,
            _filterType ?? InferFilterType(valueType),
            _filterOperators,
            _filterSelectOptions,
            _aggregate,
            _aggregateProperty,
            _aggregateFormat,
            _groupable,
            _groupHierarchy,
            _frozen,
            _isHierarchyAnchor);
    }

    private DataGridColumnBuilder<TItem, TValue> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }

    private static ColumnFilterType InferFilterType(Type type)
    {
        if (type == typeof(bool)) return ColumnFilterType.Boolean;
        if (type.IsEnum) return ColumnFilterType.Select;
        if (type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return ColumnFilterType.Date;
        return Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte
            or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
            or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double
            or TypeCode.Decimal
            ? ColumnFilterType.Number
            : ColumnFilterType.Text;
    }
}

internal interface IDataGridColumnDefinitionBuilder<TItem>
{
    string PropertyName { get; }

    DataGridColumnDefinition<TItem> Build();
}

internal sealed class ExistingDataGridColumnDefinitionBuilder<TItem>(DataGridColumnDefinition<TItem> definition)
    : IDataGridColumnDefinitionBuilder<TItem>
{
    public string PropertyName => definition.PropertyName;

    public DataGridColumnDefinition<TItem> Build() => definition;
}

internal sealed record DataGridMemberPath(string Path, PropertyInfo Leaf)
{
    internal static DataGridMemberPath Resolve<TItem, TValue>(Expression<Func<TItem, TValue>> selector)
    {
        Expression body = selector.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            body = unary.Operand;
        List<PropertyInfo> properties = [];
        while (body is MemberExpression { Member: PropertyInfo property } member)
        {
            if (property.GetIndexParameters().Length != 0)
                throw new ArgumentException("DataGrid selectors cannot contain indexers.", nameof(selector));
            properties.Add(property);
            body = member.Expression!;
        }
        if (body is not ParameterExpression || properties.Count == 0)
            throw new ArgumentException("DataGrid selectors must be direct property paths.", nameof(selector));
        properties.Reverse();
        return new DataGridMemberPath(
            string.Join('.', properties.Select(static property => property.Name)),
            properties[^1]);
    }
}
