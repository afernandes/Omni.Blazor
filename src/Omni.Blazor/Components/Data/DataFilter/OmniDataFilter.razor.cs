using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

public partial class OmniDataFilter<TItem>
{
    /// <summary>Creates a typed visual query builder.</summary>
    public OmniDataFilter()
    {
        RegisterParameter<DefinitionState>("DataFilterDefinition")
            .WithParameter(() => new DefinitionState(Schema, Query, Rules, Logic, Data, Auto))
            .WithComparer(DefinitionStateComparer.Instance)
            .WithChangeHandler(ApplyDefinitionState)
            .Attach();
    }

    // ─── Data ────────────────────────────────────────────────────────────────

    /// <summary>The items to filter.</summary>
    [Parameter] public IEnumerable<TItem>? Data { get; set; }

    /// <summary>Re-filter and raise <see cref="Filter"/> on every change. Default true.</summary>
    [Parameter] public bool Auto { get; set; } = true;

    /// <summary>
    /// Immutable strongly typed field schema used by the visual builder and query serialization.
    /// </summary>
    [Parameter, EditorRequired] public DataFilterSchema<TItem> Schema { get; set; } = default!;

    /// <summary>Immutable, versioned query snapshot.</summary>
    [Parameter] public DataFilterQuery<TItem>? Query { get; set; }

    /// <summary>Raised with a fresh immutable query after a visual edit.</summary>
    [Parameter] public EventCallback<DataFilterQuery<TItem>> QueryChanged { get; set; }

    /// <summary>Root list of rules. Two-way bindable.</summary>
    [Parameter] public List<OmniFilterRule>? Rules { get; set; }

    /// <summary>Fired when the rule tree changes (paired with <see cref="Rules"/> for two-way binding).</summary>
    [Parameter] public EventCallback<List<OmniFilterRule>> RulesChanged { get; set; }

    /// <summary>Logic combining the root rules. Two-way bindable.</summary>
    [Parameter] public FilterLogic Logic { get; set; } = FilterLogic.And;

    /// <summary>Fired when the root logic changes (paired with <see cref="Logic"/> for two-way binding).</summary>
    [Parameter] public EventCallback<FilterLogic> LogicChanged { get; set; }

    /// <summary>Raised with the filtered items (Auto mode, or when ApplyFilterAsync runs).</summary>
    [Parameter] public EventCallback<IEnumerable<TItem>> Filter { get; set; }

    /// <summary>Raised with the filtered items alongside <see cref="Filter"/> (binding-friendly).</summary>
    [Parameter] public EventCallback<IEnumerable<TItem>> ViewChanged { get; set; }

    // ─── Behaviour / text ──────────────────────────────────────────────────────

    /// <summary>Allow nested groups (the "add group" button). Default true.</summary>
    [Parameter] public bool AllowGroups { get; set; } = true;

    /// <summary>Height/typography of every field (selects, inputs, date &amp; list editors). Default Sm.</summary>
    [Parameter] public ComponentSize Size { get; set; } = ComponentSize.Sm;

    /// <summary>Disable the whole builder.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Label of the "add condition" button. Default <c>"Adicionar condição"</c>.</summary>
    [Parameter] public string? AddFilterText { get; set; }
    private string EffAddFilterText => AddFilterText ?? Texts.AddCondition;

    /// <summary>Label of the "add group" button. Default <c>"Adicionar grupo"</c>.</summary>
    [Parameter] public string? AddGroupText { get; set; }
    private string EffAddGroupText => AddGroupText ?? Texts.AddGroup;

    /// <summary>Title/aria-label of the remove-condition button. Default <c>"Remover"</c>.</summary>
    [Parameter] public string? RemoveFilterText { get; set; }
    private string EffRemoveFilterText => RemoveFilterText ?? Texts.Remove;

    /// <summary>Label of the "clear all" button. Default <c>"Limpar tudo"</c>.</summary>
    [Parameter] public string? ClearFilterText { get; set; }
    private string EffClearFilterText => ClearFilterText ?? Texts.ClearAll;

    /// <summary>Label of the apply button (shown when <see cref="Auto"/> is false). Default <c>"Aplicar"</c>.</summary>
    [Parameter] public string? ApplyFilterText { get; set; }
    private string EffApplyFilterText => ApplyFilterText ?? Texts.Apply;

