namespace Omni.Blazor.Utilities;

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
