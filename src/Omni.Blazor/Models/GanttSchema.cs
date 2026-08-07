using System.Linq.Expressions;

namespace Omni.Blazor.Models;

/// <summary>Immutable typed Gantt dependency metadata.</summary>
public sealed record GanttDependencySchema<TItem>(TItem From, TItem To, GanttDependencyType Type);

/// <summary>Immutable Gantt timeline marker metadata.</summary>
public sealed record GanttMarkerSchema(DateTime Date, string? Label = null, string? Color = null);

/// <summary>Immutable strongly typed task projection and presentation defaults for OmniGantt.</summary>
public sealed class GanttSchema<TItem>
{
    internal GanttSchema(
        Func<TItem, object?>? key,
        Func<TItem, object?>? parentKey,
        Func<TItem, string?>? text,
        Func<TItem, DateTime> start,
        Func<TItem, DateTime> end,
        Func<TItem, double?>? progress,
        Func<TItem, DateTime?>? baselineStart,
        Func<TItem, DateTime?>? baselineEnd,
        IReadOnlyList<GanttDependencySchema<TItem>> dependencies,
        IReadOnlyList<GanttMarkerSchema> markers,
        GanttZoomLevel zoomLevel,
        int rowHeight,
        string leftPaneWidth,
        bool showNavigation,
        bool showTodayLine,
        bool showWeekends,
        bool showCriticalPath,
        IReadOnlyList<DayOfWeek> nonWorkingDays)
    {
        Key = key;
        ParentKey = parentKey;
        Text = text;
        Start = start;
        End = end;
        Progress = progress;
        BaselineStart = baselineStart;
        BaselineEnd = baselineEnd;
        Dependencies = dependencies;
        Markers = markers;
        RuntimeDependencies = Array.AsReadOnly(dependencies.Select(static dependency => new GanttDependency<TItem>
        {
            From = dependency.From,
            To = dependency.To,
            Type = dependency.Type
        }).ToArray());
        RuntimeMarkers = Array.AsReadOnly(markers.Select(static marker => new GanttMarker
        {
            Date = marker.Date,
            Label = marker.Label,
            Color = marker.Color
        }).ToArray());
        ZoomLevel = zoomLevel;
        RowHeight = rowHeight;
        LeftPaneWidth = leftPaneWidth;
        ShowNavigation = showNavigation;
        ShowTodayLine = showTodayLine;
        ShowWeekends = showWeekends;
        ShowCriticalPath = showCriticalPath;
        NonWorkingDays = nonWorkingDays;
    }

    /// <summary>Optional stable task key selector.</summary>
    public Func<TItem, object?>? Key { get; }

    /// <summary>Optional parent task key selector.</summary>
    public Func<TItem, object?>? ParentKey { get; }

    /// <summary>Optional task label selector.</summary>
    public Func<TItem, string?>? Text { get; }

    /// <summary>Task start selector.</summary>
    public Func<TItem, DateTime> Start { get; }

    /// <summary>Task end selector.</summary>
    public Func<TItem, DateTime> End { get; }

    /// <summary>Optional progress selector in the zero-to-one-hundred range.</summary>
    public Func<TItem, double?>? Progress { get; }

    /// <summary>Optional baseline start selector.</summary>
    public Func<TItem, DateTime?>? BaselineStart { get; }

    /// <summary>Optional baseline end selector.</summary>
    public Func<TItem, DateTime?>? BaselineEnd { get; }

    /// <summary>Immutable typed task dependencies.</summary>
    public IReadOnlyList<GanttDependencySchema<TItem>> Dependencies { get; }

    /// <summary>Immutable timeline markers.</summary>
    public IReadOnlyList<GanttMarkerSchema> Markers { get; }

    internal IReadOnlyList<GanttDependency<TItem>> RuntimeDependencies { get; }

    internal IReadOnlyList<GanttMarker> RuntimeMarkers { get; }

    /// <summary>Default zoom level.</summary>
    public GanttZoomLevel ZoomLevel { get; }

    /// <summary>Task row height in pixels.</summary>
    public int RowHeight { get; }

    /// <summary>Fallback left pane width.</summary>
    public string LeftPaneWidth { get; }

