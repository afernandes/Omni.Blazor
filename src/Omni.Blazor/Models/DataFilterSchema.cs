using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Omni.Blazor.Models;

/// <summary>
/// Immutable, strongly typed definition of the fields exposed by an OmniDataFilter.
/// Reuse one schema across renders and filter instances.
/// </summary>
public sealed class DataFilterSchema<TItem>
{
    private readonly IReadOnlyDictionary<string, DataFilterField<TItem>> _fieldsById;
    private readonly IReadOnlyDictionary<string, DataFilterField<TItem>> _fieldsByPath;

    internal DataFilterSchema(
        IReadOnlyList<DataFilterField<TItem>> fields,
        int maximumDepth,
        int maximumRules)
    {
        Fields = fields;
        MaximumDepth = maximumDepth;
        MaximumRules = maximumRules;
        _fieldsById = new ReadOnlyDictionary<string, DataFilterField<TItem>>(
            fields.ToDictionary(static field => field.Id, StringComparer.Ordinal));
        _fieldsByPath = new ReadOnlyDictionary<string, DataFilterField<TItem>>(
            fields.ToDictionary(static field => field.MemberPath, StringComparer.Ordinal));
    }

    /// <summary>Filterable fields in deterministic declaration order.</summary>
    public IReadOnlyList<DataFilterField<TItem>> Fields { get; }

    /// <summary>Maximum accepted nested group depth. Default 16.</summary>
    public int MaximumDepth { get; }

    /// <summary>Maximum accepted condition and group count. Default 512.</summary>
    public int MaximumRules { get; }

    /// <summary>Creates and builds an immutable schema in one expression.</summary>
    public static DataFilterSchema<TItem> Create(Action<DataFilterSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataFilterSchemaBuilder<TItem> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a reusable schema builder.</summary>
    public static DataFilterSchemaBuilder<TItem> Builder() => new();

    /// <summary>Creates an immutable derived schema from this reusable base.</summary>
    public DataFilterSchema<TItem> Extend(Action<DataFilterSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataFilterSchemaBuilder<TItem> builder = new();
        builder.Include(this);
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a typed query against this schema.</summary>
    public DataFilterQuery<TItem> Query(
        Action<DataFilterGroupBuilder<TItem>> configure,
        FilterLogic logic = FilterLogic.And)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataFilterGroupBuilder<TItem> builder = new(this, logic);
        configure(builder);
        return builder.BuildQuery();
    }

    /// <summary>Finds a field by its stable serialized id.</summary>
    public bool TryGetField(string id, out DataFilterField<TItem>? field)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _fieldsById.TryGetValue(id, out field);
    }

    internal DataFilterField<TItem> GetField(string id)
        => _fieldsById.TryGetValue(id, out DataFilterField<TItem>? field)
            ? field
            : throw new InvalidOperationException($"DataFilter field '{id}' is not declared by the schema.");

    internal DataFilterField<TItem> GetField<TValue>(Expression<Func<TItem, TValue>> selector)
    {
        DataFilterMemberPath path = DataFilterSchemaBuilder<TItem>.ResolveMember(selector);
        return _fieldsByPath.TryGetValue(path.Path, out DataFilterField<TItem>? field)
            ? field
            : throw new InvalidOperationException(
                $"DataFilter member '{path.Path}' is not declared by the schema.");
    }
}

/// <summary>Fluent builder for an immutable <see cref="DataFilterSchema{TItem}"/>.</summary>
public sealed class DataFilterSchemaBuilder<TItem>
{
    private readonly List<IDataFilterFieldBuilder<TItem>> _fields = [];
    private readonly HashSet<string> _memberPaths = new(StringComparer.Ordinal);
    private int _maximumDepth = 16;
    private int _maximumRules = 512;
    private bool _built;

