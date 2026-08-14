namespace Omni.Blazor.Components;

/// <summary>
/// Flags for the regions <c>OmniCulturePicker</c> is most often asked to show.
///
/// This is deliberately a short list rather than an atlas: there are some 250 regions,
/// and a component library that half-draws them serves nobody. A region that is not here
/// falls back to a code badge, and a consumer who needs a particular flag supplies it
/// through <c>OmniCulturePicker.FlagTemplate</c>.
///
/// Each entry is a complete SVG on a 28×20 viewBox, simplified for a chip roughly 22px
/// wide — the point is recognition at that size, not heraldic accuracy.
/// </summary>
internal static class OmniFlagLibrary
{
    private static readonly Dictionary<string, string> _flags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BR"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#009b3a"/><path d="M14 2.6 25.4 10 14 17.4 2.6 10Z" fill="#fedf00"/><circle cx="14" cy="10" r="4.2" fill="#002776"/><path d="M9.9 8.7c2.7-.7 5.6-.5 8.2.6" stroke="#fff" stroke-width="1.15" fill="none"/></svg>""",
        ["PT"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#f00"/><rect width="11.2" height="20" fill="#060"/><circle cx="11.2" cy="10" r="3.4" fill="#ff0" stroke="#fff" stroke-width=".5"/><circle cx="11.2" cy="10" r="1.9" fill="#fff"/><circle cx="11.2" cy="10" r="1.9" fill="none" stroke="#039" stroke-width="1.1"/></svg>""",
        ["US"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#fff"/><g fill="#b22234"><rect width="28" height="1.54"/><rect width="28" height="1.54" y="3.08"/><rect width="28" height="1.54" y="6.16"/><rect width="28" height="1.54" y="9.24"/><rect width="28" height="1.54" y="12.32"/><rect width="28" height="1.54" y="15.4"/><rect width="28" height="1.54" y="18.46"/></g><rect width="12" height="10.8" fill="#3c3b6e"/><g fill="#fff"><circle cx="2.4" cy="2.2" r=".7"/><circle cx="6" cy="2.2" r=".7"/><circle cx="9.6" cy="2.2" r=".7"/><circle cx="4.2" cy="4.5" r=".7"/><circle cx="7.8" cy="4.5" r=".7"/><circle cx="2.4" cy="6.8" r=".7"/><circle cx="6" cy="6.8" r=".7"/><circle cx="9.6" cy="6.8" r=".7"/><circle cx="4.2" cy="9.1" r=".7"/><circle cx="7.8" cy="9.1" r=".7"/></g></svg>""",
        // Stroke widths here are load-bearing: any thicker and the diagonals plus the
        // cross swallow the blue quadrants entirely, leaving a red square.
        ["GB"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#012169"/><path d="M0 0 28 20M28 0 0 20" stroke="#fff" stroke-width="2.6"/><path d="M0 0 28 20M28 0 0 20" stroke="#c8102e" stroke-width="1.2"/><path d="M14 0v20M0 10h28" stroke="#fff" stroke-width="4.6"/><path d="M14 0v20M0 10h28" stroke="#c8102e" stroke-width="2.6"/></svg>""",
        ["FR"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#fff"/><rect width="9.33" height="20" fill="#002395"/><rect width="9.33" height="20" x="18.67" fill="#ed2939"/></svg>""",
        ["IT"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#fff"/><rect width="9.33" height="20" fill="#009246"/><rect width="9.33" height="20" x="18.67" fill="#ce2b37"/></svg>""",
        ["DE"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#ffce00"/><rect width="28" height="13.34" fill="#d00"/><rect width="28" height="6.67" fill="#000"/></svg>""",
        ["ES"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#c60b1e"/><rect width="28" height="10" y="5" fill="#ffc400"/><rect x="4" y="7.6" width="3.6" height="4.8" rx=".6" fill="#c60b1e" opacity=".85"/></svg>""",
        ["JP"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#fff"/><circle cx="14" cy="10" r="5.4" fill="#bc002d"/></svg>""",
        ["SA"] = """<svg viewBox="0 0 28 20" xmlns="http://www.w3.org/2000/svg"><rect width="28" height="20" fill="#165d31"/><path d="M6 8.2h16" stroke="#fff" stroke-width="1.5" stroke-linecap="round"/><path d="M7 12.4h13.5" stroke="#fff" stroke-width="1.1" stroke-linecap="round"/><path d="M20.4 11.2v2.6" stroke="#fff" stroke-width="1.1" stroke-linecap="round"/></svg>""",
    };

    /// <summary>Returns the SVG for a region, or null when it is not in the set.</summary>
    internal static string? Get(string? region) =>
        !string.IsNullOrWhiteSpace(region) && _flags.TryGetValue(region, out string? svg) ? svg : null;

    /// <summary>Regions this library can draw.</summary>
    internal static IEnumerable<string> Regions => _flags.Keys;
}
