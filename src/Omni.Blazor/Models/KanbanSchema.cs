using System.Linq.Expressions;

namespace Omni.Blazor.Models;

/// <summary>Immutable Kanban column metadata.</summary>
public sealed record KanbanColumnSchema(
    string Id,
    string? Title,
    string? Icon,
    CardTone Tone,
    int? WipLimit,
    bool Collapsed);

/// <summary>Immutable Kanban swimlane metadata.</summary>
public sealed record KanbanSwimlaneSchema(
    string Id,
    string? Title,
    string? Icon,
    bool Collapsed);

/// <summary>Immutable typed Kanban quick-filter metadata.</summary>
public sealed record KanbanQuickFilterSchema<TCard>(
    string Label,
    Func<TCard, bool> Predicate,
    string? Icon);

/// <summary>Immutable strongly typed board, card and workflow metadata for OmniKanban.</summary>
public sealed class KanbanSchema<TCard>
{
    internal KanbanSchema(
        IReadOnlyList<KanbanColumnSchema> columns,
        Func<TCard, string> columnSelector,
        Action<TCard, string>? columnSetter,
        Func<TCard, object>? cardId,
        IReadOnlyList<KanbanSwimlaneSchema> swimlanes,
        Func<TCard, string>? swimlaneSelector,
        Action<TCard, string>? swimlaneSetter,
        Func<TCard, string>? searchSelector,
        IReadOnlyList<KanbanQuickFilterSchema<TCard>> quickFilters,
        KanbanCardSchema<TCard> card,
        bool showSearch,
        bool showCount,
        bool collapsible,
        bool swimlanesCollapsible,
        bool allowAddCard,
        bool allowColumnReorder,
        bool dragDisabled,
        WipLimitMode wipLimitMode,
        string? columnWidth)
    {
        Columns = columns;
        ColumnSelector = columnSelector;
        ColumnSetter = columnSetter;
        CardId = cardId;
        Swimlanes = swimlanes;
        SwimlaneSelector = swimlaneSelector;
        SwimlaneSetter = swimlaneSetter;
        SearchSelector = searchSelector;
        QuickFilters = quickFilters;
        Card = card;
        ShowSearch = showSearch;
        ShowCount = showCount;
        Collapsible = collapsible;
        SwimlanesCollapsible = swimlanesCollapsible;
        AllowAddCard = allowAddCard;
        AllowColumnReorder = allowColumnReorder;
        DragDisabled = dragDisabled;
        WipLimitMode = wipLimitMode;
        ColumnWidth = columnWidth;
    }

    /// <summary>Immutable board column snapshot.</summary>
    public IReadOnlyList<KanbanColumnSchema> Columns { get; }

    /// <summary>Compiled card-to-column selector.</summary>
    public Func<TCard, string> ColumnSelector { get; }

    /// <summary>Optional card column mutation delegate.</summary>
    public Action<TCard, string>? ColumnSetter { get; }

    /// <summary>Optional stable card key selector.</summary>
    public Func<TCard, object>? CardId { get; }

    /// <summary>Immutable swimlane snapshot.</summary>
    public IReadOnlyList<KanbanSwimlaneSchema> Swimlanes { get; }

    /// <summary>Optional card-to-swimlane selector.</summary>
    public Func<TCard, string>? SwimlaneSelector { get; }

    /// <summary>Optional card swimlane mutation delegate.</summary>
    public Action<TCard, string>? SwimlaneSetter { get; }

    /// <summary>Optional searchable text selector.</summary>
    public Func<TCard, string>? SearchSelector { get; }

    /// <summary>Immutable quick-filter snapshot.</summary>
    public IReadOnlyList<KanbanQuickFilterSchema<TCard>> QuickFilters { get; }

    /// <summary>Typed card presentation selectors.</summary>
    public KanbanCardSchema<TCard> Card { get; }

    /// <summary>Whether board search is shown.</summary>
    public bool ShowSearch { get; }

    /// <summary>Whether card counts are shown.</summary>
    public bool ShowCount { get; }

    /// <summary>Whether columns can be collapsed.</summary>
    public bool Collapsible { get; }

    /// <summary>Whether swimlanes can be collapsed.</summary>
    public bool SwimlanesCollapsible { get; }

