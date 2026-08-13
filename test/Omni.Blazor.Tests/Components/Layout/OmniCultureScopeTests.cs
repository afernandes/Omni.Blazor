using System.Globalization;
using Omni.Blazor;

namespace Omni.Blazor.Tests.Components.Layout;

public class OmniCultureScopeTests : TestContextBase
{
    [Fact]
    public void Cascades_formatting_and_ui_cultures_and_emits_language_metadata()
    {
        Services.AddOmniComponents();
        CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

        var cut = Render<OmniCultureScope>(parameters => parameters
            .Add(component => component.Culture, culture)
            .Add(component => component.UICulture, culture)
            .AddChildContent<OmniAlert>(alert => alert.Add(component => component.Dismissible, true)));

        var root = cut.Find(".omni-culture-scope");
        Assert.Equal("en-US", root.GetAttribute("lang"));
        Assert.Equal("ltr", root.GetAttribute("dir"));
        Assert.Equal("Close", cut.Find(".omni-alert-close").GetAttribute("aria-label"));
    }

    [Fact]
    public void Infers_rtl_direction()
    {
        var cut = Render<OmniCultureScope>(parameters => parameters
            .Add(component => component.UICulture, CultureInfo.GetCultureInfo("ar-SA"))
            .AddChildContent("<span>مرحبا</span>"));

        Assert.Equal("rtl", cut.Find(".omni-culture-scope").GetAttribute("dir"));
    }
}
