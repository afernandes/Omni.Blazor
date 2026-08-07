using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>
/// Immutable, reusable definition of the explicitly configured fields in an
/// <c>OmniDataForm</c>. Create instances with <see cref="Create"/> or
/// <see cref="Builder"/> and reuse them across renders and form instances.
/// </summary>
public sealed class DataFormSchema<TModel> where TModel : class
{
    private readonly IReadOnlyList<DataFormField<TModel>> _fields;

    internal DataFormSchema(
        IReadOnlyList<DataFormField<TModel>> fields,
        IReadOnlyList<DataFormGroup<TModel>> groups,
        DataFormLayout layout,
        bool autoGenerateFields,
        IReadOnlyList<DataFormFieldConvention<TModel>> conventions)
    {
        _fields = fields;
        Groups = groups;
        Layout = layout;
        AutoGenerateFields = autoGenerateFields;
        Conventions = conventions;
    }

    /// <summary>
    /// Whether supported model properties not explicitly configured are generated.
    /// Conventional identifiers, key attributes and database-generated identities are
    /// excluded unless explicitly declared. Default true.
    /// </summary>
    public bool AutoGenerateFields { get; }

    /// <summary>Number of explicitly configured fields.</summary>
    public int Count => _fields.Count;

    /// <summary>Responsive root grid used by fields outside a group.</summary>
    public DataFormLayout Layout { get; }

    /// <summary>Top-level semantic groups declared by the schema.</summary>
    public IReadOnlyList<DataFormGroup<TModel>> Groups { get; }

    internal IReadOnlyList<DataFormField<TModel>> Fields => _fields;
    internal IReadOnlyList<DataFormFieldConvention<TModel>> Conventions { get; }

    /// <summary>Creates and builds an immutable schema in one expression.</summary>
    public static DataFormSchema<TModel> Create(
        Action<DataFormSchemaBuilder<TModel>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataFormSchemaBuilder<TModel> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a builder for advanced or conditional schema construction.</summary>
    public static DataFormSchemaBuilder<TModel> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public DataFormSchema<TModel> Extend(Action<DataFormSchemaBuilder<TModel>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataFormSchemaBuilder<TModel> builder = new();
        builder.Include(this);
        builder.AutoGenerateFields(AutoGenerateFields);
        builder.Layout(layout =>
        {
            layout.Columns(Layout.Columns).RowGap(Layout.RowGap).ColumnGap(Layout.ColumnGap);
            foreach ((Breakpoint breakpoint, int columns) in Layout.ResponsiveColumns)
                layout.Columns(breakpoint, columns);
        });
        configure(builder);
        return builder.Build();
    }
}

/// <summary>Fluent builder for an immutable <see cref="DataFormSchema{TModel}"/>.</summary>
public sealed class DataFormSchemaBuilder<TModel> where TModel : class
{
    private readonly List<IDataFormFieldBuilder<TModel>> _fields = [];
    private readonly List<DataFormGroupBuilder<TModel>> _groups = [];
    private readonly List<DataFormGroup<TModel>> _importedGroups = [];
    private readonly List<DataFormFieldConvention<TModel>> _conventions = [];
    private readonly HashSet<string> _propertyNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _groupIds = new(StringComparer.Ordinal);
    private readonly DataFormLayoutBuilder _layout;
    private bool _autoGenerateFields = true;
    private bool _built;

    /// <summary>Creates an empty fluent schema builder.</summary>
    public DataFormSchemaBuilder() => _layout = new DataFormLayoutBuilder(EnsureMutable);

    /// <summary>
    /// Includes supported model properties that were not explicitly configured.
    /// Conventional identifiers, key attributes and database-generated identities are
    /// excluded unless explicitly declared. Default true.
    /// </summary>
    public DataFormSchemaBuilder<TModel> AutoGenerateFields(bool enabled = true)
    {
        EnsureMutable();
        _autoGenerateFields = enabled;
        return this;
    }

    /// <summary>Adds or overrides a model property using a strongly typed selector.</summary>
    public DataFormSchemaBuilder<TModel> Field<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TValue>(
        Expression<Func<TModel, TValue>> property,
        Action<DataFormFieldBuilder<TModel, TValue>>? configure = null)
        => AddField(property, configure, groupId: null);

    /// <summary>
    /// Includes fields, groups and conventions from an immutable schema. The
    /// receiving builder keeps its own layout and auto-generation policy.
    /// </summary>
    public DataFormSchemaBuilder<TModel> Include(DataFormSchema<TModel> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);

        foreach (DataFormField<TModel> field in schema.Fields)
        {
            if (!_propertyNames.Add(field.Property))
                throw new InvalidOperationException($"OmniDataForm field '{field.Property}' was declared more than once.");
            _fields.Add(new ImportedDataFormFieldBuilder<TModel>(field));
        }

        foreach (DataFormGroup<TModel> group in schema.Groups)
        {
            RegisterImportedGroup(group);
            _importedGroups.Add(group);
        }

