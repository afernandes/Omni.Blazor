using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Omni.Blazor.Serialization;

namespace Omni.Blazor.Models;

/// <summary>Stable primitive representation used by serialized DataFilter conditions.</summary>
public sealed class DataFilterValue
{
    /// <summary>Creates a serialized value.</summary>
    [JsonConstructor]
    public DataFilterValue(DataFilterValueKind kind, string? text)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == DataFilterValueKind.Null && text is not null)
            throw new ArgumentException("A null DataFilter value cannot contain text.", nameof(text));
        if (kind != DataFilterValueKind.Null && text is null)
            throw new ArgumentException("A non-null DataFilter value requires text.", nameof(text));
        Kind = kind;
        Text = text;
    }

    /// <summary>Serialized primitive family.</summary>
    public DataFilterValueKind Kind { get; }

    /// <summary>Invariant text payload. Null only for <see cref="DataFilterValueKind.Null"/>.</summary>
    public string? Text { get; }

    /// <summary>Canonical null value.</summary>
    public static DataFilterValue Null { get; } = new(DataFilterValueKind.Null, null);

    /// <summary>Creates a value owned by a field-specific codec.</summary>
    public static DataFilterValue Custom(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new DataFilterValue(DataFilterValueKind.Custom, text);
    }

    /// <summary>Converts one supported CLR primitive to its invariant representation.</summary>
    public static DataFilterValue From(object? value) => value switch
    {
        null => Null,
        string text => new(DataFilterValueKind.String, text),
        char character => new(DataFilterValueKind.String, character.ToString()),
        bool boolean => new(DataFilterValueKind.Boolean, boolean ? "true" : "false"),
        sbyte or short or int or long => new(
            DataFilterValueKind.SignedInteger,
            Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)),
        byte or ushort or uint or ulong => new(
            DataFilterValueKind.UnsignedInteger,
            Convert.ToUInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)),
        decimal number => new(DataFilterValueKind.Decimal, number.ToString(CultureInfo.InvariantCulture)),
        float number => new(DataFilterValueKind.Double, number.ToString("R", CultureInfo.InvariantCulture)),
        double number => new(DataFilterValueKind.Double, number.ToString("R", CultureInfo.InvariantCulture)),
        DateTime dateTime => new(DataFilterValueKind.DateTime, dateTime.ToString("O", CultureInfo.InvariantCulture)),
        DateTimeOffset dateTimeOffset => new(DataFilterValueKind.DateTimeOffset, dateTimeOffset.ToString("O", CultureInfo.InvariantCulture)),
        DateOnly date => new(DataFilterValueKind.DateOnly, date.ToString("O", CultureInfo.InvariantCulture)),
        TimeOnly time => new(DataFilterValueKind.TimeOnly, time.ToString("O", CultureInfo.InvariantCulture)),
        Guid guid => new(DataFilterValueKind.Guid, guid.ToString("D")),
        Enum enumeration => new(DataFilterValueKind.Enum, enumeration.ToString()),
        _ => throw new NotSupportedException(
            $"DataFilter value type '{value.GetType()}' needs a field ValueCodec.")
    };

    internal object? ConvertTo(Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        Type target = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        if (Kind == DataFilterValueKind.Null) return null;
        string text = Text ?? throw new InvalidOperationException("A non-null DataFilter value has no payload.");

        if (target == typeof(string)) return text;
        if (target == typeof(char)) return text.Length == 1
            ? text[0]
            : throw new FormatException("A DataFilter character must contain exactly one character.");
        if (target.IsEnum)
        {
            if (Kind is not (DataFilterValueKind.Enum or DataFilterValueKind.String))
                throw Incompatible(destinationType);
            return Enum.Parse(target, text, ignoreCase: true);
        }

        return Type.GetTypeCode(target) switch
        {
            TypeCode.Boolean when Kind == DataFilterValueKind.Boolean => bool.Parse(text),
            TypeCode.SByte when IsInteger => sbyte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.Byte when IsInteger => byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.Int16 when IsInteger => short.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.UInt16 when IsInteger => ushort.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.Int32 when IsInteger => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.UInt32 when IsInteger => uint.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.Int64 when IsInteger => long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.UInt64 when IsInteger => ulong.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            TypeCode.Decimal when IsNumber => decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture),
            TypeCode.Single when IsNumber => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            TypeCode.Double when IsNumber => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            TypeCode.DateTime when Kind == DataFilterValueKind.DateTime =>
                DateTime.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => ConvertNonTypeCode(target, text)
        };
    }

    private bool IsInteger => Kind is DataFilterValueKind.SignedInteger or DataFilterValueKind.UnsignedInteger;
    private bool IsNumber => IsInteger || Kind is DataFilterValueKind.Decimal or DataFilterValueKind.Double;

    private object ConvertNonTypeCode(Type target, string text)
    {
        if (target == typeof(DateTimeOffset) && Kind == DataFilterValueKind.DateTimeOffset)
            return DateTimeOffset.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (target == typeof(DateOnly) && Kind == DataFilterValueKind.DateOnly)
            return DateOnly.ParseExact(text, "O", CultureInfo.InvariantCulture);
        if (target == typeof(TimeOnly) && Kind == DataFilterValueKind.TimeOnly)
            return TimeOnly.ParseExact(text, "O", CultureInfo.InvariantCulture);
        if (target == typeof(Guid) && Kind == DataFilterValueKind.Guid)
            return Guid.ParseExact(text, "D");
        throw Incompatible(target);
    }

    private InvalidOperationException Incompatible(Type destinationType)
        => new($"Serialized DataFilter value kind '{Kind}' is incompatible with '{destinationType}'.");
}