    /// <summary>Includes fields and safety limits from an immutable schema.</summary>
    public DataFilterSchemaBuilder<TItem> Include(DataFilterSchema<TItem> schema)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(schema);
        foreach (DataFilterField<TItem> field in schema.Fields)
        {
            if (!_memberPaths.Add(field.MemberPath))
                throw new InvalidOperationException($"DataFilter member '{field.MemberPath}' was declared more than once.");
            _fields.Add(new ExistingDataFilterFieldBuilder<TItem>(field));
        }
        _maximumDepth = schema.MaximumDepth;
        _maximumRules = schema.MaximumRules;
        return this;
    }

    /// <summary>Adds a strongly typed filterable member.</summary>
    public DataFilterSchemaBuilder<TItem> Field<TValue>(
        Expression<Func<TItem, TValue>> selector,
        Action<DataFilterFieldBuilder<TItem, TValue>>? configure = null)
    {
        EnsureMutable();
        DataFilterMemberPath path = ResolveMember(selector);
        if (!_memberPaths.Add(path.Path))
            throw new InvalidOperationException($"DataFilter member '{path.Path}' was declared more than once.");

        DataFilterFieldBuilder<TItem, TValue> field = new(path, EnsureMutable);
        configure?.Invoke(field);
        _fields.Add(field);
        return this;
    }

    /// <summary>Replaces an inherited field while preserving its selector position.</summary>
    public DataFilterSchemaBuilder<TItem> OverrideField<TValue>(
        Expression<Func<TItem, TValue>> selector,
        Action<DataFilterFieldBuilder<TItem, TValue>>? configure = null)
    {
        EnsureMutable();
        DataFilterMemberPath path = ResolveMember(selector);
        int index = _fields.FindIndex(field => string.Equals(field.MemberPath, path.Path, StringComparison.Ordinal));
        if (index < 0)
            throw new InvalidOperationException($"DataFilter member '{path.Path}' cannot be overridden because it is not declared.");
        DataFilterFieldBuilder<TItem, TValue> field = new(path, EnsureMutable);
        configure?.Invoke(field);
        _fields[index] = field;
        return this;
    }

    /// <summary>Removes inherited fields while preserving safety limits.</summary>
    public DataFilterSchemaBuilder<TItem> ClearFields()
    {
        EnsureMutable();
        _fields.Clear();
        _memberPaths.Clear();
        return this;
    }

    /// <summary>Bounds deserialized and programmatically built query trees.</summary>
    public DataFilterSchemaBuilder<TItem> Limits(int maximumDepth = 16, int maximumRules = 512)
    {
        EnsureMutable();
        if (maximumDepth is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        if (maximumRules is < 1 or > 10_000) throw new ArgumentOutOfRangeException(nameof(maximumRules));
        _maximumDepth = maximumDepth;
        _maximumRules = maximumRules;
        return this;
    }

    /// <summary>Builds the immutable schema. A builder can be built only once.</summary>
    public DataFilterSchema<TItem> Build()
    {
        EnsureMutable();
        _built = true;

        DataFilterField<TItem>[] fields = new DataFilterField<TItem>[_fields.Count];
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int index = 0; index < fields.Length; index++)
        {
            DataFilterField<TItem> field = _fields[index].Build();
            if (!ids.Add(field.Id))
                throw new InvalidOperationException($"DataFilter field id '{field.Id}' was declared more than once.");
            fields[index] = field;
        }

        return new DataFilterSchema<TItem>(
            Array.AsReadOnly(fields),
            _maximumDepth,
            _maximumRules);
    }

    internal static DataFilterMemberPath ResolveMember<TValue>(Expression<Func<TItem, TValue>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression body = expression.Body;
        while (body is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
            } conversion)
            body = conversion.Operand;

        List<PropertyInfo> reversed = [];
        Expression? current = body;
        while (current is MemberExpression member && member.Member is PropertyInfo property)
        {
            if (property.GetMethod is null || property.GetIndexParameters().Length != 0) break;
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
                "DataFilter fields must select one readable, non-indexed public property path, " +
                "for example: item => item.Name or item => item.Address.City.",
                nameof(expression));
        }

        reversed.Reverse();
        PropertyInfo[] properties = reversed.ToArray();
        return new DataFilterMemberPath(
            string.Join('.', properties.Select(static property => property.Name)),
            properties[^1].Name,
            Array.AsReadOnly(properties),
            expression);
    }

    private void EnsureMutable()
    {
        if (_built)
            throw new InvalidOperationException("This DataFilterSchemaBuilder has already built an immutable schema.");
    }
}

/// <summary>Strongly typed options for one filterable member.</summary>
public sealed class DataFilterFieldBuilder<TItem, TValue> : IDataFilterFieldBuilder<TItem>
{
    private readonly DataFilterMemberPath _path;
    private readonly Action _ensureMutable;
    private string _id;
    private string _title;
    private ColumnFilterType _type;
    private IReadOnlyList<FilterOperator>? _operators;
    private IReadOnlyList<object?> _options = Array.Empty<object?>();
    private Func<object?, string>? _optionText;
    private Func<TValue, string>? _customSerializer;
    private Func<string, TValue>? _customDeserializer;

    string IDataFilterFieldBuilder<TItem>.MemberPath => _path.Path;

    internal DataFilterFieldBuilder(DataFilterMemberPath path, Action ensureMutable)
    {
        _path = path;
        _ensureMutable = ensureMutable;
        _id = path.Path;
        _title = path.DefaultTitle;
        _type = DataFilterOperatorDefaults.InferType(typeof(TValue));

        Type valueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if (valueType.IsEnum)
        {
            Array values = Enum.GetValuesAsUnderlyingType(valueType);
            object?[] options = new object?[values.Length];
            for (int index = 0; index < values.Length; index++)
                options[index] = Enum.ToObject(valueType, values.GetValue(index)!);
            _options = Array.AsReadOnly(options);
        }
    }

