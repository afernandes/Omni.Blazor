using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniHtmlEditor"/>: structure (toolbar +
/// contenteditable), default vs custom toolbar, disabled state, the command
/// state → button highlight, and the content-change → Value/Input commit paths.
/// JS interop runs in bUnit Loose mode (no-op), so command execution is checked
/// at the C# boundary.
/// </summary>
public class OmniHtmlEditorTests : TestContextBase
{
    private IRenderedComponent<OmniHtmlEditor> Render(
        Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>>? extra = null)
        => RenderComponent<OmniHtmlEditor>(p => extra?.Invoke(p));

    // ─── Structure ────────────────────────────────────────────────────────

    [Fact]
    public void Renders_root_toolbar_and_contenteditable()
    {
        var cut = Render();
        Assert.NotNull(cut.Find("div.omni-he"));
        Assert.NotNull(cut.Find(".omni-he-toolbar"));
        var content = cut.Find(".omni-he-content");
        Assert.Equal("true", content.GetAttribute("contenteditable"));
    }

    [Fact]
    public void Default_toolbar_renders_standard_tools()
    {
        var cut = Render();
        // Default set includes bold/italic/underline/strike + aligns + lists etc.
        Assert.True(cut.FindAll(".omni-he-btn").Count >= 10);
        Assert.NotNull(cut.Find(".omni-he-select"));   // format-block dropdown
        Assert.NotNull(cut.Find("input[type=color]")); // text color
    }

    [Fact]
    public void ShowToolbar_false_hides_toolbar()
    {
        var cut = Render(p => p.Add(c => c.ShowToolbar, false));
        Assert.Empty(cut.FindAll(".omni-he-toolbar"));
        Assert.NotNull(cut.Find(".omni-he-content"));
    }

    [Fact]
    public void Disabled_makes_content_non_editable()
    {
        var cut = Render(p => p.Add(c => c.Disabled, true));
        Assert.Equal("false", cut.Find(".omni-he-content").GetAttribute("contenteditable"));
        Assert.Contains("omni-he-disabled", cut.Find("div.omni-he").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_and_splats_attributes()
    {
        var cut = Render(p => p.Add(c => c.Class, "doc").AddUnmatched("data-testid", "he1"));
        var root = cut.Find("div.omni-he");
        Assert.Contains("doc", root.ClassName);
        Assert.Equal("he1", root.GetAttribute("data-testid"));
    }

    [Fact]
    public void Applies_height_css_var()
    {
        var cut = Render(p => p.Add(c => c.Height, "500px"));
        Assert.Contains("--omni-he-height:500px", cut.Find("div.omni-he").GetAttribute("style") ?? "");
    }

    // ─── Custom toolbar buttons ───────────────────────────────────────────

    [Fact]
    public void Custom_toolbar_button_renders_and_reflects_active_state()
    {
        RenderFragment toolbar = b =>
        {
            b.OpenComponent<OmniHtmlEditorButton>(0);
            b.AddAttribute(1, nameof(OmniHtmlEditorButton.Command), "bold");
            b.AddAttribute(2, nameof(OmniHtmlEditorButton.Icon), "bold");
            b.CloseComponent();
        };
        var cut = Render(p => p.Add(c => c.ChildContent, toolbar));
        var btn = cut.Find(".omni-he-btn");
        Assert.DoesNotContain("omni-active", btn.ClassName);

        // Simulate the JS selection-change pushing a state where bold is active.
        cut.InvokeAsync(() => cut.Instance.OnSelectionChanged(new OmniHtmlEditorCommandState { Bold = true }));
        Assert.Contains("omni-active", cut.Find(".omni-he-btn").ClassName);
    }

    // ─── Commit paths ─────────────────────────────────────────────────────

    [Fact]
    public async Task OnContentChanged_fires_Input()
    {
        string? captured = null;
        var cut = Render(p => p.Add(c => c.Input,
            EventCallback.Factory.Create<string>(this, h => captured = h)));

        await cut.InvokeAsync(() => cut.Instance.OnContentChanged("<p>hi</p>"));
        Assert.Equal("<p>hi</p>", captured);
    }

    [Fact]
    public async Task Commits_Value_on_blur_after_edit()
    {
        string? value = null;
        var cut = Render(p => p.Add(c => c.ValueChanged,
            EventCallback.Factory.Create<string?>(this, v => value = v)));

        await cut.InvokeAsync(() => cut.Instance.OnContentChanged("<p>edited</p>"));
        Assert.Null(value); // not committed yet (waits for blur)

        cut.Find(".omni-he-content").Blur();
        Assert.Equal("<p>edited</p>", value);
    }

    [Fact]
    public void Selection_change_updates_State()
    {
        var cut = Render();
        cut.InvokeAsync(() => cut.Instance.OnSelectionChanged(new OmniHtmlEditorCommandState { Italic = true, FormatBlock = "h2" }));
        Assert.True(cut.Instance.State.Italic);
        Assert.Equal("h2", cut.Instance.State.FormatBlock);
        Assert.True(cut.Instance.State.IsActive("italic"));
        Assert.False(cut.Instance.State.IsActive("bold"));
    }
}
