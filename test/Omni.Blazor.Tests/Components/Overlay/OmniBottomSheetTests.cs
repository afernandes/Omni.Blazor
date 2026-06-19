namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniBottomSheet"/>: only renders when
/// <c>Open=true</c>; in mobile/default mode emits the sheet with modal/standard
/// modifier + optional drag handle. Adaptive desktop mode delegates to
/// dialog/drawer fallback markup. The component also exposes
/// <c>OpenAsync</c>/<c>CloseAsync</c> as imperative APIs.
/// </summary>
public class OmniBottomSheetTests : TestContextBase
{
    [Fact]
    public void Hidden_when_Open_false()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, false)
            .AddChildContent("body"));

        Assert.Empty(cut.FindAll(".omni-bs-sheet"));
        Assert.Empty(cut.FindAll(".omni-bs-backdrop"));
    }

    [Fact]
    public void Renders_sheet_with_modal_modifier_by_default_when_open()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .AddChildContent("body"));

        var sheet = cut.Find(".omni-bs-sheet");
        Assert.Contains("omni-bs-modal", sheet.ClassName);
        Assert.Equal("dialog", sheet.GetAttribute("role"));
        Assert.Equal("true", sheet.GetAttribute("aria-modal"));
    }

    [Fact]
    public void Modal_variant_renders_backdrop()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Variant, BottomSheetVariant.Modal)
            .AddChildContent("body"));

        Assert.NotNull(cut.Find(".omni-bs-backdrop"));
    }

    [Fact]
    public void Standard_variant_skips_backdrop_and_marks_modifier()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Variant, BottomSheetVariant.Standard)
            .AddChildContent("body"));

        Assert.Empty(cut.FindAll(".omni-bs-backdrop"));
        var sheet = cut.Find(".omni-bs-sheet");
        Assert.Contains("omni-bs-standard", sheet.ClassName);
        Assert.Equal("false", sheet.GetAttribute("aria-modal"));
    }

    [Fact]
    public void Default_shows_drag_handle()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .AddChildContent("body"));

        Assert.NotNull(cut.Find(".omni-bs-handle"));
        Assert.DoesNotContain("omni-bs-no-handle", cut.Find(".omni-bs-sheet").ClassName);
    }

    [Fact]
    public void ShowDragHandle_false_omits_handle_and_marks_modifier()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.ShowDragHandle, false)
            .AddChildContent("body"));

        Assert.Empty(cut.FindAll(".omni-bs-handle"));
        Assert.Contains("omni-bs-no-handle", cut.Find(".omni-bs-sheet").ClassName);
    }

    [Fact]
    public void Title_renders_in_header()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Title, "Detalhes")
            .AddChildContent("body"));

        var title = cut.Find(".omni-bs-title");
        Assert.Contains("Detalhes", title.TextContent);
    }

    [Fact]
    public void Footer_renders_when_provided()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Footer, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "probe-footer");
                builder.CloseElement();
            })
            .AddChildContent("body"));

        Assert.NotNull(cut.Find(".omni-bs-footer .probe-footer"));
    }

    [Fact]
    public void ChildContent_renders_inside_body()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .AddChildContent("<span class=\"probe-body\">hello</span>"));

        Assert.NotNull(cut.Find(".omni-bs-body .probe-body"));
    }

    [Fact]
    public async Task CloseAsync_flips_state_and_invokes_OpenChanged()
    {
        var openValue = true;
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, openValue)
            .Add(c => c.OpenChanged, (bool v) => openValue = v)
            .AddChildContent("body"));

        Assert.NotNull(cut.Find(".omni-bs-sheet"));
        await cut.InvokeAsync(() => cut.Instance.CloseAsync());

        Assert.False(openValue);
        Assert.Empty(cut.FindAll(".omni-bs-sheet"));
    }

    [Fact]
    public async Task OpenAsync_flips_state_and_invokes_OpenChanged()
    {
        var openValue = false;
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, openValue)
            .Add(c => c.OpenChanged, (bool v) => openValue = v)
            .AddChildContent("body"));

        Assert.Empty(cut.FindAll(".omni-bs-sheet"));
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        Assert.True(openValue);
        Assert.NotNull(cut.Find(".omni-bs-sheet"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.Class, "custom-bs")
            .AddChildContent("body"));

        Assert.Contains("custom-bs", cut.Find(".omni-bs-sheet").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniBottomSheet>(p => p
            .Add(c => c.Open, true)
            .AddUnmatched("data-testid", "bs")
            .AddChildContent("body"));

        Assert.Equal("bs", cut.Find(".omni-bs-sheet").GetAttribute("data-testid"));
    }
}
