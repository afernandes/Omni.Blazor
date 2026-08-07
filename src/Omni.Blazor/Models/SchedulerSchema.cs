using System.Globalization;
using System.Linq.Expressions;

namespace Omni.Blazor.Models;

/// <summary>Immutable strongly typed appointment projection and presentation defaults for OmniScheduler.</summary>
public sealed class SchedulerSchema<TItem>
{
    internal SchedulerSchema(
        Func<TItem, DateTime> start,
        Func<TItem, DateTime> end,
        Func<TItem, string?>? text,
        int selectedViewIndex,
        string height,
        CultureInfo? culture,
        bool showHeader,
        bool showNavigationButtons,
        bool showTodayButton,
        bool showDateTitle)
    {
        Start = start;
        End = end;
        Text = text;
        SelectedViewIndex = selectedViewIndex;
        Height = height;
        Culture = culture;
        ShowHeader = showHeader;
        ShowNavigationButtons = showNavigationButtons;
        ShowTodayButton = showTodayButton;
        ShowDateTitle = showDateTitle;
    }

    /// <summary>Compiled appointment start selector.</summary>
    public Func<TItem, DateTime> Start { get; }

    /// <summary>Compiled appointment end selector.</summary>
    public Func<TItem, DateTime> End { get; }

    /// <summary>Optional compiled appointment label selector.</summary>
    public Func<TItem, string?>? Text { get; }

    /// <summary>Initial selected scheduler view index.</summary>
    public int SelectedViewIndex { get; }

    /// <summary>Scheduler CSS height.</summary>
    public string Height { get; }

    /// <summary>Optional formatting culture.</summary>
    public CultureInfo? Culture { get; }

    /// <summary>Whether the complete header is shown.</summary>
    public bool ShowHeader { get; }

    /// <summary>Whether previous and next actions are shown.</summary>
    public bool ShowNavigationButtons { get; }

    /// <summary>Whether the today action is shown.</summary>
    public bool ShowTodayButton { get; }

    /// <summary>Whether the current range title is shown.</summary>
    public bool ShowDateTitle { get; }

    /// <summary>Creates an immutable scheduler schema.</summary>
    public static SchedulerSchema<TItem> Create(Action<SchedulerSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        SchedulerSchemaBuilder<TItem> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable one-shot builder.</summary>
    public static SchedulerSchemaBuilder<TItem> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public SchedulerSchema<TItem> Extend(Action<SchedulerSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        SchedulerSchemaBuilder<TItem> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Strongly typed one-shot builder for Scheduler appointment metadata.</summary>
public sealed class SchedulerSchemaBuilder<TItem>
{
    private Func<TItem, DateTime>? _start;
    private Func<TItem, DateTime>? _end;
    private Func<TItem, string?>? _text;
    private int _selectedViewIndex;
    private string _height = "600px";
    private CultureInfo? _culture;
    private bool _showHeader = true;
    private bool _showNavigationButtons = true;
    private bool _showTodayButton = true;
    private bool _showDateTitle = true;
    private bool _built;

    /// <summary>Includes projection and presentation defaults from an immutable schema.</summary>
    public SchedulerSchemaBuilder<TItem> Include(SchedulerSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        _start = schema.Start;
        _end = schema.End;
        _text = schema.Text;
        _selectedViewIndex = schema.SelectedViewIndex;
        _height = schema.Height;
        _culture = schema.Culture;
        _showHeader = schema.ShowHeader;
        _showNavigationButtons = schema.ShowNavigationButtons;
        _showTodayButton = schema.ShowTodayButton;
        _showDateTitle = schema.ShowDateTitle;
        return this;
    }

    /// <summary>Maps the appointment start and end using compile-time checked expressions.</summary>
    public SchedulerSchemaBuilder<TItem> Range(
        Expression<Func<TItem, DateTime>> start,
        Expression<Func<TItem, DateTime>> end)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        _start = start.Compile();
        _end = end.Compile();
        return this;
    }

    /// <summary>Maps the appointment display text.</summary>
    public SchedulerSchemaBuilder<TItem> Text(Expression<Func<TItem, string?>> selector)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(selector);
        _text = selector.Compile();
        return this;
    }

    /// <summary>Sets the initial selected view index.</summary>
    public SchedulerSchemaBuilder<TItem> SelectedView(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        return Set(ref _selectedViewIndex, index);
    }

    /// <summary>Sets the scheduler CSS height.</summary>
    public SchedulerSchemaBuilder<TItem> Height(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _height, value);
    }

    /// <summary>Sets the scheduler formatting culture.</summary>
    public SchedulerSchemaBuilder<TItem> Culture(CultureInfo? value) => Set(ref _culture, value);

    /// <summary>Configures header visibility.</summary>
    public SchedulerSchemaBuilder<TItem> Header(
        bool visible = true,
        bool navigationButtons = true,
        bool todayButton = true,
        bool dateTitle = true)
    {
        EnsureMutable();
        _showHeader = visible;
        _showNavigationButtons = navigationButtons;
        _showTodayButton = todayButton;
        _showDateTitle = dateTitle;
        return this;
    }

    /// <summary>Builds the immutable schema.</summary>
    public SchedulerSchema<TItem> Build()
    {
        EnsureMutable();
        _built = true;
        return new SchedulerSchema<TItem>(
            _start ?? throw new InvalidOperationException("Scheduler schema requires a Start selector."),
            _end ?? throw new InvalidOperationException("Scheduler schema requires an End selector."),
            _text,
            _selectedViewIndex,
            _height,
            _culture,
            _showHeader,
            _showNavigationButtons,
            _showTodayButton,
            _showDateTitle);
    }

    private SchedulerSchemaBuilder<TItem> Set<T>(ref T target, T value)
    {
        EnsureMutable();
        target = value;
        return this;
    }

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("Scheduler schema is immutable after Build().");
    }
}