/// <summary>Supported stable serialized value families.</summary>
public enum DataFilterValueKind
{
    /// <summary>Null.</summary>
    Null,
    /// <summary>Unicode text or a character.</summary>
    String,
    /// <summary>Boolean.</summary>
    Boolean,
    /// <summary>Signed integral number.</summary>
    SignedInteger,
    /// <summary>Unsigned integral number.</summary>
    UnsignedInteger,
    /// <summary>Decimal number.</summary>
    Decimal,
    /// <summary>Single or double precision number.</summary>
    Double,
    /// <summary>Date and time with DateTime semantics.</summary>
    DateTime,
    /// <summary>Date and time with offset.</summary>
    DateTimeOffset,
    /// <summary>Date without time.</summary>
    DateOnly,
    /// <summary>Time without date.</summary>
    TimeOnly,
    /// <summary>Guid.</summary>
    Guid,
    /// <summary>Enum member name.</summary>
    Enum,
    /// <summary>Payload owned by a field-specific codec.</summary>
    Custom
}

/// <summary>Immutable group in a typed DataFilter query tree.</summary>
public sealed class DataFilterQueryGroup
{
    /// <summary>Creates an immutable group snapshot.</summary>
    [JsonConstructor]
    public DataFilterQueryGroup(FilterLogic logic, IReadOnlyList<DataFilterQueryRule>? rules)
    {
        if (!Enum.IsDefined(logic)) throw new ArgumentOutOfRangeException(nameof(logic));
        DataFilterQueryRule[] snapshot = rules?.ToArray() ?? Array.Empty<DataFilterQueryRule>();
        if (Array.Exists(snapshot, static rule => rule is null))
            throw new ArgumentException("A DataFilter group cannot contain a null rule.", nameof(rules));
        Logic = logic;
        Rules = Array.AsReadOnly(snapshot);
    }

    /// <summary>How direct children are combined.</summary>
    public FilterLogic Logic { get; }

    /// <summary>Immutable direct child conditions and groups.</summary>
    public IReadOnlyList<DataFilterQueryRule> Rules { get; }
}

/// <summary>Immutable condition or nested group in a typed DataFilter query.</summary>
public sealed class DataFilterQueryRule
{
    /// <summary>Creates one serialized query node.</summary>
    [JsonConstructor]
    public DataFilterQueryRule(
        string? field,
        FilterOperator @operator,
        DataFilterValue? value,
        DataFilterValue? upperValue,
        DataFilterQueryGroup? group)
    {
        Field = field;
        Operator = @operator;
        Value = value;
        UpperValue = upperValue;
        Group = group;
    }

    /// <summary>Stable schema field id. Null for groups.</summary>
    public string? Field { get; }

    /// <summary>Condition operator.</summary>
    public FilterOperator Operator { get; }

    /// <summary>Condition value.</summary>
    public DataFilterValue? Value { get; }

    /// <summary>Upper value used by Between and NotBetween.</summary>
    public DataFilterValue? UpperValue { get; }

    /// <summary>Nested group. Non-null marks this node as a group.</summary>
    public DataFilterQueryGroup? Group { get; }

    /// <summary>Whether this node contains a nested group.</summary>
    [JsonIgnore]
    public bool IsGroup => Group is not null;

