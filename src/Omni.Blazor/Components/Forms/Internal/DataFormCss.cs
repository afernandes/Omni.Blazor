using System.Globalization;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

internal static class DataFormCss
{
    public static string LayoutStyle(DataFormLayout layout)
        => StyleBuilder.Default()
            .AddStyle("--omni-data-form-columns", Number(layout.Columns))
            .AddStyle("--omni-data-form-columns-sm", Number(layout.GetColumns(Breakpoint.Sm)))
            .AddStyle("--omni-data-form-columns-md", Number(layout.GetColumns(Breakpoint.Md)))
            .AddStyle("--omni-data-form-columns-lg", Number(layout.GetColumns(Breakpoint.Lg)))
            .AddStyle("--omni-data-form-columns-xl", Number(layout.GetColumns(Breakpoint.Xl)))
            .AddStyle("--omni-data-form-columns-xxl", Number(layout.GetColumns(Breakpoint.Xxl)))
            .AddStyle("--omni-data-form-row-gap", layout.RowGap, layout.RowGap is not null)
            .AddStyle("--omni-data-form-column-gap", layout.ColumnGap, layout.ColumnGap is not null)
            .Build();

    public static string CellStyle(
        int baseSpan,
        IReadOnlyDictionary<Breakpoint, int> responsiveSpans,
        DataFormLayout layout,
        int? columnOverride = null)
    {
        int Span(Breakpoint breakpoint)
        {
            int result = baseSpan;
            for (Breakpoint current = Breakpoint.Sm; current <= breakpoint; current++)
            {
                if (responsiveSpans.TryGetValue(current, out int value)) result = value;
            }
            return result;
        }

        return StyleBuilder.Default()
            .AddStyle("--omni-data-form-span", Number(Math.Min(baseSpan, columnOverride ?? layout.Columns)))
            .AddStyle("--omni-data-form-span-sm", Number(Math.Min(Span(Breakpoint.Sm), columnOverride ?? layout.GetColumns(Breakpoint.Sm))))
            .AddStyle("--omni-data-form-span-md", Number(Math.Min(Span(Breakpoint.Md), columnOverride ?? layout.GetColumns(Breakpoint.Md))))
            .AddStyle("--omni-data-form-span-lg", Number(Math.Min(Span(Breakpoint.Lg), columnOverride ?? layout.GetColumns(Breakpoint.Lg))))
            .AddStyle("--omni-data-form-span-xl", Number(Math.Min(Span(Breakpoint.Xl), columnOverride ?? layout.GetColumns(Breakpoint.Xl))))
            .AddStyle("--omni-data-form-span-xxl", Number(Math.Min(Span(Breakpoint.Xxl), columnOverride ?? layout.GetColumns(Breakpoint.Xxl))))
            .Build();
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
