using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Tiny reflection helper used by <c>OmniScheduler</c> to project arbitrary
/// items into appointments by property name. <see cref="PropertyInfo"/> lookups
/// are cached per (type, name) — appointment sets are small (a visible range),
/// so per-item reflection is comfortably fast.
/// </summary>
internal static class SchedulerReflection
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _cache = new();

    /// <summary>Read a property value by name (cached). Returns null if the property doesn't exist.</summary>
    public static object? GetValue(object item, string propertyName)
    {
        var pi = _cache.GetOrAdd((item.GetType(), propertyName),
            static key => key.Item1.GetProperty(key.Item2));
        return pi?.GetValue(item);
    }

    /// <summary>Read a property as a <see cref="DateTime"/>, coercing <see cref="DateTimeOffset"/>/strings.</summary>
    public static DateTime GetDateTime(object item, string propertyName)
    {
        var value = GetValue(item, propertyName);
        return value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.LocalDateTime,
            null => default,
            _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture),
        };
    }
}

/// <summary>
/// Merges a component's own <c>class</c>/<c>style</c> with the consumer-provided
/// attributes from an <c>AppointmentRender</c>/<c>SlotRender</c> hook, so a
/// consumer style never clobbers the layout/positioning style (and vice-versa).
/// Used by the scheduler views, which splat the result via <c>@attributes</c>.
/// </summary>
internal static class SchedulerAttributes
{
    public static IReadOnlyDictionary<string, object> Merge(
        string baseClass, string baseStyle, IReadOnlyDictionary<string, object>? custom)
    {
        var dict = new Dictionary<string, object>();
        var cls = baseClass;
        var style = baseStyle;

        if (custom is not null)
        {
            foreach (var kv in custom)
            {
                if (kv.Key == "class")
                {
                    var v = kv.Value?.ToString() ?? string.Empty;
                    cls = string.IsNullOrEmpty(cls) ? v : $"{cls} {v}";
                }
                else if (kv.Key == "style")
                {
                    var v = kv.Value?.ToString() ?? string.Empty;
                    style = string.IsNullOrEmpty(style) ? v : $"{style}{v}";
                }
                else
                {
                    dict[kv.Key] = kv.Value!;
                }
            }
        }

        if (!string.IsNullOrEmpty(cls)) dict["class"] = cls;
        if (!string.IsNullOrEmpty(style)) dict["style"] = style;
        return dict;
    }
}
