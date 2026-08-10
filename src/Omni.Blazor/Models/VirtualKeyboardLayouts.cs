namespace Omni.Blazor.Models;

/// <summary>
/// The layouts behind <see cref="VirtualKeyboardType"/>. They are ordinary
/// <see cref="VirtualKeyboardLayout"/> values, so a consumer can start from one and
/// adjust it rather than describing a whole keyboard from scratch.
/// </summary>
public static class VirtualKeyboardLayouts
{
    /// <summary>Returns the built-in layout for <paramref name="type"/>.</summary>
    public static VirtualKeyboardLayout For(VirtualKeyboardType type) => type switch
    {
        VirtualKeyboardType.Numeric => Numeric(),
        VirtualKeyboardType.Email => Email(),
        _ => Standard()
    };

    /// <summary>Full QWERTY with a digit row, Shift, Symbols and a space bar.</summary>
    public static VirtualKeyboardLayout Standard() => new()
    {
        Name = "Standard",
        Rows =
        [
            Row("1", "2", "3", "4", "5", "6", "7", "8", "9", "0"),
            Row("q", "w", "e", "r", "t", "y", "u", "i", "o", "p"),
            Row("a", "s", "d", "f", "g", "h", "j", "k", "l", "ç"),
            new VirtualKeyboardRow
            {
                Keys =
                [
                    new() { Kind = VirtualKeyboardKeyKind.Shift, Label = "⇧", Width = 1.5 },
                    Char("z"), Char("x"), Char("c"), Char("v"),
                    Char("b"), Char("n"), Char("m"),
                    new() { Kind = VirtualKeyboardKeyKind.Backspace, Label = "⌫", Width = 1.5 }
                ]
            },
            new VirtualKeyboardRow
            {
                Keys =
                [
                    new() { Kind = VirtualKeyboardKeyKind.Symbols, Label = "?!#", Width = 1.5 },
                    new() { Kind = VirtualKeyboardKeyKind.Space, Width = 4.5 },
                    Char("-"), Char(","), Char("."), Char(";")
                ]
            }
        ],
        ShiftMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["1"] = "!", ["2"] = "@", ["3"] = "#", ["4"] = "$", ["5"] = "%",
            ["6"] = "?", ["7"] = "&", ["8"] = "*", ["9"] = "(", ["0"] = ")"
        },
        SymbolsMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["1"] = "!", ["2"] = "@", ["3"] = "#", ["4"] = "$", ["5"] = "%",
            ["6"] = "?", ["7"] = "&", ["8"] = "*", ["9"] = "(", ["0"] = ")",
            ["-"] = "_", [","] = "<", ["."] = ">", [";"] = ":"
        }
    };

    /// <summary>Telephone-style digit pad. The bottom-left slot is blank, to align 0 under 8.</summary>
    public static VirtualKeyboardLayout Numeric() => new()
    {
        Name = "Numeric",
        Rows =
        [
            Row("1", "2", "3"),
            Row("4", "5", "6"),
            Row("7", "8", "9"),
            new VirtualKeyboardRow
            {
                Keys =
                [
                    new() { Kind = VirtualKeyboardKeyKind.Blank },
                    Char("0"),
                    new() { Kind = VirtualKeyboardKeyKind.Backspace, Label = "⌫" }
                ]
            }
        ]
    };

    /// <summary>QWERTY without digits, with the keys an address actually needs.</summary>
    public static VirtualKeyboardLayout Email() => new()
    {
        Name = "Email",
        Rows =
        [
            Row("q", "w", "e", "r", "t", "y", "u", "i", "o", "p"),
            Row("a", "s", "d", "f", "g", "h", "j", "k", "l", "_"),
            new VirtualKeyboardRow
            {
                Keys =
                [
                    Char("z"), Char("x"), Char("c"), Char("v"), Char("b"),
                    Char("n"), Char("m"), Char("-"), Char("."),
                    new() { Kind = VirtualKeyboardKeyKind.Backspace, Label = "⌫", Width = 1.5 }
                ]
            },
            new VirtualKeyboardRow
            {
                Keys =
                [
                    new() { Kind = VirtualKeyboardKeyKind.Shift, Label = "⇧", Width = 1.5 },
                    Char("@"),
                    new() { Text = ".com", Label = ".com", Width = 2 },
                    new() { Kind = VirtualKeyboardKeyKind.Space, Width = 3 },
                    new() { Kind = VirtualKeyboardKeyKind.Clear, Label = "⌧", Width = 1.5 }
                ]
            }
        ]
    };

    private static VirtualKeyboardRow Row(params string[] characters) =>
        new() { Keys = [.. characters.Select(Char)] };

    private static VirtualKeyboardKey Char(string text) => new() { Text = text };
}