    /// <summary>Whether navigation is shown.</summary>
    public bool ShowNavigation { get; }

    /// <summary>Whether the current time line is shown.</summary>
    public bool ShowTodayLine { get; }

    /// <summary>Whether non-working days are shaded.</summary>
    public bool ShowWeekends { get; }

    /// <summary>Whether the critical path is highlighted.</summary>
    public bool ShowCriticalPath { get; }

    /// <summary>Immutable non-working weekdays.</summary>
    public IReadOnlyList<DayOfWeek> NonWorkingDays { get; }

    /// <summary>Creates an immutable typed Gantt schema.</summary>
    public static GanttSchema<TItem> Create(Action<GanttSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        GanttSchemaBuilder<TItem> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot builder.</summary>
    public static GanttSchemaBuilder<TItem> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public GanttSchema<TItem> Extend(Action<GanttSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        GanttSchemaBuilder<TItem> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Strongly typed one-shot builder for Gantt task metadata.</summary>
public sealed class GanttSchemaBuilder<TItem>
{
    private Func<TItem, object?>? _key;
    private Func<TItem, object?>? _parentKey;
    private Func<TItem, string?>? _text;
    private Func<TItem, DateTime>? _start;
    private Func<TItem, DateTime>? _end;
    private Func<TItem, double?>? _progress;
    private Func<TItem, DateTime?>? _baselineStart;
    private Func<TItem, DateTime?>? _baselineEnd;
    private IReadOnlyList<GanttDependencySchema<TItem>> _dependencies = Array.Empty<GanttDependencySchema<TItem>>();
    private IReadOnlyList<GanttMarkerSchema> _markers = Array.Empty<GanttMarkerSchema>();
    private GanttZoomLevel _zoomLevel = GanttZoomLevel.Week;
    private int _rowHeight = 34;
    private string _leftPaneWidth = "360px";
    private bool _showNavigation = true;
    private bool _showTodayLine = true;
    private bool _showWeekends = true;
    private bool _showCriticalPath;
    private IReadOnlyList<DayOfWeek> _nonWorkingDays = Array.AsReadOnly(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
    private bool _built;

    /// <summary>Includes task projection and presentation defaults from an immutable schema.</summary>
    public GanttSchemaBuilder<TItem> Include(GanttSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _key = schema.Key;
        _parentKey = schema.ParentKey;
        _text = schema.Text;
        _start = schema.Start;
        _end = schema.End;
        _progress = schema.Progress;
        _baselineStart = schema.BaselineStart;
        _baselineEnd = schema.BaselineEnd;
        _dependencies = schema.Dependencies;
        _markers = schema.Markers;
        _zoomLevel = schema.ZoomLevel;
        _rowHeight = schema.RowHeight;
        _leftPaneWidth = schema.LeftPaneWidth;
        _showNavigation = schema.ShowNavigation;
        _showTodayLine = schema.ShowTodayLine;
        _showWeekends = schema.ShowWeekends;
        _showCriticalPath = schema.ShowCriticalPath;
        _nonWorkingDays = schema.NonWorkingDays;
        return this;
    }

    /// <summary>Maps stable and optional parent keys using compile-time checked expressions.</summary>
    public GanttSchemaBuilder<TItem> Hierarchy<TKey, TParentKey>(
        Expression<Func<TItem, TKey>> key,
        Expression<Func<TItem, TParentKey>> parentKey)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(parentKey);
        Func<TItem, TKey> compiledKey = key.Compile();
        Func<TItem, TParentKey> compiledParent = parentKey.Compile();
        _key = item => compiledKey(item);
        _parentKey = item => compiledParent(item);
        return this;
    }

    /// <summary>Maps a stable key without enabling hierarchy.</summary>
    public GanttSchemaBuilder<TItem> Key<TKey>(Expression<Func<TItem, TKey>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        Func<TItem, TKey> compiled = selector.Compile();
        _key = item => compiled(item);
        return this;
    }

    /// <summary>Maps task label, start and end values.</summary>
    public GanttSchemaBuilder<TItem> Task(
        Expression<Func<TItem, string?>> text,
        Expression<Func<TItem, DateTime>> start,
        Expression<Func<TItem, DateTime>> end)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        _text = text.Compile();
        _start = start.Compile();
        _end = end.Compile();
        return this;
    }

    /// <summary>Maps task progress.</summary>
    public GanttSchemaBuilder<TItem> Progress(Expression<Func<TItem, double?>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _progress = selector.Compile();
        return this;
    }

    /// <summary>Maps optional baseline dates.</summary>
    public GanttSchemaBuilder<TItem> Baseline(
        Expression<Func<TItem, DateTime?>> start,
        Expression<Func<TItem, DateTime?>> end)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        _baselineStart = start.Compile();
        _baselineEnd = end.Compile();
        return this;
    }

    /// <summary>Sets a typed immutable dependency snapshot.</summary>
    public GanttSchemaBuilder<TItem> Dependencies(IEnumerable<GanttDependency<TItem>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        GanttDependencySchema<TItem>[] snapshot = values.Select(static dependency =>
        {
            ArgumentNullException.ThrowIfNull(dependency);
            if (!Enum.IsDefined(dependency.Type)) throw new ArgumentOutOfRangeException(nameof(values));
            return new GanttDependencySchema<TItem>(dependency.From, dependency.To, dependency.Type);
        }).ToArray();
        return Set(ref _dependencies, Array.AsReadOnly(snapshot));
    }

    /// <summary>Sets an immutable marker snapshot.</summary>
    public GanttSchemaBuilder<TItem> Markers(IEnumerable<GanttMarker> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        GanttMarkerSchema[] snapshot = values.Select(static marker =>
        {
            ArgumentNullException.ThrowIfNull(marker);
            return new GanttMarkerSchema(marker.Date, marker.Label, marker.Color);
        }).ToArray();
        return Set(ref _markers, Array.AsReadOnly(snapshot));
    }

    /// <summary>Sets timeline presentation defaults.</summary>
    public GanttSchemaBuilder<TItem> Timeline(
        GanttZoomLevel zoom = GanttZoomLevel.Week,
        int rowHeight = 34,
        string leftPaneWidth = "360px",
        bool showNavigation = true,
        bool showTodayLine = true,
        bool showWeekends = true,
        bool showCriticalPath = false)
    {
        EnsureMutable();
        if (!Enum.IsDefined(zoom)) throw new ArgumentOutOfRangeException(nameof(zoom));
        if (rowHeight is < 20 or > 256) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        ArgumentException.ThrowIfNullOrWhiteSpace(leftPaneWidth);
        _zoomLevel = zoom;
        _rowHeight = rowHeight;
        _leftPaneWidth = leftPaneWidth;
        _showNavigation = showNavigation;
        _showTodayLine = showTodayLine;
        _showWeekends = showWeekends;
        _showCriticalPath = showCriticalPath;
        return this;
    }

    /// <summary>Sets immutable non-working weekdays.</summary>
    public GanttSchemaBuilder<TItem> NonWorkingDays(params DayOfWeek[] days)
    {
        EnsureMutable();
        if (days.Any(static day => !Enum.IsDefined(day))) throw new ArgumentOutOfRangeException(nameof(days));
        _nonWorkingDays = Array.AsReadOnly(days.Distinct().ToArray());
        return this;
    }

    /// <summary>Builds the immutable schema.</summary>
    public GanttSchema<TItem> Build()
    {
        EnsureMutable();
        _built = true;
        if ((_key is null) != (_parentKey is null) && _parentKey is not null)
            throw new InvalidOperationException("Gantt parent keys require a stable task key.");
        return new GanttSchema<TItem>(
            _key,
            _parentKey,
            _text,
            _start ?? throw new InvalidOperationException("Gantt schema requires a task Start selector."),
            _end ?? throw new InvalidOperationException("Gantt schema requires a task End selector."),
            _progress,
            _baselineStart,
            _baselineEnd,
            _dependencies,
            _markers,
            _zoomLevel,
            _rowHeight,
            _leftPaneWidth,
            _showNavigation,
            _showTodayLine,
            _showWeekends,
            _showCriticalPath,
            _nonWorkingDays);
    }

    private GanttSchemaBuilder<TItem> Set<T>(ref T target, T value)
    {
        EnsureMutable();
        target = value;
        return this;
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("Gantt schema is immutable after Build().");
    }
}
