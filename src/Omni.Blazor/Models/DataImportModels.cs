using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

/// <summary>Allocation-free custom parser contract for one typed import value.</summary>
public delegate bool DataImportTryParser<TValue>(
    ReadOnlySpan<char> text,
    IFormatProvider formatProvider,
    out TValue? value);

/// <summary>Cancellable destination invoked with the accepted typed import snapshot.</summary>
public delegate ValueTask DataImportHandler<TItem>(
    IReadOnlyList<TItem> items,
    CancellationToken cancellationToken)
    where TItem : class;

/// <summary>One target-property to source-header mapping.</summary>
public sealed record DataImportMapping(string Property, string Header, int SourceIndex);

/// <summary>One structured conversion or validation error.</summary>
public sealed record DataImportError(int RowNumber, string? Property, string Message);

/// <summary>Processed typed preview row.</summary>
public sealed record DataImportRow<TItem>(
    int RowNumber,
    IReadOnlyList<string> Values,
    TItem? Item,
    IReadOnlyList<DataImportError> Errors)
    where TItem : class
{
    /// <summary>Whether conversion and validation succeeded.</summary>
    public bool IsValid => Errors.Count == 0 && Item is not null;
}

/// <summary>Successful import event with accepted items and the complete bounded preview.</summary>
public sealed record DataImportCompletedEventArgs<TItem>(
    IReadOnlyList<TItem> Items,
    IReadOnlyList<DataImportRow<TItem>> Rows,
    int RejectedCount)
    where TItem : class;

/// <summary>Immutable typed tabular import schema.</summary>
public sealed class DataImportSchema<TItem> where TItem : class
{
    internal DataImportSchema(
        Func<TItem> factory,
        IReadOnlyList<IDataImportColumn<TItem>> columns,
        char delimiter,
        bool hasHeader)
    {
        Factory = factory;
        Columns = columns;
        Delimiter = delimiter;
        HasHeader = hasHeader;
    }

    internal Func<TItem> Factory { get; }
    internal IReadOnlyList<IDataImportColumn<TItem>> Columns { get; }

    /// <summary>Default delimited-text separator. Default comma.</summary>
    public char Delimiter { get; }

    /// <summary>Whether the first row contains source headers. Default true.</summary>
    public bool HasHeader { get; }

    /// <summary>Number of typed target columns.</summary>
    public int Count => Columns.Count;

    /// <summary>Creates and builds an immutable import schema.</summary>
    public static DataImportSchema<TItem> Create(Action<DataImportSchemaBuilder<TItem>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        DataImportSchemaBuilder<TItem> builder = new();
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates a mutable import schema builder.</summary>
    public static DataImportSchemaBuilder<TItem> Builder() => new();
}

/// <summary>Strongly typed builder for bounded delimited-text imports.</summary>
public sealed class DataImportSchemaBuilder<TItem> where TItem : class
{
    private readonly List<IDataImportColumn<TItem>> _columns = [];
    private readonly HashSet<string> _properties = new(StringComparer.Ordinal);
    private Func<TItem>? _factory;
    private char _delimiter = ',';
    private bool _hasHeader = true;
    private bool _built;

    /// <summary>Sets the item factory used only after one source row is read.</summary>
    public DataImportSchemaBuilder<TItem> Factory(Func<TItem> factory)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        return this;
    }

    /// <summary>Sets the default delimiter.</summary>
    public DataImportSchemaBuilder<TItem> Delimiter(char delimiter)
    {
        EnsureMutable();
        if (delimiter is '\r' or '\n' or '"')
            throw new ArgumentOutOfRangeException(nameof(delimiter));
        _delimiter = delimiter;
        return this;
    }

    /// <summary>Controls whether the first row is interpreted as a header. Default true.</summary>
    public DataImportSchemaBuilder<TItem> HasHeader(bool value = true)
    {
        EnsureMutable();
        _hasHeader = value;
        return this;
    }

