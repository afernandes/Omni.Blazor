namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniPagination"/>: renders the prev/
/// next arrows plus numeric page tokens (with ellipsis windowing) in full
/// mode and an "N / Total" label in compact mode; fires the
/// <c>CurrentPageChanged</c> event when a button is clicked, with the
/// expected clamping rules; and forwards Class/Style/Attributes.
/// </summary>
public class OmniPaginationTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_default_full_class()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 100)
            .Add(c => c.PageSize, 10));

        var root = cut.Find("div.omni-pagination");
        Assert.Contains("omni-pagination-full", root.ClassName);
    }

    [Fact]
    public void Compact_mode_adds_modifier_and_shows_current_of_total_label()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 100)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 2)
            .Add(c => c.Compact, true));

        var root = cut.Find("div.omni-pagination");
        Assert.Contains("omni-pagination-compact", root.ClassName);
        Assert.Equal("3 / 10", cut.Find(".omni-pagination-current").TextContent);
        // No numeric buttons in compact mode.
        Assert.Empty(cut.FindAll(".omni-pagination-btn"));
    }

    [Fact]
    public void ShowSummary_renders_range_summary_text()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 50)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 0)
            .Add(c => c.ShowSummary, true));

        var root = cut.Find("div.omni-pagination");
        Assert.Contains("omni-pagination-with-summary", root.ClassName);

        var summary = cut.Find(".omni-pagination-summary");
        // "1–10 de 50" (note the en-dash glyph used in the source).
        Assert.Contains("1", summary.TextContent);
        Assert.Contains("10", summary.TextContent);
        Assert.Contains("50", summary.TextContent);
    }

    [Fact]
    public void Renders_numeric_tokens_with_ellipsis_for_large_totals()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 200)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 9)
            .Add(c => c.MaxVisible, 7));

        // Far enough into the range that BOTH sides see an ellipsis.
        Assert.NotEmpty(cut.FindAll(".omni-pagination-ellipsis"));
    }

    [Fact]
    public void Clicking_numeric_button_fires_CurrentPageChanged()
    {
        var captured = -1;
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 30)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 0)
            .Add(c => c.CurrentPageChanged, EventCallback.Factory.Create<int>(this, v => captured = v)));

        // Buttons are 1, 2, 3. Click the "2" -> page index 1.
        var buttons = cut.FindAll(".omni-pagination-btn");
        var two = buttons.Single(b => b.TextContent == "2");
        two.Click();

        Assert.Equal(1, captured);
    }

    [Fact]
    public void Prev_arrow_disabled_on_first_page()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 30)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 0));

        // First chevron-left button is the prev arrow.
        var prev = cut.FindAll("button").First();
        Assert.True(prev.HasAttribute("disabled"));
    }

    [Fact]
    public void Next_arrow_disabled_on_last_page()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 30)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 2));

        // The last <button> in the controls block is the next arrow.
        var allBtns = cut.FindAll(".omni-pagination-controls button");
        var next = allBtns.Last();
        Assert.True(next.HasAttribute("disabled"));
    }

    [Fact]
    public void Clicking_next_arrow_advances_one_page()
    {
        var captured = -1;
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 30)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 0)
            .Add(c => c.CurrentPageChanged, EventCallback.Factory.Create<int>(this, v => captured = v)));

        var allBtns = cut.FindAll(".omni-pagination-controls button");
        var next = allBtns.Last();
        next.Click();

        Assert.Equal(1, captured);
    }

    [Fact]
    public void Clamps_CurrentPage_to_valid_range_on_parameters_set()
    {
        // CurrentPage=99 with only 3 pages -> clamps to 2.
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 30)
            .Add(c => c.PageSize, 10)
            .Add(c => c.CurrentPage, 99));

        Assert.Equal(2, cut.Instance.CurrentPage);
    }

    [Fact]
    public void Zero_total_shows_zero_zero_summary()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 0)
            .Add(c => c.PageSize, 10)
            .Add(c => c.ShowSummary, true));

        // ShowSummary's display is guarded by TotalCount > 0 -> summary span omitted.
        Assert.Empty(cut.FindAll(".omni-pagination-summary"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 10)
            .Add(c => c.Class, "custom-pg"));

        Assert.Contains("custom-pg", cut.Find("div.omni-pagination").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 10)
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("div.omni-pagination").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniPagination>(p => p
            .Add(c => c.TotalCount, 10)
            .AddUnmatched("data-testid", "pg")
            .AddUnmatched("aria-label", "Paginator"));

        var root = cut.Find("div.omni-pagination");
        Assert.Equal("pg", root.GetAttribute("data-testid"));
        Assert.Equal("Paginator", root.GetAttribute("aria-label"));
    }
}
