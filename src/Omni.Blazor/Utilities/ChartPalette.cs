using Omni.Blazor.Models;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Provides CSS color stops for chart series, indexed by series position.
/// Uses raw hex values for multi-series palettes and CSS variables for
/// semantic/accent schemes so they auto-adapt to theme switches.
/// </summary>
internal static class ChartPalette
{
    // 12 cores Tailwind-harmonizadas — mesma régua dos data-accents.
    private static readonly string[] PaletteHex =
    {
        "#d97706", "#059669", "#2563eb", "#7c3aed", "#dc2626", "#0d9488",
        "#ea580c", "#0891b2", "#4f46e5", "#c026d3", "#65a30d", "#e11d48",
    };

    // Variações monocromáticas do accent atual via color-mix percentuais.
    private static readonly string[] AccentMix =
    {
        "var(--omni-accent)",
        "color-mix(in oklab, var(--omni-accent) 75%, white)",
        "color-mix(in oklab, var(--omni-accent) 60%, black)",
        "color-mix(in oklab, var(--omni-accent) 45%, white)",
        "color-mix(in oklab, var(--omni-accent) 30%, black)",
        "color-mix(in oklab, var(--omni-accent) 90%, var(--omni-info))",
    };

    private static readonly string[] PastelHex =
    {
        "#fdba74", "#86efac", "#93c5fd", "#c4b5fd", "#fca5a5", "#5eead4",
        "#fed7aa", "#67e8f9", "#a5b4fc", "#f0abfc", "#d9f99d", "#fda4af",
    };

    // Semantic — 5 cores (Good, Warn, Danger, Info, Accent) que repetem.
    private static readonly string[] Semantic =
    {
        "var(--omni-good)",
        "var(--omni-warn)",
        "var(--omni-danger)",
        "var(--omni-info)",
        "var(--omni-accent)",
    };

    /// <summary>Returns the color at <paramref name="index"/> for the given
    /// scheme. Wraps around using modulo.</summary>
    public static string ColorAt(ChartColorScheme scheme, int index)
    {
        var palette = scheme switch
        {
            ChartColorScheme.Palette  => PaletteHex,
            ChartColorScheme.Accent   => AccentMix,
            ChartColorScheme.Pastel   => PastelHex,
            ChartColorScheme.Semantic => Semantic,
            _ => PaletteHex,
        };
        return palette[((index % palette.Length) + palette.Length) % palette.Length];
    }
}
