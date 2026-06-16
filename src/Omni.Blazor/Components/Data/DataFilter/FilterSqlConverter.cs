using System.Globalization;
using System.Text;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Converts an <c>OmniDataFilter</c> rule tree to and from a generic SQL
/// <c>WHERE</c> expression (no leading <c>WHERE</c> keyword). It is pure and
/// UI-free so it can be unit-tested and reused programmatically.
///
/// Dialect is intentionally generic/ANSI-ish: <c>=</c>, <c>&lt;&gt;</c>,
/// <c>&lt; &gt; &lt;= &gt;=</c>, <c>LIKE</c>/<c>NOT LIKE</c>, <c>IS [NOT] NULL</c>,
/// <c>AND</c>/<c>OR</c>, parentheses, <c>'strings'</c>, numbers and <c>TRUE</c>/<c>FALSE</c>.
///
/// Round-trips cleanly for the operators the filter actually evaluates. Known
/// limitations (documented): <c>Between</c>/<c>NotBetween</c> are not emitted/parsed;
/// <c>IsEmpty</c>/<c>IsNotEmpty</c> map to <c>IS NULL</c>/<c>IS NOT NULL</c> (the
/// empty-string nuance of the in-memory eval is not represented); LIKE wildcards
/// (<c>%</c>/<c>_</c>) inside a value are matched LITERALLY — the in-memory filter has no
/// pattern matching, so <c>LIKE '%a%b%'</c> means the literal substring "a%b", not a wildcard.
/// </summary>
public static class FilterSqlConverter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    // Symmetric number style for emit + parse: sign and decimal point only (no
    // thousands separators, parentheses-negatives or exponents) so a literal can't
    // silently change shape across the round-trip.
    private const NumberStyles NumStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
    // The exact date shapes FormatValue emits — parsing accepts only these.
    private static readonly string[] DateFormats = { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };

    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    { "AND", "OR", "NOT", "LIKE", "IS", "NULL", "TRUE", "FALSE", "BETWEEN" };

    // ─── Rules → SQL ─────────────────────────────────────────────────────────

    /// <summary>Render a rule tree to a SQL WHERE expression (empty string when there are no usable rules).</summary>
    public static string ToSql(IEnumerable<OmniFilterRule>? rules, FilterLogic logic,
                               IReadOnlyList<OmniFilterPropertyInfo>? properties = null)
        => RenderGroup(rules?.ToList() ?? new(), logic, properties);

    private static string RenderGroup(List<OmniFilterRule> rules, FilterLogic logic, IReadOnlyList<OmniFilterPropertyInfo>? props)
    {
        var parts = new List<string>();
        foreach (var r in rules)
        {
            var s = RenderRule(r, props);
            if (!string.IsNullOrEmpty(s)) parts.Add(s);
        }
        if (parts.Count == 0) return "";
        return string.Join(logic == FilterLogic.And ? " AND " : " OR ", parts);
    }

    private static string RenderRule(OmniFilterRule r, IReadOnlyList<OmniFilterPropertyInfo>? props)
    {
        if (r.IsGroup)
        {
            var inner = RenderGroup(r.Rules ?? new(), r.Logic, props);
            return inner.Length == 0 ? "" : $"({inner})";
        }
        if (string.IsNullOrEmpty(r.Property)) return "";

        var col = RenderIdentifier(r.Property!);
        var type = FindType(props, r.Property);

        switch (r.Operator)
        {
            case FilterOperator.IsEmpty: return $"{col} IS NULL";
            case FilterOperator.IsNotEmpty: return $"{col} IS NOT NULL";
        }

        // value-bearing operators: skip incomplete conditions (no value yet)
        if (r.Value is null || (r.Value is string es && es.Length == 0)) return "";
        var like = LikeCore(r.Value);

        return r.Operator switch
        {
            FilterOperator.Contains => $"{col} LIKE {Quote("%" + like + "%")}",
            FilterOperator.NotContains => $"{col} NOT LIKE {Quote("%" + like + "%")}",
            FilterOperator.StartsWith => $"{col} LIKE {Quote(like + "%")}",
            FilterOperator.EndsWith => $"{col} LIKE {Quote("%" + like)}",
            FilterOperator.Equals => $"{col} = {FormatValue(r.Value, type)}",
            FilterOperator.NotEquals => $"{col} <> {FormatValue(r.Value, type)}",
            FilterOperator.GreaterThan => $"{col} > {FormatValue(r.Value, type)}",
            FilterOperator.GreaterOrEqual => $"{col} >= {FormatValue(r.Value, type)}",
            FilterOperator.LessThan => $"{col} < {FormatValue(r.Value, type)}",
            FilterOperator.LessOrEqual => $"{col} <= {FormatValue(r.Value, type)}",
            _ => "" // Between / NotBetween: not supported
        };
    }

    private static string LikeCore(object? v) => v?.ToString() ?? "";

    // The parser is decimal-centric and has no exponent support, so emit doubles/floats
    // in non-scientific form (via decimal). Magnitudes decimal can't hold collapse to NULL.
    private static string FormatDouble(double d)
    {
        if (!double.IsFinite(d)) return "NULL";
        try { return ((decimal)d).ToString(Inv); }
        catch (OverflowException) { return "NULL"; }
    }

    private static string Quote(string s) => "'" + s.Replace("'", "''") + "'";

    private static string RenderIdentifier(string name)
    {
        var simple = name.Length > 0 && (char.IsLetter(name[0]) || name[0] == '_')
                     && name.All(c => char.IsLetterOrDigit(c) || c == '_')
                     && !Keywords.Contains(name);
        return simple ? name : "[" + name.Replace("]", "]]") + "]";
    }

    private static ColumnFilterType? FindType(IReadOnlyList<OmniFilterPropertyInfo>? props, string? name)
        => props?.FirstOrDefault(p => string.Equals(p.Property, name, StringComparison.OrdinalIgnoreCase))?.Type;

    private static string FormatValue(object? v, ColumnFilterType? type)
    {
        switch (v)
        {
            case null: return "NULL";
            case bool b: return b ? "TRUE" : "FALSE";
            case DateTime dt: return Quote(dt.TimeOfDay == TimeSpan.Zero ? dt.ToString("yyyy-MM-dd", Inv) : dt.ToString("yyyy-MM-dd HH:mm:ss", Inv));
            case DateOnly d: return Quote(d.ToString("yyyy-MM-dd", Inv));
            case decimal dec: return dec.ToString(Inv);
            case double dbl: return FormatDouble(dbl);
            case float fl: return FormatDouble(fl);
            case int or long or short or byte or sbyte or uint or ulong or ushort: return Convert.ToString(v, Inv)!;
        }

        var s = v.ToString() ?? "";
        // Boolean column: normalize "1"/"0"/"true"/"false" to a SQL boolean literal.
        if (type == ColumnFilterType.Boolean)
        {
            if (bool.TryParse(s, out var pb)) return pb ? "TRUE" : "FALSE";
            if (s is "1") return "TRUE";
            if (s is "0") return "FALSE";
        }
        // Number column: emit a canonical numeric literal, or quote if it isn't one.
        if (type == ColumnFilterType.Number && decimal.TryParse(s, NumStyle, Inv, out var dn)) return dn.ToString(Inv);
        return Quote(s);
    }

    // ─── SQL → Rules ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parse a SQL WHERE expression into a rule tree. Returns false (with a localized
    /// <paramref name="error"/>) when the text isn't representable by the filter.
    /// An empty/whitespace string is valid and yields an empty filter.
    /// </summary>
    public static bool TryParse(string? sql, IReadOnlyList<OmniFilterPropertyInfo> properties,
                                out List<OmniFilterRule> rules, out FilterLogic logic, out string? error)
    {
        rules = new();
        logic = FilterLogic.And;
        error = null;

        var text = sql?.Trim() ?? "";
        if (text.Length == 0) return true;

        try
        {
            var root = new Parser(text, properties).ParseRoot();
            if (root is null) return true;
            if (root.IsGroup) { rules = root.Rules!; logic = root.Logic; }
            else { rules = new() { root }; }
            return true;
        }
        catch (FilterSqlParseException ex)
        {
            error = ex.Message;
            rules = new();
            logic = FilterLogic.And;
            return false;
        }
    }

    // ─── Parser ──────────────────────────────────────────────────────────────

    private enum K { Ident, Str, Num, Eq, Ne, Lt, Gt, Le, Ge, Minus, LParen, RParen, And, Or, Not, Like, Is, Null, True, False, Between, End }

    private readonly record struct Token(K Kind, string Text, object? Value);

    private sealed class Parser
    {
        private readonly List<Token> _toks;
        private readonly Dictionary<string, OmniFilterPropertyInfo> _props;
        private int _i;

        public Parser(string sql, IReadOnlyList<OmniFilterPropertyInfo> properties)
        {
            _props = new(StringComparer.OrdinalIgnoreCase);
            foreach (var p in properties) _props[p.Property] = p;
            _toks = Tokenize(sql);
        }

        private Token Peek => _toks[_i];
        private Token Next() => _toks[_i++];
        private bool Is(K k) => _toks[_i].Kind == k;

        private Token Expect(K k, string what)
        {
            if (!Is(k)) throw Err($"esperado {what}");
            return Next();
        }

        public OmniFilterRule? ParseRoot()
        {
            if (Is(K.End)) return null;
            var node = ParseOr();
            if (!Is(K.End)) throw Err($"token inesperado “{Peek.Text}”");
            return node;
        }

        private OmniFilterRule ParseOr()
        {
            var nodes = new List<OmniFilterRule> { ParseAnd() };
            while (Is(K.Or)) { Next(); nodes.Add(ParseAnd()); }
            return nodes.Count == 1 ? nodes[0] : new OmniFilterRule { Logic = FilterLogic.Or, Rules = nodes };
        }

        private OmniFilterRule ParseAnd()
        {
            var nodes = new List<OmniFilterRule> { ParsePrimary() };
            while (Is(K.And)) { Next(); nodes.Add(ParsePrimary()); }
            return nodes.Count == 1 ? nodes[0] : new OmniFilterRule { Logic = FilterLogic.And, Rules = nodes };
        }

        private OmniFilterRule ParsePrimary()
        {
            if (Is(K.LParen))
            {
                Next();
                var inner = ParseOr();
                Expect(K.RParen, "“)”");
                // Always wrap a parenthesised sub-expression in a group so the tree
                // mirrors what the user typed (and round-trips).
                return inner.IsGroup ? inner : new OmniFilterRule { Logic = FilterLogic.And, Rules = new() { inner } };
            }
            if (Is(K.Not))
                throw Err("NOT só é suportado na forma infixa (campo NOT LIKE '%x%', <>, IS NOT NULL)");
            return ParseCondition();
        }

        private OmniFilterRule ParseCondition()
        {
            if (!Is(K.Ident)) throw Err($"esperado um campo, encontrado “{Peek.Text}”");
            var idTok = Next();
            if (!_props.TryGetValue(idTok.Text, out var info))
                throw Err($"coluna “{idTok.Text}” desconhecida");

            var rule = new OmniFilterRule { Property = info.Property };

            // IS [NOT] NULL
            if (Is(K.Is))
            {
                Next();
                var not = Is(K.Not);
                if (not) Next();
                Expect(K.Null, "NULL");
                rule.Operator = not ? FilterOperator.IsNotEmpty : FilterOperator.IsEmpty;
                return rule;
            }

            // [NOT] LIKE 'pattern'
            var negatedLike = false;
            if (Is(K.Not)) { Next(); negatedLike = true; }
            if (Is(K.Like))
            {
                Next();
                var pat = Expect(K.Str, "um padrão entre aspas").Value as string ?? "";
                ApplyLike(rule, pat, negatedLike);
                return rule;
            }
            if (negatedLike) throw Err("esperado LIKE após NOT");

            if (Is(K.Between)) throw Err("operador BETWEEN ainda não é suportado");

            // comparison operator + value
            var op = Peek.Kind switch
            {
                K.Eq => FilterOperator.Equals,
                K.Ne => FilterOperator.NotEquals,
                K.Gt => FilterOperator.GreaterThan,
                K.Ge => FilterOperator.GreaterOrEqual,
                K.Lt => FilterOperator.LessThan,
                K.Le => FilterOperator.LessOrEqual,
                _ => throw Err($"esperado um operador após “{info.Property}”")
            };
            Next();
            rule.Operator = op;
            rule.Value = CoerceValue(ParseValue(), info, idTok.Text);
            return rule;
        }

        private static void ApplyLike(OmniFilterRule rule, string pattern, bool negated)
        {
            var lead = pattern.StartsWith('%');
            var core = lead ? pattern[1..] : pattern;
            var trail = core.EndsWith('%');
            if (trail) core = core[..^1];

            if (negated)
            {
                // Only %x% (NotContains) and a plain literal (NotEquals) have counterparts.
                if (lead && trail) { rule.Operator = FilterOperator.NotContains; rule.Value = core; return; }
                if (!lead && !trail) { rule.Operator = FilterOperator.NotEquals; rule.Value = core; return; }
                throw new FilterSqlParseException("NOT LIKE só suporta '%valor%' ou um valor exato");
            }

            if (lead && trail) rule.Operator = FilterOperator.Contains;
            else if (trail) rule.Operator = FilterOperator.StartsWith;
            else if (lead) rule.Operator = FilterOperator.EndsWith;
            else rule.Operator = FilterOperator.Equals;
            rule.Value = core;
        }

        // Returns the raw literal: decimal, string, bool, or null.
        private object? ParseValue()
        {
            if (Is(K.Minus))
            {
                Next();
                var n = Expect(K.Num, "um número").Value;
                return -(decimal)n!;
            }
            return Peek.Kind switch
            {
                K.Num => Next().Value,
                K.Str => Next().Value,
                K.True => Skip(true),
                K.False => Skip(false),
                K.Null => Skip((object?)null),
                _ => throw Err($"esperado um valor, encontrado “{Peek.Text}”")
            };
        }

        private object? Skip(object? v) { Next(); return v; }

        // Coerce the raw literal to the column's value type (matching the visual editors).
        private object? CoerceValue(object? raw, OmniFilterPropertyInfo info, string col)
        {
            if (raw is null) return null;
            switch (info.Type)
            {
                case ColumnFilterType.Number:
                    if (raw is decimal d) return d;
                    if (raw is string sn && decimal.TryParse(sn, NumStyle, Inv, out var pn)) return pn;
                    throw Err($"valor numérico esperado para “{col}”");

                case ColumnFilterType.Date:
                    if (raw is string sd && DateTime.TryParseExact(sd, DateFormats, Inv, DateTimeStyles.None, out var pd)) return pd;
                    throw Err($"data inválida para “{col}” (use 'AAAA-MM-DD')");

                case ColumnFilterType.Boolean:
                    if (raw is bool b) return b;
                    if (raw is decimal db) return db == 1 ? true : db == 0 ? false
                        : throw Err($"valor booleano esperado para “{col}” (TRUE/FALSE)");
                    if (raw is string sb && bool.TryParse(sb, out var pb)) return pb;
                    throw Err($"valor booleano esperado para “{col}” (TRUE/FALSE)");

                default: // Text / Select
                    return raw is decimal dx ? dx.ToString(Inv)
                         : raw is bool bx ? (bx ? "true" : "false")
                         : raw.ToString();
            }
        }

        private FilterSqlParseException Err(string msg) => new(msg);

        // ── tokenizer ──
        private static List<Token> Tokenize(string s)
        {
            var toks = new List<Token>();
            int i = 0, n = s.Length;
            while (i < n)
            {
                var c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }

                switch (c)
                {
                    case '(': toks.Add(new(K.LParen, "(", null)); i++; continue;
                    case ')': toks.Add(new(K.RParen, ")", null)); i++; continue;
                    case '=': toks.Add(new(K.Eq, "=", null)); i++; continue;
                    case '-': toks.Add(new(K.Minus, "-", null)); i++; continue;
                    case '<':
                        if (i + 1 < n && s[i + 1] == '>') { toks.Add(new(K.Ne, "<>", null)); i += 2; }
                        else if (i + 1 < n && s[i + 1] == '=') { toks.Add(new(K.Le, "<=", null)); i += 2; }
                        else { toks.Add(new(K.Lt, "<", null)); i++; }
                        continue;
                    case '>':
                        if (i + 1 < n && s[i + 1] == '=') { toks.Add(new(K.Ge, ">=", null)); i += 2; }
                        else { toks.Add(new(K.Gt, ">", null)); i++; }
                        continue;
                    case '!':
                        if (i + 1 < n && s[i + 1] == '=') { toks.Add(new(K.Ne, "!=", null)); i += 2; continue; }
                        throw new FilterSqlParseException("caractere inesperado “!”");
                }

                // quoted string '...'  ('' is an escaped quote)
                if (c == '\'')
                {
                    var sb = new StringBuilder();
                    i++;
                    while (i < n)
                    {
                        if (s[i] == '\'')
                        {
                            if (i + 1 < n && s[i + 1] == '\'') { sb.Append('\''); i += 2; continue; }
                            i++; goto strDone;
                        }
                        sb.Append(s[i++]);
                    }
                    throw new FilterSqlParseException("string não terminada (faltou aspas de fechamento)");
                strDone:
                    toks.Add(new(K.Str, sb.ToString(), sb.ToString()));
                    continue;
                }

                // bracketed / double-quoted identifier (close char doubled = literal)
                if (c == '[' || c == '"')
                {
                    var close = c == '[' ? ']' : '"';
                    var sb = new StringBuilder();
                    i++;
                    var closed = false;
                    while (i < n)
                    {
                        if (s[i] == close)
                        {
                            if (i + 1 < n && s[i + 1] == close) { sb.Append(close); i += 2; continue; }
                            i++; closed = true; break;
                        }
                        sb.Append(s[i++]);
                    }
                    if (!closed) throw new FilterSqlParseException("identificador não terminado");
                    toks.Add(new(K.Ident, sb.ToString(), null));
                    continue;
                }

                // number
                if (char.IsDigit(c))
                {
                    int start = i;
                    while (i < n && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    var raw = s[start..i];
                    if (!decimal.TryParse(raw, NumberStyles.Any, Inv, out var dv))
                        throw new FilterSqlParseException($"número inválido “{raw}”");
                    toks.Add(new(K.Num, raw, dv));
                    continue;
                }

                // identifier / keyword
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < n && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    var word = s[start..i];
                    var kind = word.ToUpperInvariant() switch
                    {
                        "AND" => K.And,
                        "OR" => K.Or,
                        "NOT" => K.Not,
                        "LIKE" => K.Like,
                        "IS" => K.Is,
                        "NULL" => K.Null,
                        "TRUE" => K.True,
                        "FALSE" => K.False,
                        "BETWEEN" => K.Between,
                        _ => K.Ident
                    };
                    toks.Add(new(kind, word, null));
                    continue;
                }

                throw new FilterSqlParseException($"caractere inesperado “{c}”");
            }
            toks.Add(new(K.End, "", null));
            return toks;
        }
    }
}

/// <summary>Raised by <see cref="FilterSqlConverter.TryParse"/> internals; surfaced as the out error.</summary>
internal sealed class FilterSqlParseException : Exception
{
    public FilterSqlParseException(string message) : base(message) { }
}
