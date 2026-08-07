using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// A single node in an <c>OmniDataFilter</c> tree. It is either a <b>condition</b>
/// (<see cref="Property"/> + <see cref="Operator"/> + <see cref="Value"/>) or a
/// <b>group</b> (<see cref="Rules"/> is non-null) that combines its children with
/// <see cref="Logic"/>.
/// </summary>
public sealed class OmniFilterRule
{
    /// <summary>The filtered member name (for a condition). Null for a group.</summary>
    public string? Property { get; set; }

    /// <summary>The comparison operator (for a condition).</summary>
    public FilterOperator Operator { get; set; } = FilterOperator.Contains;

    /// <summary>The compared value (for a condition). Type depends on the property.</summary>
    public object? Value { get; set; }

    /// <summary>How this group's children are combined. Only meaningful when <see cref="Rules"/> is set.</summary>
    public FilterLogic Logic { get; set; } = FilterLogic.And;

    /// <summary>Child rules — non-null marks this node as a group.</summary>
    public List<OmniFilterRule>? Rules { get; set; }

    /// <summary>True when this node is a group (has <see cref="Rules"/>).</summary>
    public bool IsGroup => Rules is not null;

    /// <summary>Creates an empty condition rule.</summary>
    public static OmniFilterRule Condition() => new();

    /// <summary>Creates an empty group rule.</summary>
    public static OmniFilterRule Group(FilterLogic logic = FilterLogic.And) =>
        new() { Logic = logic, Rules = new() };
}

/// <summary>
/// Runtime metadata for one filterable field declared by a typed
/// <see cref="DataFilterSchema{TItem}"/>.
/// </summary>
public sealed class OmniFilterPropertyInfo
{
    /// <summary>Stable field id declared by the schema.</summary>
    public string Property { get; init; } = "";
    /// <summary>Friendly label shown in the property dropdown.</summary>
    public string Title { get; init; } = "";
    /// <summary>UI/operator family for this property.</summary>
    public ColumnFilterType Type { get; init; } = ColumnFilterType.Text;
    /// <summary>Available operators (defaults derived from <see cref="Type"/> when null).</summary>
    public IReadOnlyList<FilterOperator>? Operators { get; init; }
    /// <summary>Selectable values when <see cref="Type"/> is <c>Select</c>.</summary>
    public IReadOnlyList<object>? Options { get; init; }
    /// <summary>Renders an option's label (for <c>Select</c>). Defaults to <c>ToString()</c>.</summary>
    public Func<object?, string>? OptionText { get; init; }
    /// <summary>Optional compiled accessor supplied by a strongly typed schema.</summary>
    internal Func<object?, object?>? Accessor { get; init; }
}

/// <summary>Two typed values used by Between and NotBetween visual rules.</summary>
public sealed record DataFilterRangeValue
{
    /// <summary>Creates an immutable range value.</summary>
    public DataFilterRangeValue(object? lower, object? upper)
    {
        Lower = lower;
        Upper = upper;
    }

    /// <summary>Inclusive lower bound.</summary>
    public object? Lower { get; init; }

    /// <summary>Inclusive upper bound.</summary>
    public object? Upper { get; init; }
}

/// <summary>
/// Non-generic contract the recursive <c>OmniDataFilterItem</c> uses to talk back
/// to the typed <c>OmniDataFilter&lt;TItem&gt;</c> root (properties, operators,
/// localized text, change notification).
/// </summary>
internal interface IOmniDataFilterOwner
{
    IReadOnlyList<OmniFilterPropertyInfo> Properties { get; }
    bool AllowGroups { get; }
    bool Disabled { get; }
    ComponentSize FieldSize { get; }

    OmniFilterPropertyInfo? FindProperty(string? name);
    IReadOnlyList<FilterOperator> OperatorsFor(string? property);
    string OperatorText(FilterOperator op);
    bool OperatorNeedsValue(FilterOperator op);

    Task NotifyChangedAsync();

    string AddFilterText { get; }
    string AddGroupText { get; }
    string RemoveFilterText { get; }
    string AndText { get; }
    string OrText { get; }
    string PropertyPlaceholder { get; }
    string ValuePlaceholder { get; }
    string MinimumText { get; }
    string MaximumText { get; }
    string StartDateText { get; }
    string EndDateText { get; }
    string StartValueText { get; }
    string EndValueText { get; }
    string YesText { get; }
    string NoText { get; }
}
