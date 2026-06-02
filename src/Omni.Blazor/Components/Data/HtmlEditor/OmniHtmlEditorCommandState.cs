namespace Omni.Blazor.Components;

/// <summary>
/// Snapshot of the editor's current command state (from <c>queryCommandState</c>),
/// returned by the JS engine after every command and on selection change. Tools
/// read it to highlight themselves (e.g. Bold is "active" when the caret is in
/// bold text).
/// </summary>
public sealed class OmniHtmlEditorCommandState
{
    public string? Html { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool StrikeThrough { get; set; }
    public bool JustifyLeft { get; set; }
    public bool JustifyCenter { get; set; }
    public bool JustifyRight { get; set; }
    public bool InsertOrderedList { get; set; }
    public bool InsertUnorderedList { get; set; }
    public bool Subscript { get; set; }
    public bool Superscript { get; set; }
    public string? FormatBlock { get; set; }
    public bool Undo { get; set; }
    public bool Redo { get; set; }
    public bool Unlink { get; set; }

    /// <summary>Whether the given execCommand name is currently active (for toolbar highlighting).</summary>
    public bool IsActive(string command) => command switch
    {
        "bold" => Bold,
        "italic" => Italic,
        "underline" => Underline,
        "strikeThrough" => StrikeThrough,
        "justifyLeft" => JustifyLeft,
        "justifyCenter" => JustifyCenter,
        "justifyRight" => JustifyRight,
        "insertOrderedList" => InsertOrderedList,
        "insertUnorderedList" => InsertUnorderedList,
        "subscript" => Subscript,
        "superscript" => Superscript,
        _ => false
    };
}
