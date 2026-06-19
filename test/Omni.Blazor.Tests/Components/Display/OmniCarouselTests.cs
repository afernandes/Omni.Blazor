using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniCarousel"/> + <see cref="OmniCarouselItem"/>:
/// slide rendering, pager dots, prev/next navigation, two-way SelectedIndex, the
/// AllowPaging/AllowNavigation/PagerPosition/PagerOverlay toggles, ItemsPerPage
/// sizing, and the cross-cutting splat. Auto-play is disabled so the background
/// timer never fires mid-test.
/// </summary>
public class OmniCarouselTests : TestContextBase
{
    private IRenderedComponent<OmniCarousel> Render(
        int items = 3,
        Action<ComponentParameterCollectionBuilder<OmniCarousel>>? extra = null)
        => Render<OmniCarousel>(p =>
        {
            p.Add(c => c.Auto, false);
            for (int i = 0; i < items; i++)
            {
                var label = $"Slide {i + 1}";
                p.AddChildContent<OmniCarouselItem>(it => it.AddChildContent(label));
            }
            extra?.Invoke(p);
        });

    [Fact]
    public void Renders_section_with_carousel_class_and_items_track()
    {
        var cut = Render();
        Assert.NotNull(cut.Find("section.omni-carousel"));
        Assert.NotNull(cut.Find("ul.omni-carousel-items"));
    }

    [Fact]
    public void Renders_one_item_per_child_with_snapper()
    {
        var cut = Render(items: 4);
        var slides = cut.FindAll("li.omni-carousel-item");
        Assert.Equal(4, slides.Count);
        Assert.Equal(4, cut.FindAll(".omni-carousel-snapper").Count);
        Assert.Contains("Slide 1", slides[0].TextContent);
    }

    [Fact]
    public void Renders_one_pager_dot_per_slide()
    {
        var cut = Render(items: 3);
        Assert.Equal(3, cut.FindAll(".omni-carousel-pager-button").Count);
    }

    [Fact]
    public void First_dot_is_active_by_default()
    {
        var cut = Render();
        var dots = cut.FindAll(".omni-carousel-pager-button");
        Assert.Contains("omni-active", dots[0].ClassName);
        Assert.DoesNotContain("omni-active", dots[1].ClassName);
    }

    [Fact]
    public void Clicking_dot_navigates_and_raises_callbacks()
    {
        int? changed = null, general = null;
        var cut = Render(items: 3, extra: p => p
            .Add(c => c.SelectedIndexChanged, EventCallback.Factory.Create<int>(this, v => changed = v))
            .Add(c => c.Change, EventCallback.Factory.Create<int>(this, v => general = v)));

        cut.FindAll(".omni-carousel-pager-button")[2].Click();

        Assert.Equal(2, changed);
        Assert.Equal(2, general);
        Assert.Contains("omni-active", cut.FindAll(".omni-carousel-pager-button")[2].ClassName);
    }

    [Fact]
    public void Next_button_advances_selected_index()
    {
        int? changed = null;
        var cut = Render(items: 3, extra: p => p
            .Add(c => c.SelectedIndexChanged, EventCallback.Factory.Create<int>(this, v => changed = v)));

        cut.Find(".omni-carousel-next").Click();
        Assert.Equal(1, changed);
    }

    [Fact]
    public void Prev_button_wraps_to_last_slide()
    {
        int? changed = null;
        var cut = Render(items: 3, extra: p => p
            .Add(c => c.SelectedIndexChanged, EventCallback.Factory.Create<int>(this, v => changed = v)));

        cut.Find(".omni-carousel-prev").Click();
        Assert.Equal(2, changed);
    }

    [Fact]
    public void AllowNavigation_false_hides_arrows()
    {
        var cut = Render(extra: p => p.Add(c => c.AllowNavigation, false));
        Assert.Empty(cut.FindAll(".omni-carousel-prev"));
        Assert.Empty(cut.FindAll(".omni-carousel-next"));
        Assert.Contains("omni-carousel-no-navigation", cut.Find("section.omni-carousel").ClassName);
    }

    [Fact]
    public void AllowPaging_false_hides_dots()
    {
        var cut = Render(extra: p => p.Add(c => c.AllowPaging, false));
        Assert.Empty(cut.FindAll(".omni-carousel-pager-button"));
    }

    [Fact]
    public void PagerPosition_Top_renders_top_pager()
    {
        var top = Render(extra: p => p.Add(c => c.PagerPosition, CarouselPagerPosition.Top));
        Assert.NotNull(top.Find(".omni-carousel-pager-top"));
        Assert.Empty(top.FindAll(".omni-carousel-pager-bottom"));

        var bottom = Render();
        Assert.NotNull(bottom.Find(".omni-carousel-pager-bottom"));
    }

    [Fact]
    public void PagerPosition_TopAndBottom_renders_both_sets_of_dots()
    {
        var cut = Render(items: 3, extra: p => p.Add(c => c.PagerPosition, CarouselPagerPosition.TopAndBottom));
        Assert.Equal(6, cut.FindAll(".omni-carousel-pager-button").Count);
    }

    [Fact]
    public void PagerOverlay_toggles_root_class()
    {
        Assert.Contains("omni-carousel-pager-overlay", Render().Find("section.omni-carousel").ClassName);
        Assert.DoesNotContain("omni-carousel-pager-overlay",
            Render(extra: p => p.Add(c => c.PagerOverlay, false)).Find("section.omni-carousel").ClassName);
    }

    [Fact]
    public void ItemsPerPage_sets_fractional_item_width_and_page_count()
    {
        var cut = Render(items: 4, extra: p => p.Add(c => c.ItemsPerPage, 2));
        var style = cut.FindAll("li.omni-carousel-item")[0].GetAttribute("style") ?? "";
        Assert.Contains("calc(100% / 2)", style);
        // 4 items / 2 per page = 2 pages → 2 dots.
        Assert.Equal(2, cut.FindAll(".omni-carousel-pager-button").Count);
    }

    [Fact]
    public void AllowScroll_false_adds_no_scroll_class()
    {
        var cut = Render(extra: p => p.Add(c => c.AllowScroll, false));
        Assert.Contains("omni-carousel-no-scroll", cut.Find("ul.omni-carousel-items").ClassName);
    }

    [Fact]
    public void SelectedIndex_parameter_sets_active_dot()
    {
        var cut = Render(items: 3, extra: p => p.Add(c => c.SelectedIndex, 1));
        Assert.Contains("omni-active", cut.FindAll(".omni-carousel-pager-button")[1].ClassName);
    }

    [Fact]
    public void Empty_carousel_renders_section_without_items_or_dots()
    {
        var cut = Render(items: 0);
        Assert.NotNull(cut.Find("section.omni-carousel"));
        Assert.Empty(cut.FindAll("li.omni-carousel-item"));
        Assert.Empty(cut.FindAll(".omni-carousel-pager-button"));
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render(extra: p => p
            .Add(c => c.Class, "hero-carousel")
            .Add(c => c.Style, "height:300px")
            .AddUnmatched("data-testid", "carousel1"));
        var section = cut.Find("section.omni-carousel");
        Assert.Contains("hero-carousel", section.ClassName);
        Assert.Contains("height:300px", section.GetAttribute("style") ?? "");
        Assert.Equal("carousel1", section.GetAttribute("data-testid"));
    }
}