    /// <summary>Label of the AND logic toggle. Default <c>"E"</c>.</summary>
    [Parameter] public string AndOperatorText { get; set; } = "E";

    /// <summary>Label of the OR logic toggle. Default <c>"OU"</c>.</summary>
    [Parameter] public string OrOperatorText { get; set; } = "OU";

    /// <summary>Placeholder of the condition value inputs. Default <c>"valor"</c>.</summary>
    [Parameter] public string ValuePlaceholder { get; set; } = "valor";

    // ─── SQL mode ──────────────────────────────────────────────────────────────

    /// <summary>Show the Visual/SQL toggle, enabling an editable SQL view of the filter. Default false.</summary>
    [Parameter] public bool AllowSqlMode { get; set; }

    /// <summary>Show a read-only "generated SQL" disclosure under the visual builder. Default false.</summary>
    [Parameter] public bool ShowSqlPreview { get; set; }

    /// <summary>Label of the visual-mode toggle. Default <c>"Visual"</c>.</summary>
    [Parameter] public string VisualModeText { get; set; } = "Visual";

    /// <summary>Label of the SQL-mode toggle. Default <c>"SQL"</c>.</summary>
    [Parameter] public string SqlModeText { get; set; } = "SQL";

    /// <summary>Label of the "apply SQL to filter" button. Default <c>"Aplicar ao filtro"</c>.</summary>
    [Parameter] public string? ApplySqlText { get; set; }
    private string EffApplySqlText => ApplySqlText ?? Texts.ApplyToFilter;

    /// <summary>Status text shown when the typed SQL is valid. Default <c>"SQL válido"</c>.</summary>
    [Parameter] public string SqlValidText { get; set; } = "SQL válido";

    /// <summary>Summary label of the generated-SQL disclosure. Default <c>"SQL gerado"</c>.</summary>
    [Parameter] public string SqlPreviewLabel { get; set; } = "SQL gerado";

    /// <summary>Label of the copy button. Default <c>"Copiar"</c>.</summary>
    [Parameter] public string? CopyText { get; set; }
    private string EffCopyText => CopyText ?? Texts.Copy;

    /// <summary>Label preceding the available-fields chips in SQL mode. Default <c>"Campos"</c>.</summary>
    [Parameter] public string FieldsLabel { get; set; } = "Campos";

    /// <summary>Placeholder of the SQL textarea. Default an example expression.</summary>
    [Parameter] public string SqlPlaceholder { get; set; } = "ex.: Nome LIKE '%texto%' AND Idade >= 18";

    // ─── State ─────────────────────────────────────────────────────────────────

    private readonly OmniFilterRule _root = OmniFilterRule.Group();
    private readonly List<OmniFilterPropertyInfo> _properties = new();
    private readonly Dictionary<string, Func<object?, object?>> _schemaAccessors = new(StringComparer.Ordinal);
    private FilterLogic _lastLogic = FilterLogic.And;
    private Func<TItem, bool> _predicate = _ => true;
    private DataFilterSchema<TItem>? _lastSchema;
    private DataFilterQuery<TItem>? _lastQuery;

    private bool _sqlMode;
    private string _sqlText = "";
    private string? _sqlError;

    private bool HasRules => _root.Rules!.Count > 0;

    /// <summary>The compiled predicate for the current filter tree.</summary>
    public Func<TItem, bool> Predicate => _predicate;

    /// <summary>The filtered items (recomputed whenever the filter changes).</summary>
    public IEnumerable<TItem> View { get; private set; } = Enumerable.Empty<TItem>();

    private string RootCss => CssBuilder.Default("omni-datafilter")
        .AddClass("omni-datafilter-sm", Size == ComponentSize.Sm)
        .AddClass("omni-datafilter-lg", Size == ComponentSize.Lg)
        .AddClass("omni-datafilter-disabled", Disabled)
        .AddClass(Class)
        .Build();

    private bool _initialApplied;
    private IEnumerable<TItem>? _lastData;