    internal static DataFilterQueryRule Condition(
        string field,
        FilterOperator @operator,
        DataFilterValue? value,
        DataFilterValue? upperValue = null)
        => new(field, @operator, value, upperValue, null);

    internal static DataFilterQueryRule Nested(DataFilterQueryGroup group)
        => new(null, FilterOperator.Equals, null, null, group);
}

/// <summary>
/// Immutable, versioned and safely serializable query for one item type.
/// Field expressions and codecs remain in the corresponding DataFilterSchema.
/// </summary>
public sealed class DataFilterQuery<TItem>
{
    private const int MaximumJsonLength = 1_048_576;

    internal DataFilterQuery(int version, DataFilterQueryGroup root)
    {
        Version = version;
        Root = root;
    }

    /// <summary>Current serialized contract version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Serialized contract version.</summary>
    public int Version { get; }

    /// <summary>Root query group.</summary>
    public DataFilterQueryGroup Root { get; }

    /// <summary>Serializes this immutable query using the stable versioned contract.</summary>
    public string Serialize()
    {
        DataFilterQueryDocument document = new(Version, Root);
        return JsonSerializer.Serialize(
            document,
            DataFilterJsonSerializerContext.Default.DataFilterQueryDocument);
    }

    /// <summary>Deserializes and validates a query against an allow-listed schema.</summary>
    public static DataFilterQuery<TItem> Deserialize(
        string json,
        DataFilterSchema<TItem> schema)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(schema);
        if (json.Length > MaximumJsonLength)
            throw new InvalidOperationException("Serialized DataFilter query exceeds the 1 MiB safety limit.");

        DataFilterQueryDocument? document = JsonSerializer.Deserialize(
            json,
            DataFilterJsonSerializerContext.Default.DataFilterQueryDocument);
        if (document is null) throw new JsonException("The serialized DataFilter query is empty.");
        if (document.Version != CurrentVersion)
            throw new NotSupportedException(
                $"DataFilter query version {document.Version} is not supported; expected {CurrentVersion}.");

