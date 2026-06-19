using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Omni.Blazor.Components;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniThemePicker"/>: trigger button that
/// opens a popover with accent swatches + dark/light/system buttons.
/// </summary>
public class OmniThemePickerTests : TestContextBase
{
    public OmniThemePickerTests()
    {
        // ThemePicker injects ThemeService — register it for the tests using a
        // JSInterop-backed instance from the bUnit context.
        Services.AddSingleton<ThemeService>(sp => new ThemeService(sp.GetRequiredService<IJSRuntime>()));
    }

    [Fact]
    public void Renders_trigger_button_with_base_class()
    {
        var cut = Render<OmniThemePicker>();

        var btn = cut.Find("button.omni-theme-picker-btn");
        Assert.NotNull(btn);
        Assert.Equal("Tema", btn.GetAttribute("aria-label"));
        Assert.Equal("Tema", btn.GetAttribute("title"));
    }

    [Fact]
    public void Custom_Title_applies_to_aria_and_tooltip()
    {
        var cut = Render<OmniThemePicker>(p => p.Add(c => c.Title, "Aparência"));

        var btn = cut.Find("button.omni-theme-picker-btn");
        Assert.Equal("Aparência", btn.GetAttribute("aria-label"));
        Assert.Equal("Aparência", btn.GetAttribute("title"));
    }

    [Fact]
    public void Appends_consumer_Class_to_trigger()
    {
        var cut = Render<OmniThemePicker>(p => p.Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("button.omni-theme-picker-btn").ClassName);
    }
}