        _conventions.AddRange(schema.Conventions);
        return this;
    }

    /// <summary>Applies a reusable schema fragment to this builder.</summary>
    public DataFormSchemaBuilder<TModel> IncludeFragment(
        Action<DataFormSchemaBuilder<TModel>> fragment)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(fragment);
        fragment(this);
        return this;
    }

    /// <summary>Applies a reusable strongly typed schema profile.</summary>
    public DataFormSchemaBuilder<TModel> Apply(IDataFormSchemaProfile<TModel> profile)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(profile);
        profile.Configure(this);
        return this;
    }

    /// <summary>
    /// Replaces an included or locally declared field with a fresh strongly
    /// typed definition while preserving its declaration position.
    /// </summary>
    public DataFormSchemaBuilder<TModel> Override<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TValue>(
        Expression<Func<TModel, TValue>> property,
        Action<DataFormFieldBuilder<TModel, TValue>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        DataFormPropertyPath path = ResolveProperty(property);
        int index = _fields.FindIndex(field => StringComparer.Ordinal.Equals(field.PropertyPath, path.Path));
        if (index < 0)
            throw new InvalidOperationException($"OmniDataForm field '{path.Path}' cannot be overridden because it was not declared.");

        string? groupId = _fields[index].GroupId;
        DataFormFieldBuilder<TModel, TValue> replacement = new(path, groupId, EnsureMutable);
        configure(replacement);
        _fields[index] = replacement;
        return this;
    }

    /// <summary>Adds defaults for model properties with the exact value type.</summary>
    public DataFormSchemaBuilder<TModel> ConventionFor<TValue>(
        Action<DataFormConventionBuilder<TModel>> configure)
        => AddConvention(
            property => property.PropertyType == typeof(TValue),
            configure);

    /// <summary>Adds defaults for properties carrying an attribute.</summary>
    public DataFormSchemaBuilder<TModel> ConventionForAttribute<TAttribute>(
        Action<DataFormConventionBuilder<TModel>> configure)
        where TAttribute : Attribute
        => AddConvention(
            property => property.IsDefined(typeof(TAttribute), inherit: true),
            configure);

    /// <summary>Configures the responsive root grid.</summary>
    public DataFormSchemaBuilder<TModel> Layout(Action<DataFormLayoutBuilder> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_layout);
        return this;
    }

    /// <summary>Adds a semantic, nestable field group.</summary>
    public DataFormSchemaBuilder<TModel> Group(
        string title,
        Action<DataFormGroupBuilder<TModel>> configure)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(configure);
        DataFormGroupBuilder<TModel> group = CreateGroup(title);
        configure(group);
        _groups.Add(group);
        return this;
    }

    /// <summary>Builds the schema. A builder can be built only once.</summary>
    public DataFormSchema<TModel> Build()
    {
        EnsureMutable();
        _built = true;

        DataFormField<TModel>[] fields = new DataFormField<TModel>[_fields.Count];
        for (int index = 0; index < _fields.Count; index++)
        {
            fields[index] = _fields[index].Build();
        }

        DataFormGroup<TModel>[] groups = new DataFormGroup<TModel>[_importedGroups.Count + _groups.Count];
        for (int index = 0; index < _importedGroups.Count; index++)
            groups[index] = _importedGroups[index];
        for (int index = 0; index < _groups.Count; index++)
            groups[_importedGroups.Count + index] = _groups[index].Build();

        return new DataFormSchema<TModel>(
            Array.AsReadOnly(fields),
            Array.AsReadOnly(groups),
            _layout.Build(),
            _autoGenerateFields,
            Array.AsReadOnly(_conventions.ToArray()));
    }

    internal DataFormSchemaBuilder<TModel> AddField<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TValue>(
        Expression<Func<TModel, TValue>> property,
        Action<DataFormFieldBuilder<TModel, TValue>>? configure,
        string? groupId)
    {
        EnsureMutable();
        DataFormPropertyPath propertyPath = ResolveProperty(property);
        if (!_propertyNames.Add(propertyPath.Path))
        {
            throw new InvalidOperationException(
                $"OmniDataForm field '{propertyPath.Path}' was declared more than once.");
        }

        DataFormFieldBuilder<TModel, TValue> field = new(propertyPath, groupId, EnsureMutable);
        configure?.Invoke(field);
        _fields.Add(field);
        return this;
    }

    internal DataFormGroupBuilder<TModel> CreateGroup(string title)
    {
        string id = $"group-{_groupIds.Count + 1}";
        _groupIds.Add(id);
        return new DataFormGroupBuilder<TModel>(this, id, title, EnsureMutable);
    }

    internal void ReplaceGroupId(string previous, string next)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(next);
        if (StringComparer.Ordinal.Equals(previous, next)) return;
        if (!_groupIds.Add(next))
            throw new InvalidOperationException($"OmniDataForm group '{next}' was declared more than once.");
        _groupIds.Remove(previous);
    }

    private DataFormSchemaBuilder<TModel> AddConvention(
        Func<PropertyInfo, bool> predicate,
        Action<DataFormConventionBuilder<TModel>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        DataFormConventionBuilder<TModel> builder = new(EnsureMutable);
        configure(builder);
        _conventions.Add(builder.Build(predicate));
        return this;
    }

    private void RegisterImportedGroup(DataFormGroup<TModel> group)
    {
        if (!_groupIds.Add(group.Id))
            throw new InvalidOperationException($"OmniDataForm group '{group.Id}' was declared more than once.");
        foreach (DataFormGroup<TModel> child in group.Groups) RegisterImportedGroup(child);
    }

    internal static DataFormPropertyPath ResolveProperty<TValue>(
        Expression<Func<TModel, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        Expression body = expression.Body;
        while (body is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } conversion)
        {
            body = conversion.Operand;
        }

        List<PropertyInfo> reversed = [];
        Expression? current = body;
        while (current is MemberExpression member && member.Member is PropertyInfo property)
        {
            if (property.GetMethod is null || property.GetIndexParameters().Length != 0)
                break;
            reversed.Add(property);
            current = member.Expression;
            while (current is UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
                } conversion)
                current = conversion.Operand;
        }

        if (!ReferenceEquals(current, expression.Parameters[0]) || reversed.Count == 0)
        {
            throw new ArgumentException(
                "DataForm fields must select one readable, non-indexed public property path, " +
                "for example: model => model.Name or model => model.Address.City.",
                nameof(expression));
        }

        reversed.Reverse();
        PropertyInfo[] properties = reversed.ToArray();
        return new DataFormPropertyPath(
            string.Join('.', properties.Select(static property => property.Name)),
            properties,
            expression);
    }

    private void EnsureMutable()
    {
        if (_built)
        {
            throw new InvalidOperationException(
                "This DataFormSchemaBuilder has already built an immutable schema.");
        }
    }
}

