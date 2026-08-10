namespace Omni.Blazor.Models;

/// <summary>Built-in layout of an <c>OmniVirtualKeyboard</c>.</summary>
public enum VirtualKeyboardType
{
    /// <summary>Full QWERTY: digits, letters, shift, symbols, space and punctuation.</summary>
    Standard,

    /// <summary>Telephone-style 3x4 digit pad.</summary>
    Numeric,

    /// <summary>QWERTY without the digit row, plus <c>@</c>, <c>.</c> and a <c>.com</c> key.</summary>
    Email
}

/// <summary>
/// What a key does. Anything other than <see cref="Character"/> is handled by the
/// keyboard itself rather than inserted into the value.
/// </summary>
public enum VirtualKeyboardKeyKind
{
    /// <summary>Inserts its text into the value.</summary>
    Character,

    /// <summary>Shifts the next character. Cleared once one is typed.</summary>
    Shift,

    /// <summary>Deletes the character before the caret.</summary>
    Backspace,

    /// <summary>Inserts a space.</summary>
    Space,

    /// <summary>Toggles the symbol set.</summary>
    Symbols,

    /// <summary>Raises <c>OnEnter</c> and does not change the value.</summary>
    Enter,

    /// <summary>Empties the value.</summary>
    Clear,

    /// <summary>Occupies its slot without rendering a key. Use it to align a row.</summary>
    Blank
}

/// <summary>One key of a <see cref="VirtualKeyboardLayout"/>.</summary>
public sealed class VirtualKeyboardKey
{
    /// <summary>Character inserted when pressed. Empty for non-character keys.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// What is drawn on the key. Defaults to <see cref="Text"/> — set it when the two
    /// differ, as on <c>⌫</c> or a wide space bar.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>What the key does. Defaults to <see cref="VirtualKeyboardKeyKind.Character"/>.</summary>
    public VirtualKeyboardKeyKind Kind { get; init; } = VirtualKeyboardKeyKind.Character;

    /// <summary>
    /// Width relative to the other keys in the row: 1 is a normal key, 2 twice as wide.
    /// A row divides its space in proportion to these, so rows need not sum to anything.
    /// </summary>
    public double Width { get; init; } = 1;

    /// <summary>Accessible name. Falls back to the label, then the text.</summary>
    public string? AriaLabel { get; init; }
}

/// <summary>One row of keys.</summary>
public sealed class VirtualKeyboardRow
{
    /// <summary>Keys in the row, left to right.</summary>
    public IReadOnlyList<VirtualKeyboardKey> Keys { get; init; } = [];
}

/// <summary>
/// A complete keyboard: the rows to draw, plus what Shift and Symbols substitute.
/// Pass one to <c>OmniVirtualKeyboard.Layout</c> to replace the built-in layouts.
/// </summary>
public sealed class VirtualKeyboardLayout
{
    /// <summary>Identifies the layout. Shown in no UI; useful when swapping layouts.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Rows to draw, top to bottom.</summary>
    public IReadOnlyList<VirtualKeyboardRow> Rows { get; init; } = [];

    /// <summary>
    /// Substitutions while Shift is held, keyed by <see cref="VirtualKeyboardKey.Text"/>.
    /// A letter with no entry here is uppercased instead.
    /// </summary>
    public IReadOnlyDictionary<string, string> ShiftMap { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Substitutions while the symbol set is active. Takes precedence over Shift.</summary>
    public IReadOnlyDictionary<string, string> SymbolsMap { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>Raised for every key press, after the value has been updated.</summary>
/// <param name="Key">The key that was pressed.</param>
/// <param name="Text">
/// What the key produced after Shift and Symbols were applied — empty for keys that
/// insert nothing.
/// </param>
public sealed record VirtualKeyboardKeyEventArgs(VirtualKeyboardKey Key, string Text)
{
    /// <summary>Convenience over <c>Key.Kind</c>.</summary>
    public VirtualKeyboardKeyKind Kind => Key.Kind;
}