    private void ApplyDefinitionState(ParameterChangedEventArgs<DefinitionState> change)
    {
        if (Schema is null)
            throw new InvalidOperationException("OmniDataFilter requires a strongly typed Schema.");

        DefinitionState state = change.Value;
        bool schemaChanged = !ReferenceEquals(Schema, _lastSchema);
        bool queryChangedExternally = Query is not null &&
                                      (schemaChanged || !ReferenceEquals(Query, _lastQuery));
        bool legacyFilterChanged = Query is null &&
                                   (!ReferenceEquals(state.Rules, change.LastValue.Rules) ||
                                    state.Logic != change.LastValue.Logic);
        if (schemaChanged)
        {
            _lastSchema = Schema;
            ApplySchema();
        }

        if (Query is not null)
        {
            if (queryChangedExternally)
            {
                Query.Validate(Schema);
                AdoptQuery(Query);
                _lastQuery = Query;
            }
        }
        // Adopt a consumer-provided rules list (and keep using the same reference after).
        else if (Rules is not null && !ReferenceEquals(Rules, _root.Rules))
        {
            _root.Rules = Rules;
            _lastQuery = null;
        }
        // Only honour the Logic parameter when it actually changed — otherwise an
        // uncontrolled toggle would be reset on every render.
        if (Query is null && Logic != _lastLogic) { _root.Logic = Logic; _lastLogic = Logic; }

        // Controlled query/rule updates and data swaps must immediately refresh
        // Predicate/View. EmitAsync itself only publishes callbacks in Auto mode.
        bool dataChanged = !ReferenceEquals(state.Data, _lastData);
        bool autoEnabled = state.Auto && !change.LastValue.Auto;
        if (_initialApplied && (queryChangedExternally || legacyFilterChanged || dataChanged || autoEnabled))
        {
            _lastData = state.Data;
            ObserveTask(InvokeAsync(EmitAsync), "OmniDataFilter.Emit");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Apply any code-seeded Rules and initialise View/Predicate immediately,
            // so a pre-set filter takes effect without requiring a user edit.
            _initialApplied = true;
            _lastData = Data;
            await EmitAsync();
        }
    }