/// <summary>Fluent builder for a semantic and responsive DataForm group.</summary>
public sealed class DataFormGroupBuilder<TModel> where TModel : class
{
    private readonly DataFormSchemaBuilder<TModel> _owner;
    private readonly Action _ensureMutable;
    private readonly DataFormLayoutBuilder _layout;
    private readonly List<DataFormGroupBuilder<TModel>> _groups = [];
    private string _id;
    private readonly string _title;
    private string? _description;
    private int _order;
    private bool _visible = true;
    private bool _hasContent;
    private Func<TModel, bool>? _visibleWhen;

    internal DataFormGroupBuilder(
        DataFormSchemaBuilder<TModel> owner,
        string id,
        string title,
        Action ensureMutable)
    {
        _owner = owner;
        _id = id;
        _title = title;
        _ensureMutable = ensureMutable;
        _layout = new DataFormLayoutBuilder(ensureMutable);
    }

    /// <summary>Overrides the stable group identity.</summary>
    public DataFormGroupBuilder<TModel> Id(string value)
    {
        if (_hasContent)
            throw new InvalidOperationException("Configure a DataForm group Id before adding fields or nested groups.");
        _owner.ReplaceGroupId(_id, value);
        _id = value;
        return this;
    }

    /// <summary>Sets descriptive text below the group legend.</summary>
    public DataFormGroupBuilder<TModel> Description(string? value)
        => Set(ref _description, value);

    /// <summary>Sets the display order among sibling schema nodes.</summary>
    public DataFormGroupBuilder<TModel> Order(int value) => Set(ref _order, value);

    /// <summary>Sets static group visibility.</summary>
    public DataFormGroupBuilder<TModel> Visible(bool value = true) => Set(ref _visible, value);

    /// <summary>Shows the group only while the model predicate returns true.</summary>
    public DataFormGroupBuilder<TModel> VisibleWhen(Func<TModel, bool> predicate)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(predicate);
        _visibleWhen = predicate;
        return this;
    }

    /// <summary>Configures the group's own responsive grid.</summary>
    public DataFormGroupBuilder<TModel> Layout(Action<DataFormLayoutBuilder> configure)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        configure(_layout);
        return this;
    }

    /// <summary>Adds a strongly typed field to this group.</summary>
    public DataFormGroupBuilder<TModel> Field<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TValue>(
        Expression<Func<TModel, TValue>> property,
        Action<DataFormFieldBuilder<TModel, TValue>>? configure = null)
    {
        _hasContent = true;
        _owner.AddField(property, configure, _id);
        return this;
    }

    /// <summary>Adds a nested semantic group.</summary>
    public DataFormGroupBuilder<TModel> Group(
        string title,
        Action<DataFormGroupBuilder<TModel>> configure)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(configure);
        DataFormGroupBuilder<TModel> group = _owner.CreateGroup(title);
        configure(group);
        _groups.Add(group);
        _hasContent = true;
        return this;
    }

    internal DataFormGroup<TModel> Build()
    {
        DataFormGroup<TModel>[] groups = new DataFormGroup<TModel>[_groups.Count];
        for (int index = 0; index < _groups.Count; index++) groups[index] = _groups[index].Build();
        return new DataFormGroup<TModel>(
            _id,
            _title,
            _description,
            _order,
            _visible,
            _visibleWhen,
            _layout.Build(),
            Array.AsReadOnly(groups));
    }

    private DataFormGroupBuilder<TModel> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