    /// <summary>Whether add-card actions are shown.</summary>
    public bool AllowAddCard { get; }

    /// <summary>Whether columns can be reordered.</summary>
    public bool AllowColumnReorder { get; }

    /// <summary>Whether card movement is disabled.</summary>
    public bool DragDisabled { get; }

    /// <summary>How WIP limits are enforced.</summary>
    public WipLimitMode WipLimitMode { get; }

    /// <summary>Optional CSS column width.</summary>
    public string? ColumnWidth { get; }

    /// <summary>Creates an immutable typed Kanban schema.</summary>
    public static KanbanSchema<TCard> Create(Action<KanbanSchemaBuilder<TCard>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        KanbanSchemaBuilder<TCard> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot builder.</summary>
    public static KanbanSchemaBuilder<TCard> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public KanbanSchema<TCard> Extend(Action<KanbanSchemaBuilder<TCard>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        KanbanSchemaBuilder<TCard> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Immutable typed card presentation selectors.</summary>
public sealed record KanbanCardSchema<TCard>(
    Func<TCard, string>? Title,
    Func<TCard, string?>? Assignee,
    Func<TCard, string?>? Avatar,
    Func<TCard, DateTime?>? DueDate,
    Func<TCard, KanbanPriority>? Priority,
    Func<TCard, (int Done, int Total)?>? Subtasks,
    Func<TCard, string?>? Estimate,
    Func<TCard, IEnumerable<KanbanField>?>? Fields,
    Func<TCard, string?>? Color,
    Func<TCard, int?>? Age,
    Func<TCard, IEnumerable<KanbanCardAction>>? Actions,
    bool ShowAge,
    int AgeWarnDays,
    int AgeStaleDays);

/// <summary>Strongly typed one-shot builder for Kanban workflow and presentation metadata.</summary>
public sealed class KanbanSchemaBuilder<TCard>
{
    private readonly List<KanbanColumnSchema> _columns = [];
    private readonly HashSet<string> _columnIds = new(StringComparer.Ordinal);
    private readonly List<KanbanSwimlaneSchema> _swimlanes = [];
    private readonly HashSet<string> _swimlaneIds = new(StringComparer.Ordinal);
    private readonly List<KanbanQuickFilterSchema<TCard>> _quickFilters = [];
    private readonly KanbanCardBuilder<TCard> _card;
    private Func<TCard, string>? _columnSelector;
    private Action<TCard, string>? _columnSetter;
    private Func<TCard, object>? _cardId;
    private Func<TCard, string>? _swimlaneSelector;
    private Action<TCard, string>? _swimlaneSetter;
    private Func<TCard, string>? _searchSelector;
    private bool _showSearch;
    private bool _showCount = true;
    private bool _collapsible = true;
    private bool _swimlanesCollapsible = true;
    private bool _allowAddCard;
    private bool _allowColumnReorder;
    private bool _dragDisabled;
    private WipLimitMode _wipLimitMode = WipLimitMode.Warn;
    private string? _columnWidth;
    private bool _built;

    /// <summary>Creates an empty builder.</summary>
    public KanbanSchemaBuilder() => _card = new KanbanCardBuilder<TCard>(EnsureMutable);

    /// <summary>Includes workflow, card and presentation defaults from an immutable schema.</summary>
    public KanbanSchemaBuilder<TCard> Include(KanbanSchema<TCard> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        foreach (KanbanColumnSchema column in schema.Columns)
        {
            if (!_columnIds.Add(column.Id))
                throw new InvalidOperationException($"Kanban column '{column.Id}' was declared more than once.");
            _columns.Add(column);
        }
        foreach (KanbanSwimlaneSchema swimlane in schema.Swimlanes)
        {
            if (!_swimlaneIds.Add(swimlane.Id))
                throw new InvalidOperationException($"Kanban swimlane '{swimlane.Id}' was declared more than once.");
            _swimlanes.Add(swimlane);
        }
        _quickFilters.AddRange(schema.QuickFilters);
        _columnSelector = schema.ColumnSelector;
        _columnSetter = schema.ColumnSetter;
        _cardId = schema.CardId;
        _swimlaneSelector = schema.SwimlaneSelector;
        _swimlaneSetter = schema.SwimlaneSetter;
        _searchSelector = schema.SearchSelector;
        _card.Include(schema.Card);
        _showSearch = schema.ShowSearch;
        _showCount = schema.ShowCount;
        _collapsible = schema.Collapsible;
        _swimlanesCollapsible = schema.SwimlanesCollapsible;
        _allowAddCard = schema.AllowAddCard;
        _allowColumnReorder = schema.AllowColumnReorder;
        _dragDisabled = schema.DragDisabled;
        _wipLimitMode = schema.WipLimitMode;
        _columnWidth = schema.ColumnWidth;
        return this;
    }

    /// <summary>Maps card column state and its optional mutation delegate.</summary>
    public KanbanSchemaBuilder<TCard> ColumnState(
        Expression<Func<TCard, string>> selector,
        Action<TCard, string>? setter = null)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _columnSelector = selector.Compile();
        _columnSetter = setter;
        return this;
    }

    /// <summary>Adds one immutable board column.</summary>
    public KanbanSchemaBuilder<TCard> Column(
        string id,
        string? title = null,
        Action<KanbanColumnBuilder>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!_columnIds.Add(id)) throw new InvalidOperationException($"Kanban column '{id}' was declared more than once.");
        KanbanColumnBuilder builder = new(id, title, EnsureMutable);
        configure?.Invoke(builder);
        _columns.Add(builder.Build());
        return this;
    }

    /// <summary>Replaces an inherited column while preserving its board position.</summary>
    public KanbanSchemaBuilder<TCard> OverrideColumn(
        string id,
        string? title = null,
        Action<KanbanColumnBuilder>? configure = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        int index = _columns.FindIndex(column => string.Equals(column.Id, id, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"Kanban column '{id}' cannot be overridden because it is not declared.");
        KanbanColumnBuilder builder = new(id, title, EnsureMutable);
        configure?.Invoke(builder);
        _columns[index] = builder.Build();
        return this;
    }

    /// <summary>Removes inherited columns so a derived schema can declare a new workflow.</summary>
    public KanbanSchemaBuilder<TCard> ClearColumns()
    {
        EnsureMutable();
        _columns.Clear();
        _columnIds.Clear();
        return this;
    }

    /// <summary>Sets a stable card key.</summary>
    public KanbanSchemaBuilder<TCard> Key<TKey>(Expression<Func<TCard, TKey>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        Func<TCard, TKey> compiled = selector.Compile();
        _cardId = card => compiled(card)!;
        return this;
    }

    /// <summary>Maps optional swimlane state and its mutation delegate.</summary>
    public KanbanSchemaBuilder<TCard> SwimlaneState(
        Expression<Func<TCard, string>> selector,
        Action<TCard, string>? setter = null)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _swimlaneSelector = selector.Compile();
        _swimlaneSetter = setter;
        return this;
    }

    /// <summary>Adds one immutable swimlane.</summary>
    public KanbanSchemaBuilder<TCard> Swimlane(
        string id,
        string? title = null,
        string? icon = null,
        bool collapsed = false)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!_swimlaneIds.Add(id)) throw new InvalidOperationException($"Kanban swimlane '{id}' was declared more than once.");
        _swimlanes.Add(new KanbanSwimlaneSchema(id, title, icon, collapsed));
        return this;
    }

    /// <summary>Removes inherited swimlanes.</summary>
    public KanbanSchemaBuilder<TCard> ClearSwimlanes()
    {
        EnsureMutable();
        _swimlanes.Clear();
        _swimlaneIds.Clear();
        return this;
    }

    /// <summary>Enables board search with a typed text selector.</summary>
    public KanbanSchemaBuilder<TCard> Search(Expression<Func<TCard, string>> selector, bool enabled = true)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _searchSelector = selector.Compile();
        _showSearch = enabled;
        return this;
    }

    /// <summary>Adds a typed quick filter.</summary>
    public KanbanSchemaBuilder<TCard> QuickFilter(
        string label,
        Func<TCard, bool> predicate,
        string? icon = null)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(predicate);
        if (_quickFilters.Any(filter => string.Equals(filter.Label, label, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Kanban quick filter '{label}' was declared more than once.");
        _quickFilters.Add(new KanbanQuickFilterSchema<TCard>(label, predicate, icon));
        return this;
    }

    /// <summary>Removes inherited quick filters.</summary>
    public KanbanSchemaBuilder<TCard> ClearQuickFilters()
    {
        EnsureMutable();
        _quickFilters.Clear();
        return this;
    }

    /// <summary>Configures typed default-card selectors.</summary>
    public KanbanSchemaBuilder<TCard> Card(Action<KanbanCardBuilder<TCard>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_card);
        return this;
    }

    /// <summary>Configures board behavior defaults.</summary>
    public KanbanSchemaBuilder<TCard> Behavior(
        bool showCount = true,
        bool collapsible = true,
        bool swimlanesCollapsible = true,
        bool allowAddCard = false,
        bool allowColumnReorder = false,
        bool dragDisabled = false,
        WipLimitMode wipLimitMode = WipLimitMode.Warn,
        string? columnWidth = null)
    {
        EnsureMutable();
        if (!Enum.IsDefined(wipLimitMode)) throw new ArgumentOutOfRangeException(nameof(wipLimitMode));
        _showCount = showCount;
        _collapsible = collapsible;
        _swimlanesCollapsible = swimlanesCollapsible;
        _allowAddCard = allowAddCard;
        _allowColumnReorder = allowColumnReorder;
        _dragDisabled = dragDisabled;
        _wipLimitMode = wipLimitMode;
        _columnWidth = columnWidth;
        return this;
    }

    /// <summary>Builds the immutable schema.</summary>
    public KanbanSchema<TCard> Build()
    {
        EnsureMutable();
        _built = true;
        if (_columns.Count == 0) throw new InvalidOperationException("Kanban schema requires at least one column.");
        return new KanbanSchema<TCard>(
            Array.AsReadOnly(_columns.ToArray()),
            _columnSelector ?? throw new InvalidOperationException("Kanban schema requires a ColumnState selector."),
            _columnSetter,
            _cardId,
            Array.AsReadOnly(_swimlanes.ToArray()),
            _swimlaneSelector,
            _swimlaneSetter,
            _searchSelector,
            Array.AsReadOnly(_quickFilters.ToArray()),
            _card.Build(),
            _showSearch,
            _showCount,
            _collapsible,
            _swimlanesCollapsible,
            _allowAddCard,
            _allowColumnReorder,
            _dragDisabled,
            _wipLimitMode,
            _columnWidth);
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("Kanban schema is immutable after Build().");
    }
}

