using System.Globalization;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// In-memory evaluation of a single <c>OmniDataFilter</c> condition. Coerces the
/// actual member value and the edited value to a common comparable form so the
/// operators behave sensibly across strings, numbers, dates, bools and enums.
/// </summary>
internal static class DataFilterEval
{
    public static bool Matches(object? actual, FilterOperator op, object? expected)
    {
        switch (op)
        {
            case FilterOperator.IsEmpty:
                return actual is null || (actual is string es && es.Length == 0);
            case FilterOperator.IsNotEmpty:
                return !(actual is null || (actual is string ns && ns.Length == 0));
        }

        // String family — compare as text, case-insensitive.
        if (op is FilterOperator.Contains or FilterOperator.NotContains
            or FilterOperator.StartsWith or FilterOperator.EndsWith)
        {
            var a = actual?.ToString() ?? "";
            var e = expected?.ToString() ?? "";
            var has = op switch
            {
                FilterOperator.Contains => a.Contains(e, StringComparison.OrdinalIgnoreCase),
                FilterOperator.NotContains => !a.Contains(e, StringComparison.OrdinalIgnoreCase),
                FilterOperator.StartsWith => a.StartsWith(e, StringComparison.OrdinalIgnoreCase),
                FilterOperator.EndsWith => a.EndsWith(e, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
            return has;
        }

        if (op is FilterOperator.Equals or FilterOperator.NotEquals)
        {
            var eq = AreEqual(actual, expected);
            return op == FilterOperator.Equals ? eq : !eq;
        }

        // Ordered comparisons.
        var cmp = Compare(actual, expected);
        if (cmp is null) return false;
        return op switch
        {
            FilterOperator.GreaterThan => cmp > 0,
            FilterOperator.GreaterOrEqual => cmp >= 0,
            FilterOperator.LessThan => cmp < 0,
            FilterOperator.LessOrEqual => cmp <= 0,
            _ => false
        };
    }

    private static bool AreEqual(object? a, object? b)
    {
        if (a is null || b is null) return a is null && b is null;

        // Numbers: compare numerically regardless of boxed type.
        if (TryToDouble(a, out var da) && TryToDouble(b, out var db))
            return da == db;

        // Bool: tolerate "true"/"false" strings from the editor.
        if (a is bool || b is bool)
            return ToBool(a) == ToBool(b);

        // Strings (and enums-as-text): case-insensitive.
        return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static int? Compare(object? a, object? b)
    {
        if (a is null || b is null) return null;

        if (TryToDouble(a, out var da) && TryToDouble(b, out var db))
            return da.CompareTo(db);

        if (TryToDate(a, out var dta) && TryToDate(b, out var dtb))
            return dta.CompareTo(dtb);

        if (a is IComparable ca && a.GetType() == b.GetType())
            return ca.CompareTo(b);

        return string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryToDouble(object? o, out double value)
    {
        value = 0;
        if (o is null) return false;
        if (o is double d) { value = d; return true; }
        if (o is bool) return false;
        if (o is DateTime || o is DateTimeOffset || o is DateOnly) return false;
        try
        {
            if (o is string s) return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            value = Convert.ToDouble(o, CultureInfo.InvariantCulture);
            return true;
        }
        catch { return false; }
    }

    private static bool TryToDate(object? o, out DateTime value)
    {
        value = default;
        switch (o)
        {
            case DateTime dt: value = dt; return true;
            case DateTimeOffset dto: value = dto.DateTime; return true;
            case DateOnly d: value = d.ToDateTime(TimeOnly.MinValue); return true;
            case string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var p):
                value = p; return true;
            default: return false;
        }
    }

    private static bool ToBool(object? o) => o switch
    {
        bool b => b,
        string s => bool.TryParse(s, out var r) && r,
        _ => false
    };
}