        DataFilterQuery<TItem> query = new(document.Version, document.Root);
        query.Validate(schema);
        return query;
    }

    /// <summary>Attempts to deserialize and validate a query without leaking expected parse failures.</summary>
    public static bool TryDeserialize(
        string? json,
        DataFilterSchema<TItem> schema,
        out DataFilterQuery<TItem>? query,
        out string? error)
    {
        query = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "The serialized DataFilter query is empty.";
            return false;
        }

        try
        {
            query = Deserialize(json, schema);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or FormatException or
                                           InvalidOperationException or NotSupportedException or
                                           ArgumentException or OverflowException)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>Builds an in-memory predicate with the schema's compiled typed accessors.</summary>
    public Func<TItem, bool> Compile(DataFilterSchema<TItem> schema)
    {
        Validate(schema);
        return CompileGroup(Root, schema);
    }

    /// <summary>
    /// Builds an IQueryable-friendly expression. String comparison semantics follow
    /// the target LINQ provider/collation instead of forcing a client-side comparer.
    /// </summary>
    public Expression<Func<TItem, bool>> ToExpression(DataFilterSchema<TItem> schema)
    {
        Validate(schema);
        return DataFilterExpressionBuilder<TItem>.Build(Root, schema);
    }

    /// <summary>Filters an in-memory sequence without materializing it.</summary>
    public IEnumerable<TItem> Apply(IEnumerable<TItem> source, DataFilterSchema<TItem> schema)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(Compile(schema));
    }

    /// <summary>Applies this query to an IQueryable provider.</summary>
    public IQueryable<TItem> Apply(IQueryable<TItem> source, DataFilterSchema<TItem> schema)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(ToExpression(schema));
    }

    /// <summary>Validates field ids, operators, values, size and depth.</summary>
    public void Validate(DataFilterSchema<TItem> schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        int count = 0;
        ValidateGroup(Root, schema, depth: 0, ref count);
    }

    private static void ValidateGroup(
        DataFilterQueryGroup group,
        DataFilterSchema<TItem> schema,
        int depth,
        ref int count)
    {
        if (depth > schema.MaximumDepth)
            throw new InvalidOperationException($"DataFilter query exceeds maximum depth {schema.MaximumDepth}.");

        foreach (DataFilterQueryRule rule in group.Rules)
        {
            if (++count > schema.MaximumRules)
                throw new InvalidOperationException($"DataFilter query exceeds maximum rule count {schema.MaximumRules}.");

            if (rule.Group is not null)
            {
                if (rule.Field is not null || rule.Value is not null || rule.UpperValue is not null)
                    throw new InvalidOperationException("A DataFilter group cannot also contain condition data.");
                ValidateGroup(rule.Group, schema, depth + 1, ref count);
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.Field))
                throw new InvalidOperationException("A DataFilter condition requires a field id.");
            if (!Enum.IsDefined(rule.Operator))
                throw new InvalidOperationException($"DataFilter operator '{rule.Operator}' is invalid.");

            DataFilterField<TItem> field = schema.GetField(rule.Field);
            if (!field.Operators.Contains(rule.Operator))
                throw new InvalidOperationException(
                    $"Operator '{rule.Operator}' is not allowed for DataFilter field '{field.Id}'.");

            bool needsValue = RequiresValue(rule.Operator);
            if (needsValue && rule.Value is null)
                throw new InvalidOperationException(
                    $"Operator '{rule.Operator}' requires a value for DataFilter field '{field.Id}'.");
            if (!needsValue && (rule.Value is not null || rule.UpperValue is not null))
                throw new InvalidOperationException($"Operator '{rule.Operator}' does not accept values.");
            if (rule.Operator is FilterOperator.Between or FilterOperator.NotBetween)
            {
                if (rule.UpperValue is null)
                    throw new InvalidOperationException($"Operator '{rule.Operator}' requires two values.");
            }
            else if (rule.UpperValue is not null)
            {
                throw new InvalidOperationException($"Operator '{rule.Operator}' does not accept an upper value.");
            }

            if (rule.Value is not null)
            {
                if (rule.Value.Kind == DataFilterValueKind.Null && !field.AllowsNull)
                    throw new InvalidOperationException($"DataFilter field '{field.Id}' does not accept null.");
                _ = field.DeserializeValue(rule.Value);
            }
            if (rule.UpperValue is not null)
            {
                if (rule.UpperValue.Kind == DataFilterValueKind.Null && !field.AllowsNull)
                    throw new InvalidOperationException($"DataFilter field '{field.Id}' does not accept null.");
                _ = field.DeserializeValue(rule.UpperValue);
            }
        }
    }

    private static Func<TItem, bool> CompileGroup(
        DataFilterQueryGroup group,
        DataFilterSchema<TItem> schema)
    {
        if (group.Rules.Count == 0) return static _ => true;
        Func<TItem, bool>[] predicates = new Func<TItem, bool>[group.Rules.Count];
        for (int index = 0; index < predicates.Length; index++)
        {
            DataFilterQueryRule rule = group.Rules[index];
            predicates[index] = rule.Group is not null
                ? CompileGroup(rule.Group, schema)
                : CompileCondition(rule, schema);
        }

        return group.Logic == FilterLogic.And
            ? item => All(predicates, item)
            : item => Any(predicates, item);
    }

    private static Func<TItem, bool> CompileCondition(
        DataFilterQueryRule rule,
        DataFilterSchema<TItem> schema)
    {
        DataFilterField<TItem> field = schema.GetField(rule.Field!);
        object? value = rule.Value is null ? null : field.DeserializeValue(rule.Value);
        object? upper = rule.UpperValue is null ? null : field.DeserializeValue(rule.UpperValue);
        return item => DataFilterEvaluator.Matches(field.Accessor(item), rule.Operator, value, upper);
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

    internal static bool RequiresValue(FilterOperator op)
        => op is not (FilterOperator.IsEmpty or FilterOperator.IsNotEmpty);
}

/// <summary>Fluent builder for one AND/OR query group.</summary>
public sealed class DataFilterGroupBuilder<TItem>
{
    private readonly DataFilterSchema<TItem> _schema;
    private readonly List<DataFilterQueryRule> _rules = [];
    private readonly FilterLogic _logic;
    private bool _built;

    internal DataFilterGroupBuilder(DataFilterSchema<TItem> schema, FilterLogic logic)
    {
        _schema = schema;
        _logic = logic;
    }

    /// <summary>Adds a strongly typed condition with one value.</summary>
    public DataFilterGroupBuilder<TItem> Condition<TValue>(
        Expression<Func<TItem, TValue>> field,
        FilterOperator @operator,
        TValue value)
    {
        EnsureMutable();
        if (!DataFilterQuery<TItem>.RequiresValue(@operator))
            throw new ArgumentException($"Operator '{@operator}' does not accept a value.", nameof(@operator));
        if (@operator is FilterOperator.Between or FilterOperator.NotBetween)
            throw new ArgumentException($"Use Between for operator '{@operator}'.", nameof(@operator));

        DataFilterField<TItem> definition = ResolveField(field, @operator);
        _rules.Add(DataFilterQueryRule.Condition(
            definition.Id,
            @operator,
            definition.SerializeValue(value)));
        return this;
    }