    /// <summary>Adds one rename-safe writable target property.</summary>
    public DataImportSchemaBuilder<TItem> Column<TValue>(
        Expression<Func<TItem, TValue>> property,
        Action<DataImportColumnBuilder<TItem, TValue>>? configure = null)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(property);
        MemberExpression member = UnwrapMember(property.Body)
            ?? throw new ArgumentException("DataImport columns must select a direct writable property.", nameof(property));
        if (member.Expression != property.Parameters[0] || member.Member is not PropertyInfo info || info.SetMethod is null)
            throw new ArgumentException("DataImport columns must select a direct writable property.", nameof(property));
        if (info.PropertyType != typeof(TValue))
            throw new ArgumentException("DataImport columns cannot convert the selected property to another type.", nameof(property));
        if (!_properties.Add(info.Name))
            throw new InvalidOperationException($"DataImport property '{info.Name}' was declared more than once.");
        DataImportColumnBuilder<TItem, TValue> builder = new(info, property, EnsureMutable);
        configure?.Invoke(builder);
        _columns.Add(builder.Build());
        return this;
    }

    /// <summary>Builds the immutable import schema.</summary>
    public DataImportSchema<TItem> Build()
    {
        EnsureMutable();
        _built = true;
        if (_columns.Count == 0)
            throw new InvalidOperationException("DataImport requires at least one target column.");
        Func<TItem> factory = _factory ?? throw new InvalidOperationException(
            "DataImport requires an explicit Factory. This makes object creation deterministic and Native AOT safe.");
        return new DataImportSchema<TItem>(
            factory,
            Array.AsReadOnly(_columns.ToArray()),
            _delimiter,
            _hasHeader);
    }

    private static MemberExpression? UnwrapMember(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand as MemberExpression
            : expression as MemberExpression;

    private void EnsureMutable()
    {
        if (_built) throw new InvalidOperationException("DataImport schema is immutable after Build().");
    }
}

/// <summary>Builder for one typed import target column.</summary>
public sealed class DataImportColumnBuilder<TItem, TValue> where TItem : class
{
    private readonly Action _ensureMutable;
    private readonly PropertyInfo _property;
    private readonly Expression<Func<TItem, TValue>> _expression;
    private string _header;
    private readonly List<string> _aliases = [];
    private bool _required;
    private string? _requiredError;
    private DataImportTryParser<TValue>? _parser;
    private Func<TValue?, string?>? _validator;

    internal DataImportColumnBuilder(
        PropertyInfo property,
        Expression<Func<TItem, TValue>> expression,
        Action ensureMutable)
    {
        _property = property;
        _expression = expression;
        _header = property.Name;
        _ensureMutable = ensureMutable;
    }

    /// <summary>Sets the preferred source header.</summary>
    public DataImportColumnBuilder<TItem, TValue> Header(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Set(ref _header, value);
    }

    /// <summary>Adds alternative source headers used by automatic mapping.</summary>
    public DataImportColumnBuilder<TItem, TValue> Aliases(params string[] values)
    {
        _ensureMutable();
        ArgumentNullException.ThrowIfNull(values);
        foreach (string value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!_aliases.Contains(value, StringComparer.OrdinalIgnoreCase)) _aliases.Add(value);
        }
        return this;
    }

    /// <summary>Requires a mapped non-blank source value.</summary>
    public DataImportColumnBuilder<TItem, TValue> Required(string? error = null)
    {
        _ensureMutable();
        _required = true;
        _requiredError = error;
        return this;
    }

    /// <summary>Uses an allocation-free custom typed parser.</summary>
    public DataImportColumnBuilder<TItem, TValue> Parse(DataImportTryParser<TValue> parser)
    {
        ArgumentNullException.ThrowIfNull(parser);
        return Set(ref _parser, parser);
    }

    /// <summary>Adds typed value validation after parsing.</summary>
    public DataImportColumnBuilder<TItem, TValue> Validate(Func<TValue?, string?> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        return Set(ref _validator, validator);
    }

    internal IDataImportColumn<TItem> Build()
        => new DataImportColumn<TItem, TValue>(
            _property,
            _expression,
            _header,
            Array.AsReadOnly(_aliases.ToArray()),
            _required,
            _requiredError,
            _parser,
            _validator);

    private DataImportColumnBuilder<TItem, TValue> Set<T>(ref T target, T value)
    {
        _ensureMutable();
        target = value;
        return this;
    }
}

internal interface IDataImportColumn<TItem> where TItem : class
{
    string Property { get; }
    PropertyInfo PropertyInfo { get; }
    string Header { get; }
    IReadOnlyList<string> Aliases { get; }
    bool Required { get; }
    string? RequiredError { get; }
    bool TryAssign(
        TItem item,
        string text,
        IFormatProvider provider,
        string requiredFormat,
        string invalidFormat,
        out string? error);
}

internal sealed class DataImportColumn<TItem, TValue> : IDataImportColumn<TItem> where TItem : class
{
    private readonly Action<TItem, TValue?> _setter;
    private readonly DataImportTryParser<TValue>? _parser;
    private readonly Func<TValue?, string?>? _validator;

