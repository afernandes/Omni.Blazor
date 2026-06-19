namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniBreadcrumb"/>: renders a
/// <c>&lt;nav&gt;</c> with a <c>/</c> separator between items, marks the
/// last item as <c>omni-breadcrumb-current</c>, renders linked entries as
/// <c>&lt;a&gt;</c>, and forwards Class/Style/Attributes.
/// </summary>
public class OmniBreadcrumbTests : TestContextBase
{
    [Fact]
    public void Renders_nav_with_breadcrumb_class()
    {
        var cut = Render<OmniBreadcrumb>();

        var nav = cut.Find("nav");
        Assert.Contains("omni-breadcrumb", nav.ClassName);
    }

    [Fact]
    public void Renders_items_with_separators_and_last_as_current()
    {
        var items = new[]
        {
            new OmniBreadcrumb.Item("Home", "/"),
            new OmniBreadcrumb.Item("Catalog", "/catalog"),
            new OmniBreadcrumb.Item("Pizza"),
        };
        var cut = Render<OmniBreadcrumb>(p => p.Add(c => c.Items, items));

        // Two anchors (Home, Catalog) + one current span (Pizza).
        var anchors = cut.FindAll("a");
        Assert.Equal(2, anchors.Count);
        Assert.Equal("Home", anchors[0].TextContent);
        Assert.Equal("/", anchors[0].GetAttribute("href"));
        Assert.Equal("Catalog", anchors[1].TextContent);
        Assert.Equal("/catalog", anchors[1].GetAttribute("href"));

        var current = cut.Find(".omni-breadcrumb-current");
        Assert.Equal("Pizza", current.TextContent);

        // Two separators between three items.
        Assert.Equal(2, cut.FindAll(".omni-breadcrumb-sep").Count);
    }

    [Fact]
    public void Item_without_path_renders_as_current_span_even_when_not_last()
    {
        var items = new[]
        {
            new OmniBreadcrumb.Item("Home"),       // no path -> current span
            new OmniBreadcrumb.Item("End", "/e"),
        };
        var cut = Render<OmniBreadcrumb>(p => p.Add(c => c.Items, items));

        // The "End" item is last -> current span. "Home" has no path -> also current span.
        Assert.Equal(2, cut.FindAll(".omni-breadcrumb-current").Count);
        Assert.Empty(cut.FindAll("a"));
    }

    [Fact]
    public void Null_items_renders_empty_nav()
    {
        var cut = Render<OmniBreadcrumb>(p => p.Add(c => c.Items, (IEnumerable<OmniBreadcrumb.Item>?)null));

        var nav = cut.Find("nav");
        Assert.Contains("omni-breadcrumb", nav.ClassName);
        Assert.Empty(cut.FindAll("a"));
        Assert.Empty(cut.FindAll(".omni-breadcrumb-current"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniBreadcrumb>(p => p.Add(c => c.Class, "custom-bc"));

        Assert.Contains("custom-bc", cut.Find("nav").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniBreadcrumb>(p => p.Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("nav").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniBreadcrumb>(p => p
            .AddUnmatched("data-testid", "bc")
            .AddUnmatched("aria-label", "Trail"));

        var nav = cut.Find("nav");
        Assert.Equal("bc", nav.GetAttribute("data-testid"));
        Assert.Equal("Trail", nav.GetAttribute("aria-label"));
    }
}
