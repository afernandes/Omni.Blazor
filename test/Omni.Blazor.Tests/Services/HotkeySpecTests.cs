using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Services;

/// <summary>
/// Parsing contract for <see cref="HotkeyService.ParseSpec"/>: single combos vs.
/// space-separated sequences ("g d" = press g then d), and mixed alternatives.
/// </summary>
public class HotkeySpecTests
{
    [Fact]
    public void Single_combo_parses_as_single_not_sequence()
    {
        var (singles, sequences) = HotkeyService.ParseSpec("Ctrl+K");
        Assert.Single(singles);
        Assert.Empty(sequences);
    }

    [Fact]
    public void Space_separated_parses_as_a_sequence()
    {
        var (singles, sequences) = HotkeyService.ParseSpec("g d");
        Assert.Empty(singles);
        Assert.Single(sequences);
        Assert.Equal(2, sequences[0].Length);
        Assert.Equal("g", sequences[0][0].Key, ignoreCase: true);
        Assert.Equal("d", sequences[0][1].Key, ignoreCase: true);
    }

    [Fact]
    public void Mixed_alternatives_split_into_singles_and_sequences()
    {
        var (singles, sequences) = HotkeyService.ParseSpec("Ctrl+K|g d");
        Assert.Single(singles);
        Assert.Single(sequences);
    }

    [Fact]
    public void Sequence_steps_can_carry_modifiers()
    {
        var (_, sequences) = HotkeyService.ParseSpec("g Ctrl+d");
        Assert.Single(sequences);
        Assert.Equal(Modifier.None, sequences[0][0].Modifiers);
        Assert.True(sequences[0][1].Modifiers.HasFlag(Modifier.Ctrl));
    }

    [Fact]
    public void Blank_spec_yields_nothing()
    {
        var (singles, sequences) = HotkeyService.ParseSpec("   ");
        Assert.Empty(singles);
        Assert.Empty(sequences);
    }

    [Theory]
    [InlineData("Ctrl + K")]
    [InlineData("Ctrl +K")]
    [InlineData("Ctrl + Shift + P")]
    public void Padding_around_plus_parses_as_a_single_combo_not_a_dead_sequence(string spec)
    {
        var (singles, sequences) = HotkeyService.ParseSpec(spec);
        Assert.Single(singles);
        Assert.Empty(sequences);
    }

    [Theory]
    [InlineData("Ctrl+K, Ctrl+D")]   // VS-style with comma + space
    [InlineData("Ctrl+K,Ctrl+D")]    // comma, no space
    [InlineData("Ctrl+K Ctrl+D")]    // space only
    public void Vs_style_chord_parses_as_a_two_step_sequence(string spec)
    {
        var (singles, sequences) = HotkeyService.ParseSpec(spec);
        Assert.Empty(singles);
        Assert.Single(sequences);
        Assert.Equal(2, sequences[0].Length);
        Assert.True(sequences[0][0].Modifiers.HasFlag(Modifier.Ctrl));
        Assert.True(sequences[0][1].Modifiers.HasFlag(Modifier.Ctrl));
        Assert.Equal("K", sequences[0][0].Key, ignoreCase: true);
        Assert.Equal("D", sequences[0][1].Key, ignoreCase: true);
    }

    [Fact]
    public void Comma_key_combo_still_parses_as_a_single_not_a_sequence()
    {
        var (singles, sequences) = HotkeyService.ParseSpec("Ctrl+,");
        Assert.Single(singles);
        Assert.Empty(sequences);
        Assert.Equal(",", singles[0].Key);
        Assert.True(singles[0].Modifiers.HasFlag(Modifier.Ctrl));
    }
}