/// <summary>Fluent builder for one Kanban column.</summary>
public sealed class KanbanColumnBuilder
{
    private readonly Action _ensureMutable;
    private readonly string _id;
    private string? _title;
    private string? _icon;
    private CardTone _tone;
    private int? _wipLimit;
    private bool _collapsed;

    internal KanbanColumnBuilder(string id, string? title, Action ensureMutable)
    {
        _id = id;
        _title = title;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets header icon and tone.</summary>
    public KanbanColumnBuilder Appearance(string? icon = null, CardTone tone = CardTone.None)
    {
        _ensureMutable();
        if (!Enum.IsDefined(tone)) throw new ArgumentOutOfRangeException(nameof(tone));
        _icon = icon;
        _tone = tone;
        return this;
    }

    /// <summary>Sets an optional WIP limit and initial collapsed state.</summary>
    public KanbanColumnBuilder State(int? wipLimit = null, bool collapsed = false)
    {
        _ensureMutable();
        if (wipLimit < 1) throw new ArgumentOutOfRangeException(nameof(wipLimit));
        _wipLimit = wipLimit;
        _collapsed = collapsed;
        return this;
    }

    internal KanbanColumnSchema Build()
        => new(_id, _title, _icon, _tone, _wipLimit, _collapsed);
}

/// <summary>Strongly typed builder for the default Kanban card.</summary>
public sealed class KanbanCardBuilder<TCard>
{
    private readonly Action _ensureMutable;
    private Func<TCard, string>? _title;
    private Func<TCard, string?>? _assignee;
    private Func<TCard, string?>? _avatar;
    private Func<TCard, DateTime?>? _dueDate;
    private Func<TCard, KanbanPriority>? _priority;
    private Func<TCard, (int Done, int Total)?>? _subtasks;
    private Func<TCard, string?>? _estimate;
    private Func<TCard, IEnumerable<KanbanField>?>? _fields;
    private Func<TCard, string?>? _color;
    private Func<TCard, int?>? _age;
    private Func<TCard, IEnumerable<KanbanCardAction>>? _actions;
    private bool _showAge;
    private int _ageWarnDays = 3;
    private int _ageStaleDays = 5;