    // Recompute + publish the view (no RulesChanged/LogicChanged — the rules were
    // not edited by the user here). Used for the initial apply and data swaps.
    private async Task EmitAsync()
    {
        Recompute();
        StateHasChanged();
        if (Auto)
        {
            if (Filter.HasDelegate) await Filter.InvokeAsync(View);
            if (ViewChanged.HasDelegate) await ViewChanged.InvokeAsync(View);
        }
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    /// <summary>Recomputes <see cref="View"/> and raises <see cref="Filter"/> (use when Auto is false).</summary>
    public async Task ApplyFilterAsync()
    {
        Recompute();
        StateHasChanged();
        if (Filter.HasDelegate) await Filter.InvokeAsync(View);
        if (ViewChanged.HasDelegate) await ViewChanged.InvokeAsync(View);
    }

    /// <summary>Captures the current visual tree as an immutable typed query.</summary>
    public DataFilterQuery<TItem> CaptureQuery()
    {
        DataFilterQuery<TItem> query = new(
            DataFilterQuery<TItem>.CurrentVersion,
            CaptureGroup(_root, Schema));
        query.Validate(Schema);
        return query;
    }

    /// <summary>Applies and publishes a validated immutable query.</summary>
    public async Task ApplyQueryAsync(DataFilterQuery<TItem> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        query.Validate(Schema);
        AdoptQuery(query);
        _lastQuery = query;
        Query = query;
        await NotifyChangedAsync();
    }

    private async Task ClearAsync()
    {
        _root.Rules!.Clear();
        await NotifyChangedAsync();
    }

    // ─── SQL mode ────────────────────────────────────────────────────────────

    private string CurrentSql => FilterSqlConverter.ToSql(_root.Rules!, _root.Logic, _properties);

    private string ModeCss(bool sql) => CssBuilder.Default("omni-datafilter-mode")
        .AddClass("omni-active", _sqlMode == sql).Build();

    private string SqlStatusCss => CssBuilder.Default("omni-datafilter-sql-status")
        .AddClass("omni-datafilter-sql-invalid", _sqlError is not null).Build();

    private Task SetSqlModeAsync(bool sql)
    {
        if (_sqlMode == sql) return Task.CompletedTask;
        if (sql) { _sqlText = CurrentSql; ValidateSql(); }  // seed from the live tree
        _sqlMode = sql;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void OnSqlInput(ChangeEventArgs e)
    {
        _sqlText = e.Value?.ToString() ?? "";
        ValidateSql();
    }

    private void ValidateSql()
        => FilterSqlConverter.TryParse(_sqlText, _properties, out _, out _, out _sqlError);

    private async Task ApplySqlAsync()
    {
        if (!FilterSqlConverter.TryParse(_sqlText, _properties, out var parsed, out var logic, out var error))
        {
            _sqlError = error;
            StateHasChanged();
            return;
        }
        // Mutate the bound list in place so the consumer's Rules reference stays valid.
        _root.Rules!.Clear();
        _root.Rules.AddRange(parsed);
        _root.Logic = logic;
        _sqlError = null;
        _sqlMode = false;
        await NotifyChangedAsync();
    }

    private async Task CopySqlAsync() => await CopyAsync(_sqlText);
    private async Task CopyPreviewAsync() => await CopyAsync(CurrentSql);
    private async Task CopyAsync(string text)
    {
        await Clipboard.TryWriteTextAsync(text);
    }

    // ─── Predicate building ────────────────────────────────────────────────────

    private void Recompute()
    {
        _predicate = BuildGroup(_root.Rules!, _root.Logic);
        View = Data is null ? Enumerable.Empty<TItem>() : Data.Where(_predicate).ToList();
    }

    private Func<TItem, bool> BuildGroup(List<OmniFilterRule> rules, FilterLogic logic)
    {
        var preds = new List<Func<TItem, bool>>();
        foreach (var r in rules)
        {
            var p = BuildRule(r);
            if (p is not null) preds.Add(p);
        }
        if (preds.Count == 0) return _ => true;
        Func<TItem, bool>[] snapshot = preds.ToArray();
        return logic == FilterLogic.And
            ? item => All(snapshot, item)
            : item => Any(snapshot, item);
    }

    private Func<TItem, bool>? BuildRule(OmniFilterRule r)
    {
        if (r.IsGroup) return BuildGroup(r.Rules ?? new(), r.Logic);
        if (string.IsNullOrEmpty(r.Property)) return null;
        var needsValue = OperatorNeedsValueImpl(r.Operator);
        if (needsValue && (r.Value is null || (r.Value is string s && s.Length == 0))) return null;
        var prop = r.Property!;
        var op = r.Operator;
        var val = r.Value;
        if (val is DataFilterRangeValue range)
            return item => DataFilterEvaluator.Matches(GetMember(item, prop), op, range.Lower, range.Upper);
        return item => DataFilterEvaluator.Matches(GetMember(item, prop), op, val);
    }

    private static bool All(Func<TItem, bool>[] predicates, TItem item)
    {
        for (int index = 0; index < predicates.Length; index++)
            if (!predicates[index](item)) return false;
        return true;
    }

    private static bool Any(Func<TItem, bool>[] predicates, TItem item)
    {
        for (int index = 0; index < predicates.Length; index++)
            if (predicates[index](item)) return true;
        return false;
    }

    private object? GetMember(TItem item, string name)
    {
        if (_schemaAccessors.TryGetValue(name, out Func<object?, object?>? accessor))
            return accessor(item);
        throw new InvalidOperationException($"DataFilter field '{name}' is not declared by the schema.");
    }

    private static IReadOnlyList<FilterOperator> DefaultOperators(ColumnFilterType type) => type switch
    {
        ColumnFilterType.Number => new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.GreaterOrEqual, FilterOperator.LessThan, FilterOperator.LessOrEqual },
        ColumnFilterType.Date => new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan, FilterOperator.GreaterOrEqual, FilterOperator.LessThan, FilterOperator.LessOrEqual },
        ColumnFilterType.Boolean => new[] { FilterOperator.Equals, FilterOperator.NotEquals },
        ColumnFilterType.Select => new[] { FilterOperator.Equals, FilterOperator.NotEquals },
        _ => new[] { FilterOperator.Contains, FilterOperator.NotContains, FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.StartsWith, FilterOperator.EndsWith, FilterOperator.IsEmpty, FilterOperator.IsNotEmpty }
    };

    private static bool OperatorNeedsValueImpl(FilterOperator op) =>
        op != FilterOperator.IsEmpty && op != FilterOperator.IsNotEmpty;