    /// <summary>Adds IsEmpty or IsNotEmpty without a value.</summary>
    public DataFilterGroupBuilder<TItem> Condition<TValue>(
        Expression<Func<TItem, TValue>> field,
        FilterOperator @operator)
    {
        EnsureMutable();
        if (DataFilterQuery<TItem>.RequiresValue(@operator))
            throw new ArgumentException($"Operator '{@operator}' requires a value.", nameof(@operator));
        DataFilterField<TItem> definition = ResolveField(field, @operator);
        _rules.Add(DataFilterQueryRule.Condition(definition.Id, @operator, null));
        return this;
    }

    /// <summary>Adds an inclusive Between or exclusive NotBetween condition.</summary>
    public DataFilterGroupBuilder<TItem> Between<TValue>(
        Expression<Func<TItem, TValue>> field,
        TValue lower,
        TValue upper,
        bool negate = false)
    {
        EnsureMutable();
        FilterOperator @operator = negate ? FilterOperator.NotBetween : FilterOperator.Between;
        DataFilterField<TItem> definition = ResolveField(field, @operator);
        _rules.Add(DataFilterQueryRule.Condition(
            definition.Id,
            @operator,
            definition.SerializeValue(lower),
            definition.SerializeValue(upper)));
        return this;
    }

    /// <summary>Adds a nested group.</summary>
    public DataFilterGroupBuilder<TItem> Group(
        FilterLogic logic,
        Action<DataFilterGroupBuilder<TItem>> configure)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(configure);
        DataFilterGroupBuilder<TItem> child = new(_schema, logic);
        configure(child);
        _rules.Add(DataFilterQueryRule.Nested(child.BuildGroup()));
        return this;
    }

    internal DataFilterQuery<TItem> BuildQuery()
    {
        DataFilterQuery<TItem> query = new(DataFilterQuery<TItem>.CurrentVersion, BuildGroup());
        query.Validate(_schema);
        return query;
    }

    internal DataFilterQueryGroup BuildGroup()
    {
        EnsureMutable();
        _built = true;
        return new DataFilterQueryGroup(_logic, _rules);
    }

    private DataFilterField<TItem> ResolveField<TValue>(
        Expression<Func<TItem, TValue>> field,
        FilterOperator @operator)
    {
        DataFilterField<TItem> definition = _schema.GetField(field);
        if (definition.ValueType != typeof(TValue))
            throw new InvalidOperationException(
                $"DataFilter field '{definition.Id}' was declared with value type '{definition.ValueType}'.");
        if (!definition.Operators.Contains(@operator))
            throw new InvalidOperationException(
                $"Operator '{@operator}' is not allowed for DataFilter field '{definition.Id}'.");
        return definition;
    }

    private void EnsureMutable()
    {
        if (_built)
            throw new InvalidOperationException("This DataFilterGroupBuilder has already built an immutable group.");
    }
}

internal sealed class DataFilterQueryDocument
{
    [JsonConstructor]
    public DataFilterQueryDocument(int version, DataFilterQueryGroup root)
    {
        Version = version;
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public int Version { get; }
    public DataFilterQueryGroup Root { get; }
}

internal static class DataFilterExpressionBuilder<TItem>
{
    private static readonly MethodInfo StringContains = GetStringMethod(nameof(string.Contains));
    private static readonly MethodInfo StringStartsWith = GetStringMethod(nameof(string.StartsWith));
    private static readonly MethodInfo StringEndsWith = GetStringMethod(nameof(string.EndsWith));

