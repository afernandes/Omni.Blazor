namespace Omni.Blazor.Models;

/// <summary>
/// A single key combination — modifier flags + a key. The <see cref="Key"/> is
/// matched case-insensitively against both <c>KeyboardEvent.key</c> and
/// <c>KeyboardEvent.code</c>, so both <c>"K"</c> and <c>"KeyK"</c> work.
/// </summary>
public readonly record struct HotkeyCombo(string Key, Modifier Modifiers)
{
    /// <summary>
    /// Parse a single combo like <c>"Ctrl+K"</c> or <c>"Ctrl+Shift+P"</c>.
    /// Recognized modifier aliases:
    /// <c>Ctrl|Control</c>, <c>Alt|Option</c>, <c>Shift</c>, <c>Meta|Cmd|Win|⌘</c>.
    /// </summary>
    public static bool TryParse(string? text, out HotkeyCombo combo)
    {
        combo = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var tokens = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return false;

        var mods = Modifier.None;
        string? key = null;
        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i];
            var m = ParseModifier(t);
            if (m is not null) { mods |= m.Value; continue; }
            // Anything not a modifier becomes the key. The last non-modifier wins
            // — this lets users write the key in any position.
            key = t;
        }
        if (key is null) return false;
        combo = new HotkeyCombo(key, mods);
        return true;
    }

    /// <summary>
    /// Parse one or more combos separated by <c>|</c>. Skips invalid entries
    /// silently — callers should pre-validate if strictness matters.
    /// </summary>
    public static HotkeyCombo[] ParseMany(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<HotkeyCombo>();
        var parts = text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<HotkeyCombo>(parts.Length);
        foreach (var p in parts)
        {
            if (TryParse(p, out var c)) list.Add(c);
        }
        return list.ToArray();
    }

    private static Modifier? ParseModifier(string token) => token.ToLowerInvariant() switch
    {
        "ctrl" or "control"      => Modifier.Ctrl,
        "alt"  or "option"       => Modifier.Alt,
        "shift"                  => Modifier.Shift,
        "meta" or "cmd" or "win" or "⌘" => Modifier.Meta,
        _ => null
    };
}