    /// <summary>Overrides the stable id stored in serialized queries.</summary>
    public DataFilterFieldBuilder<TItem, TValue> Id(string value)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _id = value;
        return this;
    }

    /// <summary>Overrides the field label shown in the property selector.</summary>
    public DataFilterFieldBuilder<TItem, TValue> Title(string value)
    {
        _ensureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _title = value;
        return this;
    }

    /// <summary>Overrides the inferred editor/operator family.</summary>
    public DataFilterFieldBuilder<TItem, TValue> Type(ColumnFilterType value)
    {
        _ensureMutable();
        _type = value;
        return this;
    }

    /// <summary>Restricts the operators available to this field.</summary>
    public DataFilterFieldBuilder<TItem, TValue> Operators(params FilterOperator[] values)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0) throw new ArgumentException("At least one operator is required.", nameof(values));
        if (values.Distinct().Count() != values.Length)
            throw new ArgumentException("DataFilter operators must be unique.", nameof(values));
        _operators = Array.AsReadOnly((FilterOperator[])values.Clone());
        return this;
    }

    /// <summary>Uses a typed select editor with deterministic option labels.</summary>
    public DataFilterFieldBuilder<TItem, TValue> Select(
        IEnumerable<TValue> values,
        Func<TValue, string>? textSelector = null)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(values);
        TValue[] snapshot = values.ToArray();
        object?[] options = new object?[snapshot.Length];
        for (int index = 0; index < snapshot.Length; index++) options[index] = snapshot[index];
        _options = Array.AsReadOnly(options);
        _optionText = value => value is TValue typed
            ? textSelector?.Invoke(typed) ?? typed?.ToString() ?? string.Empty
            : string.Empty;
        _type = ColumnFilterType.Select;
        return this;
    }

    /// <summary>
    /// Configures a stable invariant codec for custom value types. Primitive, enum,
    /// date, time and Guid values already have built-in codecs.
    /// </summary>
    public DataFilterFieldBuilder<TItem, TValue> ValueCodec(
        Func<TValue, string> serialize,
        Func<string, TValue> deserialize)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(serialize);
        ArgumentNullException.ThrowIfNull(deserialize);
        _customSerializer = serialize;
        _customDeserializer = deserialize;
        return this;
    }

    DataFilterField<TItem> IDataFilterFieldBuilder<TItem>.Build()
    {
        IReadOnlyList<FilterOperator> operators = _operators
            ?? DataFilterOperatorDefaults.For(_type);
        LambdaExpression selector = _path.Expression;
        Func<TItem, object?> accessor = DataFilterMemberAccessor<TItem>.Create(_path.Properties);

        return new DataFilterField<TItem>(
            _id,
            _path.Path,
            _title,
            typeof(TValue),
            _type,
            operators,
            _options,
            _optionText,
            selector,
            accessor,
            Serialize,
            Deserialize);
    }

    private DataFilterValue Serialize(object? value)
    {
        if (value is null) return DataFilterValue.Null;
        TValue typed = Coerce(value);
        if (_customSerializer is not null)
            return DataFilterValue.Custom(_customSerializer(typed));
        return DataFilterValue.From(typed);
    }

    private object? Deserialize(DataFilterValue value)
    {
        if (value.Kind == DataFilterValueKind.Null) return null;
        if (_customDeserializer is not null)
        {
            if (value.Kind != DataFilterValueKind.Custom)
                throw new InvalidOperationException($"DataFilter field '{_id}' requires its custom value codec.");
            return _customDeserializer(value.Text ?? string.Empty);
        }
        return value.ConvertTo(typeof(TValue));
    }

    private static TValue Coerce(object value)
    {
        if (value is TValue typed) return typed;
        Type target = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        object converted;
        if (target == typeof(DateOnly) && value is DateTime dateTime)
            converted = DateOnly.FromDateTime(dateTime);
        else if (target == typeof(DateTime) && value is DateOnly date)
            converted = date.ToDateTime(TimeOnly.MinValue);
        else if (target == typeof(TimeOnly) && value is DateTime time)
            converted = TimeOnly.FromDateTime(time);
        else if (target.IsEnum)
            converted = value is string text
                ? Enum.Parse(target, text, ignoreCase: true)
                : Enum.ToObject(target, value);
        else
            converted = Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
        return (TValue)converted;
    }
}

