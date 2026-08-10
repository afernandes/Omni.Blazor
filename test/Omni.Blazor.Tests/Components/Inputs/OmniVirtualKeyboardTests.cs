using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniVirtualKeyboard"/>: the cross-cutting
/// Class/Style/Attributes surface, then the parts a touch terminal depends on — what
/// each key kind does to the bound value, and how Shift and Symbols resolve the
/// character a key produces.
/// </summary>
public class OmniVirtualKeyboardTests : TestContextBase
{
    private IRenderedComponent<OmniVirtualKeyboard> RenderKeyboard(
        Action<ComponentParameterCollectionBuilder<OmniVirtualKeyboard>>? extra = null,
        string value = "")
        => Render<OmniVirtualKeyboard>(p =>
        {
            p.Add(c => c.Value, value);
            extra?.Invoke(p);
        });

    /// <summary>Finds a key by what is drawn on it.</summary>
    private static AngleSharp.Dom.IElement Key(
        IRenderedComponent<OmniVirtualKeyboard> cut, string label)
        => cut.FindAll("button.omni-vkb-key").First(b => b.TextContent.Trim() == label);

    [Fact]
    public void Renders_root_div_with_base_class()
    {
        var cut = RenderKeyboard();

        var root = cut.Find("div.omni-vkb");
        Assert.Contains("omni-vkb", root.ClassName);
        Assert.Equal("group", root.GetAttribute("role"));
    }

    [Fact]
    public void Appends_consumer_class_to_root()
    {
        var cut = RenderKeyboard(p => p.Add(c => c.Class, "minha-classe"));

        Assert.Contains("minha-classe", cut.Find("div.omni-vkb").ClassName);
    }

    [Fact]
    public void Forwards_consumer_style_to_root()
    {
        var cut = RenderKeyboard(p => p.Add(c => c.Style, "margin-top:8px"));

        Assert.Contains("margin-top:8px", cut.Find("div.omni-vkb").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_attributes_onto_root()
    {
        var cut = RenderKeyboard(p => p.AddUnmatched("data-testid", "teclado"));

        Assert.Equal("teclado", cut.Find("div.omni-vkb").GetAttribute("data-testid"));
    }

    [Theory]
    [InlineData(VirtualKeyboardType.Standard, 5)]
    [InlineData(VirtualKeyboardType.Numeric, 4)]
    [InlineData(VirtualKeyboardType.Email, 4)]
    public void Renders_one_row_per_layout_row(VirtualKeyboardType type, int expectedRows)
    {
        var cut = RenderKeyboard(p => p.Add(c => c.Type, type));

        Assert.Equal(expectedRows, cut.FindAll("div.omni-vkb-row").Count);
    }

    [Fact]
    public void Character_key_appends_to_the_bound_value()
    {
        string? bound = "ab";
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)),
            value: "ab");

        Key(cut, "c").Click();