    // ─── IOmniDataFilterOwner ─────────────────────────────────────────────────

    IReadOnlyList<OmniFilterPropertyInfo> IOmniDataFilterOwner.Properties => _properties;
    bool IOmniDataFilterOwner.AllowGroups => AllowGroups;
    bool IOmniDataFilterOwner.Disabled => Disabled;
    ComponentSize IOmniDataFilterOwner.FieldSize => Size;
    string IOmniDataFilterOwner.AddFilterText => EffAddFilterText;
    string IOmniDataFilterOwner.AddGroupText => EffAddGroupText;
    string IOmniDataFilterOwner.RemoveFilterText => EffRemoveFilterText;
    string IOmniDataFilterOwner.AndText => AndOperatorText;
    string IOmniDataFilterOwner.OrText => OrOperatorText;
    string IOmniDataFilterOwner.PropertyPlaceholder => "Campo";
    string IOmniDataFilterOwner.ValuePlaceholder => ValuePlaceholder;
    string IOmniDataFilterOwner.MinimumText => Texts.DataFilterMinimum;
    string IOmniDataFilterOwner.MaximumText => Texts.DataFilterMaximum;
    string IOmniDataFilterOwner.StartDateText => Texts.DataFilterStartDate;
    string IOmniDataFilterOwner.EndDateText => Texts.DataFilterEndDate;
    string IOmniDataFilterOwner.StartValueText => Texts.DataFilterStartValue;
    string IOmniDataFilterOwner.EndValueText => Texts.DataFilterEndValue;
    string IOmniDataFilterOwner.YesText => Texts.Yes;
    string IOmniDataFilterOwner.NoText => Texts.No;

    OmniFilterPropertyInfo? IOmniDataFilterOwner.FindProperty(string? name) =>
        name is null ? null : _properties.FirstOrDefault(p => p.Property == name);

    IReadOnlyList<FilterOperator> IOmniDataFilterOwner.OperatorsFor(string? property)
    {
        var info = property is null ? null : _properties.FirstOrDefault(p => p.Property == property);
        if (info?.Operators is { Count: > 0 }) return info.Operators;
        return DefaultOperators(info?.Type ?? ColumnFilterType.Text);
    }

    string IOmniDataFilterOwner.OperatorText(FilterOperator op) => op switch
    {
        FilterOperator.Contains => "Contém",
        FilterOperator.NotContains => "Não contém",
        FilterOperator.Equals => "Igual a",
        FilterOperator.NotEquals => "Diferente de",
        FilterOperator.StartsWith => "Começa com",
        FilterOperator.EndsWith => "Termina com",
        FilterOperator.GreaterThan => "Maior que",
        FilterOperator.GreaterOrEqual => "Maior ou igual",
        FilterOperator.LessThan => "Menor que",
        FilterOperator.LessOrEqual => "Menor ou igual",
        FilterOperator.Between => "Entre",
        FilterOperator.NotBetween => "Fora do intervalo",
        FilterOperator.IsEmpty => "Vazio",
        FilterOperator.IsNotEmpty => "Não vazio",
        _ => op.ToString()
    };

    bool IOmniDataFilterOwner.OperatorNeedsValue(FilterOperator op) => OperatorNeedsValueImpl(op);

    async Task IOmniDataFilterOwner.NotifyChangedAsync() => await NotifyChangedAsync();

    private async Task NotifyChangedAsync()
    {
        Recompute();
        StateHasChanged();
        if (RulesChanged.HasDelegate) await RulesChanged.InvokeAsync(_root.Rules);
        if (Logic != _root.Logic) { Logic = _root.Logic; _lastLogic = _root.Logic; }
        if (LogicChanged.HasDelegate) await LogicChanged.InvokeAsync(_root.Logic);
        DataFilterQuery<TItem> next = CaptureQuery();
        Query = next;
        _lastQuery = next;
        if (QueryChanged.HasDelegate) await QueryChanged.InvokeAsync(next);
        if (Auto)
        {
            if (Filter.HasDelegate) await Filter.InvokeAsync(View);
            if (ViewChanged.HasDelegate) await ViewChanged.InvokeAsync(View);
        }
    }