    public DataImportColumn(
        PropertyInfo property,
        Expression<Func<TItem, TValue>> expression,
        string header,
        IReadOnlyList<string> aliases,
        bool required,
        string? requiredError,
        DataImportTryParser<TValue>? parser,
        Func<TValue?, string?>? validator)
    {
        PropertyInfo = property;
        Property = property.Name;
        Header = header;
        Aliases = aliases;
        Required = required;
        RequiredError = requiredError;
        _parser = parser;
        _validator = validator;
        ParameterExpression model = expression.Parameters[0];
        ParameterExpression value = Expression.Parameter(typeof(TValue), "value");
        _setter = Expression.Lambda<Action<TItem, TValue?>>(
            Expression.Assign(expression.Body, value), model, value).Compile();
    }

    public string Property { get; }
    public PropertyInfo PropertyInfo { get; }
    public string Header { get; }
    public IReadOnlyList<string> Aliases { get; }
    public bool Required { get; }
    public string? RequiredError { get; }

    public bool TryAssign(
        TItem item,
        string text,
        IFormatProvider provider,
        string requiredFormat,
        string invalidFormat,
        out string? error)
    {
        ReadOnlySpan<char> span = text.AsSpan().Trim();
        if (span.IsEmpty && Required)
        {
            error = RequiredError ?? string.Format(provider, requiredFormat, Header);
            return false;
        }
        TValue? value;
        if (span.IsEmpty)
        {
            value = default;
        }
        else if (_parser is not null)
        {
            if (!_parser(span, provider, out value))
            {
                error = string.Format(provider, invalidFormat, Header);
                return false;
            }
        }
        else if (!DataImportConversions.TryConvert(span, provider, out value))
        {
            error = string.Format(provider, invalidFormat, Header);
            return false;
        }
        error = _validator?.Invoke(value);
        if (!string.IsNullOrWhiteSpace(error)) return false;
        _setter(item, value);
        return true;
    }
}

internal static class DataImportConversions
{
    public static bool TryConvert<TValue>(
        ReadOnlySpan<char> text,
        IFormatProvider provider,
        out TValue? value)
    {
        Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        object? converted;
        bool success;
        if (type == typeof(string)) { converted = text.ToString(); success = true; }
        else if (type == typeof(int)) { success = int.TryParse(text, NumberStyles.Integer, provider, out int parsed); converted = parsed; }
        else if (type == typeof(long)) { success = long.TryParse(text, NumberStyles.Integer, provider, out long parsed); converted = parsed; }
        else if (type == typeof(decimal)) { success = decimal.TryParse(text, NumberStyles.Number, provider, out decimal parsed); converted = parsed; }
        else if (type == typeof(double)) { success = double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out double parsed); converted = parsed; }
        else if (type == typeof(float)) { success = float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out float parsed); converted = parsed; }
        else if (type == typeof(bool)) { success = TryBoolean(text, out bool parsed); converted = parsed; }
        else if (type == typeof(Guid)) { success = Guid.TryParse(text, out Guid parsed); converted = parsed; }
        else if (type == typeof(DateTime)) { success = DateTime.TryParse(text, provider, DateTimeStyles.None, out DateTime parsed); converted = parsed; }
        else if (type == typeof(DateOnly)) { success = DateOnly.TryParse(text, provider, DateTimeStyles.None, out DateOnly parsed); converted = parsed; }
        else if (type == typeof(TimeOnly)) { success = TimeOnly.TryParse(text, provider, DateTimeStyles.None, out TimeOnly parsed); converted = parsed; }
        else if (type.IsEnum) { success = Enum.TryParse(type, text.ToString(), ignoreCase: true, out converted); }
        else { converted = null; success = false; }
        value = success ? (TValue?)converted : default;
        return success;
    }

    private static bool TryBoolean(ReadOnlySpan<char> text, out bool value)
    {
        if (bool.TryParse(text, out value)) return true;
        if (text.Equals("1", StringComparison.Ordinal)
            || text.Equals("sim", StringComparison.OrdinalIgnoreCase)
            || text.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }
        if (text.Equals("0", StringComparison.Ordinal)
            || text.Equals("não", StringComparison.OrdinalIgnoreCase)
            || text.Equals("nao", StringComparison.OrdinalIgnoreCase)
            || text.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }
        value = false;
        return false;
    }
}