internal interface IDataFormFieldBuilder<TModel> where TModel : class
{
    string PropertyPath { get; }
    string? GroupId { get; }
    DataFormField<TModel> Build();
}

internal sealed class ImportedDataFormFieldBuilder<TModel>(DataFormField<TModel> sourceField)
    : IDataFormFieldBuilder<TModel>
    where TModel : class
{
    public string PropertyPath => sourceField.Property;
    public string? GroupId => sourceField.GroupId;
    public DataFormField<TModel> Build() => sourceField;
}

/// <summary>
/// Strongly typed fluent configuration for one DataForm property.
/// Editor-specific methods expose only typed values instead of parameter-name dictionaries.
/// </summary>
public sealed class DataFormFieldBuilder<TModel,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TValue>
    : IDataFormFieldBuilder<TModel>
    where TModel : class
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>(0, StringComparer.Ordinal));

    private readonly DataFormPropertyPath _propertyPath;
    private readonly string? _groupId;
    private readonly Action _ensureMutable;
    private readonly Dictionary<string, object?> _editorParameters = new(StringComparer.Ordinal);
    private readonly List<DataFormOption> _options = [];
    private string? _label;
    private string? _placeholder;
    private string? _hint;
    private string? _hintRight;
    private DataFormEditor _editor;
    private int? _order;
    private int _columnSpan = 1;
    private bool _hasExplicitColumnSpan;
    private readonly Dictionary<Breakpoint, int> _responsiveSpans = [];
    private bool _visible = true;
    private bool _hasExplicitVisible;
    private Func<TModel, bool>? _visibleWhen;
    private Func<TModel, bool>? _enabledWhen;
    private Func<TModel, bool>? _readOnlyWhen;
    private Func<TModel, bool>? _requiredWhen;
    private bool? _required;
    private bool _enforceRequired;
    private string? _requiredError;
    private bool? _disabled;
    private bool? _readOnly;
    private string? _class;
    private string? _style;
    private IDataFormFieldTemplate<TModel>? _template;
    private IDataFormLookupDefinition<TModel>? _lookup;
    private IDataFormCollectionDefinition<TModel>? _collection;
    private readonly List<IDataFormFieldValidator<TModel>> _validators = [];

    internal DataFormFieldBuilder(
        DataFormPropertyPath propertyPath,
        string? groupId,
        Action ensureMutable)
    {
        _propertyPath = propertyPath;
        _groupId = groupId;
        _ensureMutable = ensureMutable;
        AddInferredOptions();
    }

    string IDataFormFieldBuilder<TModel>.PropertyPath => _propertyPath.Path;
    string? IDataFormFieldBuilder<TModel>.GroupId => _groupId;

    /// <summary>Overrides the inferred field label.</summary>
    public DataFormFieldBuilder<TModel, TValue> Label(string? value)
        => Set(ref _label, value);

    /// <summary>Overrides the inferred editor placeholder.</summary>
    public DataFormFieldBuilder<TModel, TValue> Placeholder(string? value)
        => Set(ref _placeholder, value);

    /// <summary>Overrides the inferred helper text.</summary>
    public DataFormFieldBuilder<TModel, TValue> Hint(string? value)
        => Set(ref _hint, value);

    /// <summary>Sets text displayed on the right side of the label.</summary>
    public DataFormFieldBuilder<TModel, TValue> HintRight(string? value)
        => Set(ref _hintRight, value);

    /// <summary>Overrides the inferred field order.</summary>
    public DataFormFieldBuilder<TModel, TValue> Order(int value)
        => Set(ref _order, value);

    /// <summary>Sets the number of grid columns occupied by the field.</summary>
    public DataFormFieldBuilder<TModel, TValue> Span(int columns)
    {
        _ensureMutable();
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, 12);
        _columnSpan = columns;
        _hasExplicitColumnSpan = true;
        return this;
    }

    /// <summary>Sets the field span beginning at a standard Omni breakpoint.</summary>
    public DataFormFieldBuilder<TModel, TValue> Span(Breakpoint breakpoint, int columns)
    {
        _ensureMutable();
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, 12);
        if (breakpoint == Breakpoint.Xs)
        {
            _columnSpan = columns;
            _hasExplicitColumnSpan = true;
        }
        else _responsiveSpans[breakpoint] = columns;
        return this;
    }

    /// <summary>Sets static field visibility.</summary>
    public DataFormFieldBuilder<TModel, TValue> Visible(bool value = true)
    {
        _ensureMutable();
        _visible = value;
        _hasExplicitVisible = true;
        return this;
    }

    /// <summary>Shows the field only when the model predicate returns true.</summary>
    public DataFormFieldBuilder<TModel, TValue> VisibleWhen(Func<TModel, bool> predicate)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(predicate);
        _visibleWhen = predicate;
        return this;
    }

    /// <summary>Enables the editor only while the model predicate returns true.</summary>
    public DataFormFieldBuilder<TModel, TValue> EnabledWhen(Func<TModel, bool> predicate)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(predicate);
        _enabledWhen = predicate;
        return this;
    }

    /// <summary>Makes the editor read-only while the model predicate returns true.</summary>
    public DataFormFieldBuilder<TModel, TValue> ReadOnlyWhen(Func<TModel, bool> predicate)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(predicate);
        _readOnlyWhen = predicate;
        return this;
    }

    /// <summary>Makes the field required while the model predicate returns true.</summary>
    public DataFormFieldBuilder<TModel, TValue> RequiredWhen(
        Func<TModel, bool> predicate,
        string? error = null)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(predicate);
        if (error is not null) ArgumentException.ThrowIfNullOrWhiteSpace(error);
        _requiredWhen = predicate;
        _enforceRequired = true;
        _requiredError = error;
        return this;
    }

    /// <summary>Marks the field required and enables required-value validation.</summary>
    public DataFormFieldBuilder<TModel, TValue> Required(
        string? error = null)
    {
        _ensureMutable();
        if (error is not null) ArgumentException.ThrowIfNullOrWhiteSpace(error);
        _required = true;
        _enforceRequired = true;
        _requiredError = error;
        return this;
    }

    /// <summary>Disables the editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Disabled(bool value = true)
        => Set(ref _disabled, value);

    /// <summary>Makes the editor read-only.</summary>
    public DataFormFieldBuilder<TModel, TValue> ReadOnly(bool value = true)
        => Set(ref _readOnly, value);

    /// <summary>Appends a CSS class to the field wrapper.</summary>
    public DataFormFieldBuilder<TModel, TValue> Class(string? value)
        => Set(ref _class, value);

    /// <summary>Appends inline styles to the field wrapper.</summary>
    public DataFormFieldBuilder<TModel, TValue> Style(string? value)
        => Set(ref _style, value);

    /// <summary>Preserves editor inference from the property and its annotations.</summary>
    public DataFormFieldBuilder<TModel, TValue> Auto()
        => UseEditor(DataFormEditor.Auto);

    /// <summary>Uses a single-line text editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Text(
        Action<DataFormTextEditorBuilder>? configure = null)
    {
        EnsureStringEditor(nameof(Text));
        UseEditor(DataFormEditor.Text);
        configure?.Invoke(new DataFormTextEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses a multi-line text editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> TextArea(
        Action<DataFormTextAreaEditorBuilder>? configure = null)
    {
        EnsureStringEditor(nameof(TextArea));
        UseEditor(DataFormEditor.TextArea);
        configure?.Invoke(new DataFormTextAreaEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses a password editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Password(
        Action<DataFormPasswordEditorBuilder>? configure = null)
    {
        EnsureStringEditor(nameof(Password));
        UseEditor(DataFormEditor.Password);
        configure?.Invoke(new DataFormPasswordEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses an e-mail text editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Email(
        Action<DataFormTextEditorBuilder>? configure = null)
    {
        EnsureStringEditor(nameof(Email));
        UseEditor(DataFormEditor.Email);
        configure?.Invoke(new DataFormTextEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses a telephone text editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Telephone(
        Action<DataFormTextEditorBuilder>? configure = null)
    {
        EnsureStringEditor(nameof(Telephone));
        UseEditor(DataFormEditor.Telephone);
        configure?.Invoke(new DataFormTextEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses a URL text editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Url(
        Action<DataFormTextEditorBuilder>? configure = null)
    {
        EnsureStringEditor(nameof(Url));
        UseEditor(DataFormEditor.Url);
        configure?.Invoke(new DataFormTextEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses a culture-aware numeric editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Numeric(
        Action<DataFormNumericEditorBuilder>? configure = null)
    {
        EnsureNumericEditor();
        UseEditor(DataFormEditor.Number);
        configure?.Invoke(new DataFormNumericEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    /// <summary>Uses a date-only editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Date(
        Action<DataFormDateEditorBuilder>? configure = null)
        => ConfigureDate(DataFormEditor.Date, configure);

    /// <summary>Uses a date-and-time editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> DateTime(
        Action<DataFormDateEditorBuilder>? configure = null)
        => ConfigureDate(DataFormEditor.DateTime, configure);

    /// <summary>Uses a time-only editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Time(
        Action<DataFormDateEditorBuilder>? configure = null)
        => ConfigureDate(DataFormEditor.Time, configure);

    /// <summary>Uses a boolean checkbox editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> CheckBox()
    {
        EnsureBooleanEditor(nameof(CheckBox));
        return UseEditor(DataFormEditor.CheckBox);
    }

    /// <summary>Uses a boolean switch editor.</summary>
    public DataFormFieldBuilder<TModel, TValue> Switch()
    {
        EnsureBooleanEditor(nameof(Switch));
        return UseEditor(DataFormEditor.Switch);
    }

    /// <summary>Uses a single-choice editor with optional strongly typed options.</summary>
    public DataFormFieldBuilder<TModel, TValue> Select(
        Action<DataFormSelectEditorBuilder<TValue>>? configure = null)
    {
        UseEditor(DataFormEditor.Select);
        _lookup = null;
        configure?.Invoke(new DataFormSelectEditorBuilder<TValue>(
            _editorParameters,
            _options,
            _ensureMutable));
        return this;
    }

    /// <summary>
    /// Uses a lookup whose option item and bound value have different types.
    /// </summary>
    public DataFormFieldBuilder<TModel, TValue> Select<TItem>(
        Func<TItem, TValue> valueSelector,
        Func<TItem, string> textSelector,
        Action<DataFormLookupEditorBuilder<TModel, TItem, TValue>> configure)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(valueSelector);
        ArgumentNullException.ThrowIfNull(textSelector);
        ArgumentNullException.ThrowIfNull(configure);
        UseEditor(DataFormEditor.Select);
        _editorParameters.Clear();
        _options.Clear();
        DataFormLookupEditorBuilder<TModel, TItem, TValue> builder =
            new(valueSelector, textSelector, _ensureMutable);
        configure(builder);
        _lookup = builder.Build();
        return this;
    }

    /// <summary>Uses a bounded collection editor with nested item schemas.</summary>
    public DataFormFieldBuilder<TModel, TValue> Collection<TItem>(
        Action<DataFormCollectionEditorBuilder<TModel, TValue, TItem>> configure)
        where TItem : class
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        if (!typeof(IList<TItem>).IsAssignableFrom(typeof(TValue)))
            throw IncompatibleEditor(nameof(Collection));

        UseEditor(DataFormEditor.Collection);
        DataFormCollectionEditorBuilder<TModel, TValue, TItem> builder = new(_ensureMutable);
        configure(builder);
        _collection = builder.Build();
        return this;
    }

    /// <summary>
    /// Replaces the inferred editor with a strongly typed custom template while
    /// preserving the EditContext, ValueExpression and DataForm change pipeline.
    /// </summary>
    public DataFormFieldBuilder<TModel, TValue> Template(
        RenderFragment<DataFormFieldContext<TModel, TValue>> content)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(content);
        _template = new DataFormFieldTemplate<TModel, TValue>(content);
        return this;
    }

    /// <summary>Adds a strongly typed synchronous field validator.</summary>
    public DataFormFieldBuilder<TModel, TValue> Validate(
        Func<TValue, TModel, string?> validator)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(validator);
        _validators.Add(new DataFormFieldValidator<TModel, TValue>(validator));
        return this;
    }

    /// <summary>Adds a strongly typed cancellable asynchronous field validator.</summary>
    public DataFormFieldBuilder<TModel, TValue> ValidateAsync(
        Func<TValue, TModel, CancellationToken, ValueTask<string?>> validator)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(validator);
        _validators.Add(new DataFormFieldValidator<TModel, TValue>(validator));
        return this;
    }

    DataFormField<TModel> IDataFormFieldBuilder<TModel>.Build()
    {
        IReadOnlyDictionary<string, object?> parameters = _editorParameters.Count == 0
            ? EmptyParameters
            : new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>(_editorParameters, StringComparer.Ordinal));

        IReadOnlyList<DataFormOption>? options = _options.Count == 0
            ? null
            : Array.AsReadOnly(_options.ToArray());

        IReadOnlyDictionary<Breakpoint, int> responsiveSpans =
            new ReadOnlyDictionary<Breakpoint, int>(
                new Dictionary<Breakpoint, int>(_responsiveSpans));

        return new DataFormField<TModel>(
            _propertyPath.Path,
            _propertyPath,
            _groupId,
            _label,
            _placeholder,
            _hint,
            _hintRight,
            _editor,
            _order,
            _columnSpan,
            _hasExplicitColumnSpan,
            responsiveSpans,
            _visible,
            _hasExplicitVisible,
            _visibleWhen,
            _enabledWhen,
            _readOnlyWhen,
            _requiredWhen,
            _required,
            _enforceRequired,
            _requiredError,
            _disabled,
            _readOnly,
            _class,
            _style,
            options,
            parameters,
            _template,
            Array.AsReadOnly(_validators.ToArray()),
            _lookup,
            _collection,
            default(TValue),
            typeof(Omni.Blazor.Components.OmniDataFormFieldRenderer<TModel, TValue>));
    }

    private DataFormFieldBuilder<TModel, TValue> ConfigureDate(
        DataFormEditor editor,
        Action<DataFormDateEditorBuilder>? configure)
    {
        EnsureDateEditor(editor);
        UseEditor(editor);
        configure?.Invoke(new DataFormDateEditorBuilder(_editorParameters, _ensureMutable));
        return this;
    }

    private DataFormFieldBuilder<TModel, TValue> UseEditor(DataFormEditor editor)
    {
        _ensureMutable();
        if (_editor != editor)
        {
            _editorParameters.Clear();
            _options.Clear();
            _lookup = null;
            _collection = null;
        }
        _editor = editor;
        return this;
    }

    private DataFormFieldBuilder<TModel, TValue> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }

    private void AddInferredOptions()
    {
        Type declaredType = typeof(TValue);
        Type enumType = declaredType.IsEnum
            ? declaredType
            : Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        if (!enumType.IsEnum) return;

        Array values = Enum.GetValuesAsUnderlyingType(enumType);
        foreach (object underlyingValue in values)
        {
            object value = Enum.ToObject(enumType, underlyingValue);
            string name = Enum.GetName(enumType, value) ?? value.ToString()!;
            string label = declaredType.IsEnum
                ? declaredType.GetField(name, BindingFlags.Public | BindingFlags.Static)?
                    .GetCustomAttributes<DisplayAttribute>(inherit: false)
                    .FirstOrDefault()
                    ?.GetName() ?? name
                : name;
            _options.Add(new DataFormOption(value, label));
        }
    }

    private void EnsureStringEditor(string editor)
    {
        if (typeof(TValue) != typeof(string))
        {
            throw IncompatibleEditor(editor);
        }
    }

    private void EnsureNumericEditor()
    {
        Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if (Type.GetTypeCode(type) is not (
            TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal))
        {
            throw IncompatibleEditor(nameof(Numeric));
        }
    }

    private void EnsureDateEditor(DataFormEditor editor)
    {
        Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        bool compatible = editor switch
        {
            DataFormEditor.Date => type == typeof(DateOnly) || type == typeof(DateTime),
            DataFormEditor.DateTime => type == typeof(DateTime),
            DataFormEditor.Time => type == typeof(TimeOnly) || type == typeof(DateTime),
            _ => false
        };
        if (!compatible)
        {
            throw IncompatibleEditor(editor.ToString());
        }
    }

    private void EnsureBooleanEditor(string editor)
    {
        if (typeof(TValue) != typeof(bool))
        {
            throw IncompatibleEditor(editor);
        }
    }

    private InvalidOperationException IncompatibleEditor(string editor)
        => new(
            $"DataForm editor '{editor}' is not compatible with '{_propertyPath.Path}' " +
            $"({typeof(TValue).Name}).");
}

/// <summary>Typed options for text, e-mail, telephone and URL editors.</summary>
public sealed class DataFormTextEditorBuilder
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Action _ensureMutable;

    internal DataFormTextEditorBuilder(
        Dictionary<string, object?> parameters,
        Action ensureMutable)
    {
        _parameters = parameters;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the HTML autocomplete token.</summary>
    public DataFormTextEditorBuilder Autocomplete(string? value)
        => Set("Autocomplete", value);

    /// <summary>Shows or hides the clear button.</summary>
    public DataFormTextEditorBuilder Clearable(bool value = true)
        => Set("Clearable", value);

    /// <summary>Sets the minimum text length.</summary>
    public DataFormTextEditorBuilder MinLength(int? value)
        => Set("MinLength", value);

    /// <summary>Sets the maximum text length.</summary>
    public DataFormTextEditorBuilder MaxLength(int? value)
        => Set("MaxLength", value);

    private DataFormTextEditorBuilder Set(string name, object? value)
    {
        _ensureMutable();
        _parameters[name] = value;
        return this;
    }
}

/// <summary>Typed options for multi-line text editors.</summary>
public sealed class DataFormTextAreaEditorBuilder
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Action _ensureMutable;

    internal DataFormTextAreaEditorBuilder(
        Dictionary<string, object?> parameters,
        Action ensureMutable)
    {
        _parameters = parameters;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the visible row count.</summary>
    public DataFormTextAreaEditorBuilder Rows(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
        return Set("Rows", value);
    }

    /// <summary>Sets the maximum text length.</summary>
    public DataFormTextAreaEditorBuilder MaxLength(int? value)
        => Set("MaxLength", value);

    private DataFormTextAreaEditorBuilder Set(string name, object? value)
    {
        _ensureMutable();
        _parameters[name] = value;
        return this;
    }
}

/// <summary>Typed options for password editors.</summary>
public sealed class DataFormPasswordEditorBuilder
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Action _ensureMutable;

    internal DataFormPasswordEditorBuilder(
        Dictionary<string, object?> parameters,
        Action ensureMutable)
    {
        _parameters = parameters;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the HTML autocomplete token.</summary>
    public DataFormPasswordEditorBuilder Autocomplete(string? value)
        => Set("Autocomplete", value);

    /// <summary>Sets the minimum password length.</summary>
    public DataFormPasswordEditorBuilder MinLength(int? value)
        => Set("MinLength", value);

    /// <summary>Shows or hides the password visibility toggle.</summary>
    public DataFormPasswordEditorBuilder ShowToggle(bool value = true)
        => Set("ShowToggle", value);

    private DataFormPasswordEditorBuilder Set(string name, object? value)
    {
        _ensureMutable();
        _parameters[name] = value;
        return this;
    }
}

/// <summary>Typed options for numeric editors.</summary>
public sealed class DataFormNumericEditorBuilder
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Action _ensureMutable;

    internal DataFormNumericEditorBuilder(
        Dictionary<string, object?> parameters,
        Action ensureMutable)
    {
        _parameters = parameters;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets text before the numeric value.</summary>
    public DataFormNumericEditorBuilder Prefix(string? value) => Set("Prefix", value);

    /// <summary>Sets text after the numeric value.</summary>
    public DataFormNumericEditorBuilder Suffix(string? value) => Set("Suffix", value);

    /// <summary>Sets the minimum accepted value.</summary>
    public DataFormNumericEditorBuilder Min(decimal? value) => Set("Min", value);

    /// <summary>Sets the maximum accepted value.</summary>
    public DataFormNumericEditorBuilder Max(decimal? value) => Set("Max", value);

    /// <summary>Sets the increment/decrement step.</summary>
    public DataFormNumericEditorBuilder Step(decimal value) => Set("Step", value);

    /// <summary>Sets the number of decimal places.</summary>
    public DataFormNumericEditorBuilder Decimals(int? value) => Set("Decimals", value);

    private DataFormNumericEditorBuilder Set(string name, object? value)
    {
        _ensureMutable();
        _parameters[name] = value;
        return this;
    }
}

/// <summary>Typed options shared by date and time editors.</summary>
public sealed class DataFormDateEditorBuilder
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Action _ensureMutable;

    internal DataFormDateEditorBuilder(
        Dictionary<string, object?> parameters,
        Action ensureMutable)
    {
        _parameters = parameters;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Shows or hides the clear action.</summary>
    public DataFormDateEditorBuilder Clearable(bool value = true) => Set("Clearable", value);

    /// <summary>Sets the displayed date format.</summary>
    public DataFormDateEditorBuilder Format(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set("DateFormat", value);
    }

    /// <summary>Sets the minimum selectable date.</summary>
    public DataFormDateEditorBuilder Min(DateOnly? value) => Set("MinDate", value);

    /// <summary>Sets the maximum selectable date.</summary>
    public DataFormDateEditorBuilder Max(DateOnly? value) => Set("MaxDate", value);

    /// <summary>Enables or disables weekend dates.</summary>
    public DataFormDateEditorBuilder DisableWeekends(bool value = true)
        => Set("DisableWeekends", value);

    private DataFormDateEditorBuilder Set(string name, object? value)
    {
        _ensureMutable();
        _parameters[name] = value;
        return this;
    }
}

/// <summary>Typed options and local values for a single-choice editor.</summary>
public sealed class DataFormSelectEditorBuilder<TValue>
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly List<DataFormOption> _options;
    private readonly Action _ensureMutable;

    internal DataFormSelectEditorBuilder(
        Dictionary<string, object?> parameters,
        List<DataFormOption> options,
        Action ensureMutable)
    {
        _parameters = parameters;
        _options = options;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Replaces the local options with strongly typed values.</summary>
    public DataFormSelectEditorBuilder<TValue> Options(
        IEnumerable<TValue> values,
        Func<TValue, string> textSelector)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(textSelector);
        _options.Clear();
        foreach (TValue value in values)
        {
            _options.Add(new DataFormOption(value, textSelector(value)));
        }
        return this;
    }

    /// <summary>Adds one strongly typed option.</summary>
    public DataFormSelectEditorBuilder<TValue> Option(TValue value, string text)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(text);
        _options.Add(new DataFormOption(value, text));
        return this;
    }

    /// <summary>Shows or hides the clear action.</summary>
    public DataFormSelectEditorBuilder<TValue> Clearable(bool value = true)
    {
        _ensureMutable();
        _parameters["Clearable"] = value;
        return this;
    }

    /// <summary>Uses a cancellable and paged asynchronous option provider.</summary>
    public DataFormSelectEditorBuilder<TValue> ItemsProvider(
        OmniItemsProvider<TValue> provider,
        int pageSize = 50,
        int maxItems = 500)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(provider);
        if (pageSize is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        if (maxItems is < 1 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        _options.Clear();
        _parameters["ItemsProvider"] = provider;
        _parameters["ProviderPageSize"] = pageSize;
        _parameters["MaxProviderItems"] = maxItems;
        return this;
    }
}