    internal KanbanCardBuilder(Action ensureMutable) => _ensureMutable = ensureMutable;

    internal void Include(KanbanCardSchema<TCard> card)
    {
        _ensureMutable();
        _title = card.Title;
        _assignee = card.Assignee;
        _avatar = card.Avatar;
        _dueDate = card.DueDate;
        _priority = card.Priority;
        _subtasks = card.Subtasks;
        _estimate = card.Estimate;
        _fields = card.Fields;
        _color = card.Color;
        _age = card.Age;
        _actions = card.Actions;
        _showAge = card.ShowAge;
        _ageWarnDays = card.AgeWarnDays;
        _ageStaleDays = card.AgeStaleDays;
    }

    /// <summary>Maps the primary card title.</summary>
    public KanbanCardBuilder<TCard> Title(Expression<Func<TCard, string>> selector) => Set(selector, ref _title);

    /// <summary>Maps assignee and optional avatar URL.</summary>
    public KanbanCardBuilder<TCard> Assignee(
        Expression<Func<TCard, string?>> selector,
        Expression<Func<TCard, string?>>? avatar = null)
    {
        Set(selector, ref _assignee);
        if (avatar is not null) Set(avatar, ref _avatar);
        return this;
    }

    /// <summary>Maps a due date.</summary>
    public KanbanCardBuilder<TCard> DueDate(Expression<Func<TCard, DateTime?>> selector) => Set(selector, ref _dueDate);