/// <summary>Immutable metadata and typed accessors for one schema field.</summary>
public sealed class DataFilterField<TItem>
{
    internal DataFilterField(
        string id,
        string memberPath,
        string title,
        Type valueType,
        ColumnFilterType type,
        IReadOnlyList<FilterOperator> operators,
        IReadOnlyList<object?> options,
        Func<object?, string>? optionText,
        LambdaExpression selectorExpression,
        Func<TItem, object?> accessor,
        Func<object?, DataFilterValue> serializeValue,
        Func<DataFilterValue, object?> deserializeValue)
    {
        Id = id;
        MemberPath = memberPath;
        Title = title;
        ValueType = valueType;
        Type = type;
        Operators = operators;
        Options = options;
        OptionText = optionText ?? (static value => value?.ToString() ?? string.Empty);
        SelectorExpression = selectorExpression;
        Accessor = accessor;
        SerializeValue = serializeValue;
        DeserializeValue = deserializeValue;
    }

    /// <summary>Stable id stored in serialized queries.</summary>
    public string Id { get; }

    /// <summary>Compile-time validated member path selected by the schema.</summary>
    public string MemberPath { get; }

    /// <summary>Display label shown by the visual builder.</summary>
    public string Title { get; }

    /// <summary>CLR value type selected by the field expression.</summary>
    public Type ValueType { get; }

    /// <summary>Whether the selected CLR member accepts null values.</summary>
    public bool AllowsNull => !ValueType.IsValueType || Nullable.GetUnderlyingType(ValueType) is not null;

    /// <summary>Editor/operator family.</summary>
    public ColumnFilterType Type { get; }

    /// <summary>Allowed operators.</summary>
    public IReadOnlyList<FilterOperator> Operators { get; }

    /// <summary>Immutable select options.</summary>
    public IReadOnlyList<object?> Options { get; }

    /// <summary>Formats one select option.</summary>
    public Func<object?, string> OptionText { get; }

    internal LambdaExpression SelectorExpression { get; }
    internal Func<TItem, object?> Accessor { get; }
    internal Func<object?, DataFilterValue> SerializeValue { get; }
    internal Func<DataFilterValue, object?> DeserializeValue { get; }
}

internal interface IDataFilterFieldBuilder<TItem>
{
    string MemberPath { get; }

    DataFilterField<TItem> Build();
}

internal sealed class ExistingDataFilterFieldBuilder<TItem>(DataFilterField<TItem> definition)
    : IDataFilterFieldBuilder<TItem>
{
    public string MemberPath => definition.MemberPath;

    public DataFilterField<TItem> Build() => definition;
}

internal sealed record DataFilterMemberPath(
    string Path,
    string DefaultTitle,
    IReadOnlyList<PropertyInfo> Properties,
    LambdaExpression Expression);

internal static class DataFilterMemberAccessor<TItem>
{
    internal static Func<TItem, object?> Create(IReadOnlyList<PropertyInfo> properties)
    {
        Func<object, object?>[] getters = new Func<object, object?>[properties.Count];
        for (int index = 0; index < getters.Length; index++) getters[index] = CreateGetter(properties[index]);
        return item => Read(item, getters);
    }

    private static object? Read(TItem item, Func<object, object?>[] getters)
    {
        object? current = item;
        for (int index = 0; index < getters.Length; index++)
        {
            if (current is null) return null;
            current = getters[index](current);
        }
        return current;
    }

    private static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        UnaryExpression owner = Expression.Convert(instance, property.DeclaringType!);
        MemberExpression access = Expression.Property(owner, property);
        UnaryExpression boxed = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(boxed, instance).Compile();
    }
}

internal static class DataFilterOperatorDefaults
{
    private static readonly IReadOnlyList<FilterOperator> Text = Array.AsReadOnly(new[]
    {
        FilterOperator.Contains, FilterOperator.NotContains, FilterOperator.Equals,
        FilterOperator.NotEquals, FilterOperator.StartsWith, FilterOperator.EndsWith,
        FilterOperator.IsEmpty, FilterOperator.IsNotEmpty
    });

    private static readonly IReadOnlyList<FilterOperator> Ordered = Array.AsReadOnly(new[]
    {
        FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan,
        FilterOperator.GreaterOrEqual, FilterOperator.LessThan, FilterOperator.LessOrEqual
    });

    private static readonly IReadOnlyList<FilterOperator> Equality = Array.AsReadOnly(new[]
    {
        FilterOperator.Equals, FilterOperator.NotEquals
    });

    internal static IReadOnlyList<FilterOperator> For(ColumnFilterType type) => type switch
    {
        ColumnFilterType.Number or ColumnFilterType.Date => Ordered,
        ColumnFilterType.Boolean or ColumnFilterType.Select => Equality,
        _ => Text
    };

    internal static ColumnFilterType InferType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(bool)) return ColumnFilterType.Boolean;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(DateOnly) || type == typeof(TimeOnly)) return ColumnFilterType.Date;
        if (type.IsEnum) return ColumnFilterType.Select;
        if (IsNumeric(type)) return ColumnFilterType.Number;
        return type == typeof(string) || type == typeof(char)
            ? ColumnFilterType.Text
            : ColumnFilterType.Select;
    }

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is
        TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
        TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
}
