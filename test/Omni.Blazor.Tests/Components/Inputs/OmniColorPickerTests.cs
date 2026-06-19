using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniColorPicker"/>: trigger/swatch
/// rendering reflecting Value, panel open/close, the optional alpha/input/preset
/// sections, preset + hex commit paths, and the cross-cutting splat. The SV-drag
/// path uses pointer capture (real JS) and is covered by integration tests.
/// </summary>
public class OmniColorPickerTests : TestContextBase
{
    [Fact]
    public void Renders_root_and_trigger()
    {
        var cut = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));

        Assert.NotNull(cut.Find("div.omni-colorpicker"));
        Assert.NotNull(cut.Find(".omni-colorpicker-trigger"));
    }

    [Fact]
    public void Trigger_text_shows_current_hex()
    {
        var cut = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));

        Assert.Contains("#ff0000", cut.Find(".omni-colorpicker-text").TextContent);
    }

    [Fact]
    public void Trigger_swatch_reflects_value_color()
    {
        var cut = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));

        var style = cut.Find(".omni-colorpicker-swatch-fill").GetAttribute("style") ?? "";
        Assert.Contains("255,0,0", style);
    }

    [Fact]
    public void Falls_back_to_DefaultColor_when_value_invalid()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "not-a-color")
            .Add(c => c.DefaultColor, "#00ff00"));

        Assert.Contains("#00ff00", cut.Find(".omni-colorpicker-text").TextContent);
    }

    [Fact]
    public void Panel_hidden_until_trigger_clicked()
    {
        var cut = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));
        Assert.Empty(cut.FindAll(".omni-colorpicker-panel"));

        cut.Find(".omni-colorpicker-trigger").Click();

        Assert.NotNull(cut.Find(".omni-colorpicker-panel"));
        Assert.NotNull(cut.Find(".omni-colorpicker-sv"));
        Assert.NotNull(cut.Find(".omni-colorpicker-hue"));
    }

    [Fact]
    public void Alpha_slider_only_with_ShowAlpha()
    {
        var noAlpha = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));
        noAlpha.Find(".omni-colorpicker-trigger").Click();
        Assert.Empty(noAlpha.FindAll(".omni-colorpicker-alpha"));

        var withAlpha = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").Add(c => c.ShowAlpha, true));
        withAlpha.Find(".omni-colorpicker-trigger").Click();
        Assert.NotNull(withAlpha.Find(".omni-colorpicker-alpha"));
    }

    [Fact]
    public void Hex_field_toggles_with_ShowInput()
    {
        var withInput = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));
        withInput.Find(".omni-colorpicker-trigger").Click();
        Assert.NotNull(withInput.Find(".omni-colorpicker-hexfield"));

        var noInput = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").Add(c => c.ShowInput, false));
        noInput.Find(".omni-colorpicker-trigger").Click();
        Assert.Empty(noInput.FindAll(".omni-colorpicker-hexfield"));
    }

    [Fact]
    public void Presets_render_from_palette()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000")
            .Add(c => c.Palette, new[] { "#111111", "#222222", "#333333" }));
        cut.Find(".omni-colorpicker-trigger").Click();

        Assert.Equal(3, cut.FindAll(".omni-colorpicker-preset").Count);
    }

    [Fact]
    public void Presets_hidden_when_ShowPresets_false()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").Add(c => c.ShowPresets, false));
        cut.Find(".omni-colorpicker-trigger").Click();

        Assert.Empty(cut.FindAll(".omni-colorpicker-preset"));
    }

    [Fact]
    public void Clicking_preset_commits_value()
    {
        string? captured = null;
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#000000")
            .Add(c => c.Palette, new[] { "#ff0000", "#00ff00" })
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => captured = v)));
        cut.Find(".omni-colorpicker-trigger").Click();

        cut.FindAll(".omni-colorpicker-preset")[0].Click();

        Assert.Equal("#ff0000", captured);
    }

    [Fact]
    public void Valid_hex_input_commits_value()
    {
        string? captured = null;
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#000000")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => captured = v)));
        cut.Find(".omni-colorpicker-trigger").Click();

        cut.Find(".omni-colorpicker-hexfield").Change("#00ff00");

        Assert.Equal("#00ff00", captured);
    }

    [Fact]
    public void Invalid_hex_input_does_not_commit()
    {
        string? captured = null;
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#000000")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => captured = v)));
        cut.Find(".omni-colorpicker-trigger").Click();

        cut.Find(".omni-colorpicker-hexfield").Change("not-a-color");

        Assert.Null(captured);
    }

    [Fact]
    public void Disabled_blocks_opening()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").Add(c => c.Disabled, true));

        Assert.NotNull(cut.Find(".omni-colorpicker-trigger").GetAttribute("disabled"));

        cut.Find(".omni-colorpicker-trigger").Click();
        Assert.Empty(cut.FindAll(".omni-colorpicker-panel"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-colorpicker").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").Add(c => c.Style, "min-width: 320px"));

        Assert.Contains("min-width: 320px", cut.Find("div.omni-colorpicker").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, "#ff0000").AddUnmatched("data-testid", "cp"));

        Assert.Equal("cp", cut.Find("div.omni-colorpicker").GetAttribute("data-testid"));
    }

    // ── ParameterState: re-parse fires only when Value changes ──

    private sealed class Model
    {
        public string? Brand { get; set; }
        public string? Accent { get; set; }
    }

    [Fact]
    public void Initial_ValueExpression_triggers_recompute()
    {
        var model = new Model { Brand = "#ff0000" };
        var cut = Render<OmniColorPicker>(p => p
            .Add(c => c.Value, model.Brand)
            .Add(c => c.ValueExpression, () => model.Brand));

        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_fires_when_Value_changes()
    {
        var cut = Render<OmniColorPicker>(p => p.Add(c => c.Value, "#ff0000"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.Value, "#00ff00"));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
    }
}
