namespace Omni.Blazor.Models;

/// <summary>
/// A single entry for <c>OmniCommandPalette</c> (⌘K): a label plus optional
/// category (group), icon, shortcut hint, extra search keywords, and an action.
/// Product-agnostic — the consumer fills the list and routes via <see cref="Action"/>
/// or the palette's OnCommand callback.
/// </summary>
public sealed class OmniCommand
{
    public OmniCommand() { }

    public OmniCommand(string label, Func<Task>? action = null)
    {
        Label = label;
        Action = action;
    }

    /// <summary>Primary text shown (and the main search target).</summary>
    public string Label { get; set; } = "";

    /// <summary>Optional group heading the command is listed under.</summary>
    public string? Category { get; set; }

    /// <summary>Optional leading icon (any OmniIcon name).</summary>
    public string? Icon { get; set; }

    /// <summary>Optional keyboard-shortcut hint shown on the right (e.g. "⌘P").</summary>
    public string? Shortcut { get; set; }

    /// <summary>Extra terms that also match this command in search.</summary>
    public string[]? Keywords { get; set; }

    /// <summary>Optional action invoked when the command is chosen.</summary>
    public Func<Task>? Action { get; set; }

    /// <summary>
    /// Optional sub-commands. When set (and non-empty), choosing this command
    /// drills into a nested page instead of running <see cref="Action"/> — the
    /// palette pushes a page, and Esc/Backspace pops back out.
    /// </summary>
    public IEnumerable<OmniCommand>? Children { get; set; }
}