    internal static Expression<Func<TItem, bool>> Build(
        DataFilterQueryGroup root,
        DataFilterSchema<TItem> schema)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TItem), "item");
        Expression body = BuildGroup(root, schema, parameter);
        return Expression.Lambda<Func<TItem, bool>>(body, parameter);
    }

    private static Expression BuildGroup(
        DataFilterQueryGroup group,
        DataFilterSchema<TItem> schema,
        ParameterExpression parameter)
    {
        Expression? aggregate = null;
        foreach (DataFilterQueryRule rule in group.Rules)
        {
            Expression current = rule.Group is not null
                ? BuildGroup(rule.Group, schema, parameter)
                : BuildCondition(rule, schema, parameter);
            aggregate = aggregate is null
                ? current
                : group.Logic == FilterLogic.And
                    ? Expression.AndAlso(aggregate, current)
                    : Expression.OrElse(aggregate, current);
        }
        return aggregate ?? Expression.Constant(true);
    }

    private static Expression BuildCondition(
        DataFilterQueryRule rule,
        DataFilterSchema<TItem> schema,
        ParameterExpression parameter)
    {
        DataFilterField<TItem> field = schema.GetField(rule.Field!);
        Expression access = new ParameterReplaceVisitor(
            field.SelectorExpression.Parameters[0],
            parameter).Visit(field.SelectorExpression.Body)!;
        object? value = rule.Value is null ? null : field.DeserializeValue(rule.Value);
        object? upper = rule.UpperValue is null ? null : field.DeserializeValue(rule.UpperValue);

        if (rule.Operator is FilterOperator.IsEmpty or FilterOperator.IsNotEmpty)
        {
            Expression empty = IsEmpty(access);
            return rule.Operator == FilterOperator.IsEmpty ? empty : Expression.Not(empty);
        }

        if (rule.Operator is FilterOperator.Contains or FilterOperator.NotContains or
            FilterOperator.StartsWith or FilterOperator.EndsWith)
            return BuildText(access, rule.Operator, value?.ToString() ?? string.Empty);

        if (rule.Operator is FilterOperator.Between or FilterOperator.NotBetween)
        {
            Expression range = Expression.AndAlso(
                Expression.GreaterThanOrEqual(access, TypedConstant(access.Type, value)),
                Expression.LessThanOrEqual(access, TypedConstant(access.Type, upper)));
            return rule.Operator == FilterOperator.Between ? range : Expression.Not(range);
        }

        Expression right = TypedConstant(access.Type, value);
        return rule.Operator switch
        {
            FilterOperator.Equals => Expression.Equal(access, right),
            FilterOperator.NotEquals => Expression.NotEqual(access, right),
            FilterOperator.GreaterThan => Expression.GreaterThan(access, right),
            FilterOperator.GreaterOrEqual => Expression.GreaterThanOrEqual(access, right),
            FilterOperator.LessThan => Expression.LessThan(access, right),
            FilterOperator.LessOrEqual => Expression.LessThanOrEqual(access, right),
            _ => throw new NotSupportedException($"DataFilter operator '{rule.Operator}' cannot be translated.")
        };
    }

    private static Expression BuildText(Expression access, FilterOperator op, string value)
    {
        if (access.Type != typeof(string))
            throw new InvalidOperationException($"Text operator '{op}' requires a string field.");
        Expression notNull = Expression.NotEqual(access, Expression.Constant(null, typeof(string)));
        MethodInfo method = op switch
        {
            FilterOperator.Contains or FilterOperator.NotContains => StringContains,
            FilterOperator.StartsWith => StringStartsWith,
            FilterOperator.EndsWith => StringEndsWith,
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };
        Expression call = Expression.Call(access, method, Expression.Constant(value));
        return op == FilterOperator.NotContains
            ? Expression.OrElse(Expression.Not(notNull), Expression.Not(call))
            : Expression.AndAlso(notNull, call);
    }

    private static MethodInfo GetStringMethod(string name)
        => typeof(string).GetMethod(name, BindingFlags.Instance | BindingFlags.Public, [typeof(string)])
            ?? throw new MissingMethodException(typeof(string).FullName, name);

    private static Expression IsEmpty(Expression access)
    {
        if (access.Type == typeof(string))
        {
            return Expression.OrElse(
                Expression.Equal(access, Expression.Constant(null, typeof(string))),
                Expression.Equal(access, Expression.Constant(string.Empty)));
        }

        if (!access.Type.IsValueType || Nullable.GetUnderlyingType(access.Type) is not null)
            return Expression.Equal(access, Expression.Constant(null, access.Type));
        return Expression.Constant(false);
    }

    private static Expression TypedConstant(Type destinationType, object? value)
    {
        if (value is null)
        {
            if (destinationType.IsValueType && Nullable.GetUnderlyingType(destinationType) is null)
                throw new InvalidOperationException($"A null filter value is invalid for '{destinationType}'.");
            return Expression.Constant(null, destinationType);
        }

        Expression constant = Expression.Constant(value, value.GetType());
        return value.GetType() == destinationType
            ? constant
            : Expression.Convert(constant, destinationType);
    }

    private sealed class ParameterReplaceVisitor(ParameterExpression source, Expression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => ReferenceEquals(node, source) ? target : base.VisitParameter(node);
    }
}
