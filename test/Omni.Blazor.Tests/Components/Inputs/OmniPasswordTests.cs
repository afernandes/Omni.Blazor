using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniPassword"/>: input type toggle,
/// size modifiers, two-way Value binding, and the cross-cutting splat.
/// </summary>
public class OmniPasswordTests : TestContextBase
{
    [Fact]
    public void Renders_password_input_inside_group()
    {
        var cut = Render<OmniPassword>();
        var input = cut.Find("input");
        Assert.Equal("password", input.GetAttribute("type"));
        Assert.Contains("omni-input", input.ClassName);
    }

    [Fact]
    public void Toggle_button_switches_input_type_to_text()
    {
        var cut = Render<OmniPassword>();

        Assert.Equal("password", cut.Find("input").GetAttribute("type"));
        cut.Find("button").Click();
        Assert.Equal("text", cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void Toggle_button_has_an_accessible_name()
    {
        var cut = Render<OmniPassword>();
        var button = cut.Find("button");

        // Ícone sem texto: sem estes dois o botão não tem nome acessível.
        Assert.Equal("Mostrar senha", button.GetAttribute("aria-label"));
        Assert.Equal("Mostrar senha", button.GetAttribute("title"));
        Assert.Equal("false", button.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Toggle_button_name_and_pressed_state_follow_visibility()
    {
        var cut = Render<OmniPassword>();
        cut.Find("button").Click();

        var button = cut.Find("button");
        Assert.Equal("Ocultar senha", button.GetAttribute("aria-label"));
        Assert.Equal("Ocultar senha", button.GetAttribute("title"));
        Assert.Equal("true", button.GetAttribute("aria-pressed"));
    }

    [Fact]
    public void ShowToggle_false_hides_eye_button()
    {
        var cut = Render<OmniPassword>(p => p.Add(c => c.ShowToggle, false));
        Assert.Empty(cut.FindAll("button"));
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-input-sm")]
    [InlineData(ComponentSize.Lg, "omni-input-lg")]
    public void Applies_size_modifier_to_input(ComponentSize size, string expected)
    {
        var cut = Render<OmniPassword>(p => p.Add(c => c.Size, size));
        Assert.Contains(expected, cut.Find("input").ClassName);
    }

    [Fact]
    public void Input_event_propagates_to_ValueChanged()
    {
        string? captured = null;
        var cut = Render<OmniPassword>(p => p
            .Add(c => c.Value, "")
            .Add(c => c.ValueChanged, v => captured = v));

        cut.Find("input").Input("s3cret");
        Assert.Equal("s3cret", captured);
    }

    [Fact]
    public void Appends_consumer_Class_to_root_group()
    {
        var cut = Render<OmniPassword>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-input-group").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniPassword>(p => p.Add(c => c.Style, "width: 240px"));
        Assert.Equal("width: 240px", cut.Find("div.omni-input-group").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniPassword>(p => p
            .AddUnmatched("data-testid", "pw1"));

        Assert.Equal("pw1", cut.Find("div.omni-input-group").GetAttribute("data-testid"));
    }

    [Fact]
    public void Edge_native_reveal_and_clear_are_hidden_by_the_css_bundle()
    {
        // Edge draws its own reveal eye inside input[type=password] (and its own
        // clear "×" in text inputs), which lands next to the eye this component
        // already renders. Only the shipped bundle can prove the rules ship, since
        // the pseudo-elements are Edge-only and no bUnit render exercises them.
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "Omni.Blazor", "wwwroot", "css", "omni.css"));

        foreach (string selector in new[] { ".omni-input::-ms-reveal", ".omni-input::-ms-clear" })
        {
            int start = css.IndexOf(selector, StringComparison.Ordinal);
            Assert.True(start >= 0, $"The shipped CSS bundle does not contain '{selector}'.");

            int end = css.IndexOf('}', start);
            Assert.True(end > start, $"The shipped CSS rule for '{selector}' is incomplete.");
            Assert.Contains("display: none", css[start..end], StringComparison.Ordinal);
        }

        // Deliberately two standalone rules: a selector list is dropped as a whole
        // when any one selector fails to parse, so grouping them would mean an
        // engine supporting only one of the two silently loses both.
        Assert.DoesNotContain("::-ms-reveal,", css, StringComparison.Ordinal);
        Assert.DoesNotContain("::-ms-clear,", css, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}