        Assert.Equal("abc", bound);
    }

    [Fact]
    public void Backspace_removes_the_last_character()
    {
        string? bound = "abc";
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)),
            value: "abc");

        Key(cut, "⌫").Click();

        Assert.Equal("ab", bound);
    }

    [Fact]
    public void Backspace_on_an_empty_value_is_a_no_op()
    {
        bool raised = false;
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => raised = true)));

        Key(cut, "⌫").Click();

        Assert.False(raised);
    }

    [Fact]
    public void Space_key_appends_a_space()
    {
        string? bound = "oi";
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)),
            value: "oi");

        Key(cut, "Espaço").Click();

        Assert.Equal("oi ", bound);
    }

    [Fact]
    public void Clear_key_empties_the_value()
    {
        string? bound = "algo";
        var cut = RenderKeyboard(
            p => p
                .Add(c => c.Type, VirtualKeyboardType.Email)
                .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)),
            value: "algo");

        Key(cut, "⌧").Click();

        Assert.Equal(string.Empty, bound);
    }

    [Fact]
    public void Enter_key_raises_OnEnter_and_leaves_the_value_alone()
    {
        bool entered = false;
        bool valueChanged = false;
        var cut = Render<OmniVirtualKeyboard>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Layout, new VirtualKeyboardLayout
            {
                Rows = [new VirtualKeyboardRow { Keys = [new() { Kind = VirtualKeyboardKeyKind.Enter }] }]
            })
            .Add(c => c.OnEnter, EventCallback.Factory.Create(this, () => entered = true))
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => valueChanged = true)));

        Key(cut, "⏎").Click();

        Assert.True(entered);
        Assert.False(valueChanged);
    }

    [Fact]
    public void Shift_uppercases_a_letter_and_then_releases()
    {
        List<string?> writes = [];
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => writes.Add(v))));

        Key(cut, "⇧").Click();
        Key(cut, "A").Click();   // relabelled by Shift
        Key(cut, "b").Click();   // Shift is one-shot, so this one stays lower case

        Assert.Equal(["A", "Ab"], writes);
    }

    [Fact]
    public void Shift_maps_a_digit_to_its_symbol_instead_of_uppercasing()
    {
        string? bound = null;
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)));

        Key(cut, "⇧").Click();
        Key(cut, "!").Click();   // "1" is relabelled by the shift map

        Assert.Equal("!", bound);
    }

    [Fact]
    public void Symbols_is_a_mode_and_stays_on_until_pressed_again()
    {
        List<string?> writes = [];
        var cut = RenderKeyboard(
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => writes.Add(v))));

        Key(cut, "?!#").Click();
        Key(cut, "@").Click();   // "2" under the symbol map
        Key(cut, "#").Click();   // "3" — still in symbol mode
        Key(cut, "?!#").Click(); // back off
        Key(cut, "3").Click();

        Assert.Equal(["@", "@#", "@#3"], writes);
    }

    [Fact]
    public void Modifier_keys_report_their_state_to_assistive_tech()
    {
        var cut = RenderKeyboard();

        Assert.Equal("false", Key(cut, "⇧").GetAttribute("aria-pressed"));

        Key(cut, "⇧").Click();

        Assert.Equal("true", Key(cut, "⇧").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void MaxLength_stops_further_characters()
    {
        string? bound = "ab";
        var cut = RenderKeyboard(
            p => p
                .Add(c => c.MaxLength, 3)
                .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => bound = v)),
            value: "ab");

        Key(cut, "c").Click();
        Assert.Equal("abc", bound);

        cut.Render(p => p.Add(c => c.Value, "abc"));
        Key(cut, "d").Click();

        Assert.Equal("abc", bound);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Disabled_and_readonly_keyboards_do_not_write(bool disabled, bool readOnly)
    {
        bool raised = false;
        var cut = RenderKeyboard(p => p
            .Add(c => c.Disabled, disabled)
            .Add(c => c.ReadOnly, readOnly)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, _ => raised = true)));

        Assert.All(cut.FindAll("button.omni-vkb-key"), b => Assert.True(b.HasAttribute("disabled")));

        Key(cut, "a").Click();

        Assert.False(raised);
    }

    [Fact]
    public void OnKeyPress_reports_the_key_and_what_it_produced()
    {
        List<VirtualKeyboardKeyEventArgs> pressed = [];
        var cut = RenderKeyboard(p => p.Add(
            c => c.OnKeyPress,
            EventCallback.Factory.Create<VirtualKeyboardKeyEventArgs>(this, e => pressed.Add(e))));

        Key(cut, "⇧").Click();
        Key(cut, "A").Click();

        Assert.Equal(2, pressed.Count);
        Assert.Equal(VirtualKeyboardKeyKind.Shift, pressed[0].Kind);
        Assert.Equal(string.Empty, pressed[0].Text);
        Assert.Equal(VirtualKeyboardKeyKind.Character, pressed[1].Kind);
        Assert.Equal("A", pressed[1].Text);
    }

    [Fact]
    public void Blank_slot_renders_a_spacer_rather_than_a_key()
    {
        var cut = RenderKeyboard(p => p.Add(c => c.Type, VirtualKeyboardType.Numeric));

        Assert.Single(cut.FindAll("span.omni-vkb-blank"));
        // 10 digits + backspace, and the blank is not one of them.
        Assert.Equal(11, cut.FindAll("button.omni-vkb-key").Count);
    }

    [Fact]
    public void A_custom_layout_replaces_the_built_in_one()
    {
        var cut = RenderKeyboard(p => p.Add(c => c.Layout, new VirtualKeyboardLayout
        {
            Name = "Custom",
            Rows = [new VirtualKeyboardRow { Keys = [new() { Text = "x" }, new() { Text = "y" }] }]
        }));

        Assert.Single(cut.FindAll("div.omni-vkb-row"));
        Assert.Equal(2, cut.FindAll("button.omni-vkb-key").Count);
    }

    [Fact]
    public void Key_widths_become_flex_proportions_regardless_of_culture()
    {
        // A comma here would silently void the declaration in every pt-BR browser.
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            var cut = RenderKeyboard(p => p.Add(c => c.Layout, new VirtualKeyboardLayout
            {
                Rows = [new VirtualKeyboardRow { Keys = [new() { Text = "a", Width = 1.5 }] }]
            }));

            Assert.Contains("1.5 1 0", cut.Find("button.omni-vkb-key").GetAttribute("style"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Switching_layout_clears_a_modifier_left_on_by_the_previous_one()
    {
        var cut = RenderKeyboard();

        Key(cut, "?!#").Click();
        Assert.Equal("true", Key(cut, "?!#").GetAttribute("aria-pressed"));

        cut.Render(p => p.Add(c => c.Type, VirtualKeyboardType.Email));
        cut.Render(p => p.Add(c => c.Type, VirtualKeyboardType.Standard));

        Assert.Equal("false", Key(cut, "?!#").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Layout_is_not_rebuilt_when_only_class_or_style_change()
    {
        var cut = RenderKeyboard();
        int afterFirstRender = cut.Instance._layoutRecomputeCount;

        cut.Render(p => p.Add(c => c.Class, "x"));
        cut.Render(p => p.Add(c => c.Style, "color:red"));

        Assert.Equal(afterFirstRender, cut.Instance._layoutRecomputeCount);

        cut.Render(p => p.Add(c => c.Type, VirtualKeyboardType.Numeric));

        Assert.Equal(afterFirstRender + 1, cut.Instance._layoutRecomputeCount);
    }
}