    /// <summary>Maps card priority.</summary>
    public KanbanCardBuilder<TCard> Priority(Expression<Func<TCard, KanbanPriority>> selector) => Set(selector, ref _priority);

    /// <summary>Maps subtask progress.</summary>
    public KanbanCardBuilder<TCard> Subtasks(Func<TCard, (int Done, int Total)?> selector) => Set(selector, ref _subtasks);

    /// <summary>Maps an estimate label.</summary>
    public KanbanCardBuilder<TCard> Estimate(Expression<Func<TCard, string?>> selector) => Set(selector, ref _estimate);

    /// <summary>Maps extra card fields.</summary>
    public KanbanCardBuilder<TCard> Fields(Func<TCard, IEnumerable<KanbanField>?> selector) => Set(selector, ref _fields);

    /// <summary>Maps a CSS accent color.</summary>
    public KanbanCardBuilder<TCard> Color(Expression<Func<TCard, string?>> selector) => Set(selector, ref _color);

    /// <summary>Maps card age and warning thresholds.</summary>
    public KanbanCardBuilder<TCard> Age(
        Expression<Func<TCard, int?>> selector,
        int warningDays = 3,
        int staleDays = 5)
    {
        if (warningDays < 0) throw new ArgumentOutOfRangeException(nameof(warningDays));
        if (staleDays < warningDays) throw new ArgumentOutOfRangeException(nameof(staleDays));
        Set(selector, ref _age);
        _showAge = true;
        _ageWarnDays = warningDays;
        _ageStaleDays = staleDays;
        return this;
    }

    /// <summary>Maps card overflow actions.</summary>
    public KanbanCardBuilder<TCard> Actions(Func<TCard, IEnumerable<KanbanCardAction>> selector) => Set(selector, ref _actions);

    internal KanbanCardSchema<TCard> Build()
        => new(
            _title, _assignee, _avatar, _dueDate, _priority, _subtasks,
            _estimate, _fields, _color, _age, _actions,
            _showAge, _ageWarnDays, _ageStaleDays);

    private KanbanCardBuilder<TCard> Set<TValue>(
        Expression<Func<TCard, TValue>> selector,
        ref Func<TCard, TValue>? target)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        target = selector.Compile();
        return this;
    }

    private KanbanCardBuilder<TCard> Set<TValue>(Func<TCard, TValue> selector, ref Func<TCard, TValue>? target)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        target = selector;
        return this;
    }
}