    private void ApplySchema()
    {
        _properties.Clear();
        _schemaAccessors.Clear();

        foreach (DataFilterField<TItem> field in Schema.Fields)
        {
            object[] options = field.Options.Where(static value => value is not null).Cast<object>().ToArray();
            _properties.Add(new OmniFilterPropertyInfo
            {
                Property = field.Id,
                Title = field.Title,
                Type = field.Type,
                Operators = field.Operators,
                Options = Array.AsReadOnly(options),
                OptionText = field.OptionText,
                Accessor = value => value is TItem item ? field.Accessor(item) : null
            });
            _schemaAccessors.Add(
                field.Id,
                value => value is TItem item ? field.Accessor(item) : null);
        }
    }

    private void AdoptQuery(DataFilterQuery<TItem> query)
    {
        DataFilterSchema<TItem> schema = Schema;
        _root.Rules = query.Root.Rules.Select(rule => ToVisualRule(rule, schema)).ToList();
        _root.Logic = query.Root.Logic;
        Logic = query.Root.Logic;
        _lastLogic = query.Root.Logic;
    }

    private static OmniFilterRule ToVisualRule(
        DataFilterQueryRule rule,
        DataFilterSchema<TItem> schema)
    {
        if (rule.Group is not null)
        {
            return new OmniFilterRule
            {
                Logic = rule.Group.Logic,
                Rules = rule.Group.Rules.Select(child => ToVisualRule(child, schema)).ToList()
            };
        }

        DataFilterField<TItem> field = schema.GetField(rule.Field!);
        object? lower = rule.Value is null ? null : field.DeserializeValue(rule.Value);
        object? value = rule.Operator is FilterOperator.Between or FilterOperator.NotBetween
            ? new DataFilterRangeValue(
                lower,
                rule.UpperValue is null ? null : field.DeserializeValue(rule.UpperValue))
            : lower;
        return new OmniFilterRule
        {
            Property = field.Id,
            Operator = rule.Operator,
            Value = value
        };
    }

    private static DataFilterQueryGroup CaptureGroup(
        OmniFilterRule group,
        DataFilterSchema<TItem> schema)
    {
        List<DataFilterQueryRule> rules = new(group.Rules?.Count ?? 0);
        if (group.Rules is not null)
        {
            foreach (OmniFilterRule rule in group.Rules)
            {
                if (rule.IsGroup)
                {
                    rules.Add(DataFilterQueryRule.Nested(CaptureGroup(rule, schema)));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rule.Property)) continue;
                DataFilterField<TItem> field = schema.GetField(rule.Property);
                DataFilterValue? lower = null;
                DataFilterValue? upper = null;
                if (DataFilterQuery<TItem>.RequiresValue(rule.Operator))
                {
                    if (rule.Value is DataFilterRangeValue range)
                    {
                        if (range.Lower is null || range.Upper is null) continue;
                        lower = field.SerializeValue(range.Lower);
                        upper = field.SerializeValue(range.Upper);
                    }
                    else
                    {
                        if (rule.Value is null || rule.Value is string { Length: 0 }) continue;
                        lower = field.SerializeValue(rule.Value);
                    }
                }
                rules.Add(DataFilterQueryRule.Condition(field.Id, rule.Operator, lower, upper));
            }
        }
        return new DataFilterQueryGroup(group.Logic, rules);
    }

    private readonly record struct DefinitionState(
        DataFilterSchema<TItem>? Schema,
        DataFilterQuery<TItem>? Query,
        List<OmniFilterRule>? Rules,
        FilterLogic Logic,
        IEnumerable<TItem>? Data,
        bool Auto);

    private sealed class DefinitionStateComparer : IEqualityComparer<DefinitionState>
    {
        internal static DefinitionStateComparer Instance { get; } = new();

        public bool Equals(DefinitionState x, DefinitionState y)
            => ReferenceEquals(x.Schema, y.Schema)
               && ReferenceEquals(x.Query, y.Query)
               && ReferenceEquals(x.Rules, y.Rules)
               && x.Logic == y.Logic
               && ReferenceEquals(x.Data, y.Data)
               && x.Auto == y.Auto;

        public int GetHashCode(DefinitionState value)
            => HashCode.Combine(
                value.Schema is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value.Schema),
                value.Query is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value.Query),
                value.Rules is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value.Rules),
                value.Logic,
                value.Data is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value.Data),
                value.Auto);
    }
}
