using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniCulturePicker"/>: the cross-cutting
/// Class/Style/Attributes surface, how a culture name resolves to a flag or a code
/// badge, and the fact that choosing reports the choice without persisting anything —
/// where the language is kept is the host's business, not the component's.
/// </summary>
public class OmniCulturePickerTests : TestContextBase
{
    private static readonly IReadOnlyList<OmniCultureOption> Cultures =
    [
        new() { Name = "pt-BR", DisplayName = "Português", Description = "Brasil" },
        new() { Name = "en-US", DisplayName = "English", Description = "United States" },
        new() { Name = "en-XA", DisplayName = "Pseudo", Description = "Expandido" }
    ];

    private IRenderedComponent<OmniCulturePicker> RenderPicker(
        Action<ComponentParameterCollectionBuilder<OmniCulturePicker>>? extra = null,
        string value = "pt-BR")
        => Render<OmniCulturePicker>(p =>
        {
            p.Add(c => c.Cultures, Cultures);
            p.Add(c => c.Value, value);
            extra?.Invoke(p);
        });

    [Fact]
    public void Renders_trigger_with_base_class()
    {
        var cut = RenderPicker();

        var trigger = cut.Find("button.omni-culture-picker");
        Assert.Contains("omni-culture-picker", trigger.ClassName);
        Assert.Equal("listbox", trigger.GetAttribute("aria-haspopup"));
    }

    [Fact]
    public void Appends_consumer_class_to_the_trigger()
    {
        var cut = RenderPicker(p => p.Add(c => c.Class, "minha-classe"));

        Assert.Contains("minha-classe", cut.Find("button.omni-culture-picker").ClassName);
    }

    [Fact]
    public void Forwards_consumer_style_to_the_trigger()
    {
        var cut = RenderPicker(p => p.Add(c => c.Style, "margin-left:8px"));

        Assert.Contains("margin-left:8px", cut.Find("button.omni-culture-picker").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_attributes_onto_the_trigger()
    {
        var cut = RenderPicker(p => p.AddUnmatched("data-testid", "idioma"));

        Assert.Equal("idioma", cut.Find("button.omni-culture-picker").GetAttribute("data-testid"));
    }

    [Fact]
    public void Trigger_shows_the_flag_of_the_current_culture()
    {
        var cut = RenderPicker();

        // A region in the built-in set draws artwork rather than the code badge.
        Assert.NotEmpty(cut.Find("button.omni-culture-picker .omni-culture-picker-flag").InnerHtml);
    }

    [Fact]
    public void A_culture_without_a_known_region_falls_back_to_a_code_badge()
    {
        // en-XA is a pseudo-locale: XA is not a country, so no flag may be borrowed for it.
        var cut = RenderPicker(value: "en-XA");

        Assert.Equal("XA", cut.Find("button.omni-culture-picker .omni-culture-picker-code").TextContent.Trim());
        Assert.Empty(cut.FindAll("button.omni-culture-picker .omni-culture-picker-flag"));
    }

    [Fact]
    public void ShowFlags_false_uses_code_badges_everywhere()
    {
        // Recommended i18n practice when a language does not belong to one country.
        var cut = RenderPicker(p => p.Add(c => c.ShowFlags, false));

        Assert.Empty(cut.FindAll(".omni-culture-picker-flag"));
        Assert.Equal("BR", cut.Find("button.omni-culture-picker .omni-culture-picker-code").TextContent.Trim());
    }

    [Fact]
    public void FlagTemplate_replaces_the_built_in_artwork()
    {
        RenderFragment<OmniCultureOption> template = culture => builder =>
            builder.AddMarkupContent(0, $"<i class='custom'>{culture.Name}</i>");

        var cut = RenderPicker(p => p.Add(c => c.FlagTemplate, template));

        Assert.Empty(cut.FindAll(".omni-culture-picker-flag"));
        Assert.Equal("pt-BR", cut.Find("button.omni-culture-picker i.custom").TextContent);
    }

    [Fact]
    public void ShowLabel_puts_the_current_language_next_to_the_flag()
    {
        var cut = RenderPicker(p => p.Add(c => c.ShowLabel, true));

        Assert.Equal("Português", cut.Find(".omni-culture-picker-label").TextContent.Trim());
    }

    [Fact]
    public void Trigger_is_disabled_when_asked()
    {
        var cut = RenderPicker(p => p.Add(c => c.Disabled, true));

        Assert.True(cut.Find("button.omni-culture-picker").HasAttribute("disabled"));
    }

    [Fact]
    public void Choosing_a_culture_reports_it()
    {
        string? chosen = null;
        var cut = RenderPicker(p => p.Add(
            c => c.ValueChanged,
            EventCallback.Factory.Create<string>(this, name => chosen = name)));

        cut.Find("button.omni-culture-picker").Click();
        cut.FindAll("button.omni-culture-picker-item")[1].Click();

        Assert.Equal("en-US", chosen);
    }

    [Fact]
    public void Choosing_the_current_culture_again_reports_nothing()
    {
        // Re-selecting would otherwise cost a reload in every host that persists on change.
        bool raised = false;
        var cut = RenderPicker(p => p.Add(
            c => c.ValueChanged,
            EventCallback.Factory.Create<string>(this, _ => raised = true)));

        cut.Find("button.omni-culture-picker").Click();
        cut.FindAll("button.omni-culture-picker-item")[0].Click();

        Assert.False(raised);
    }

    [Fact]
    public void The_open_list_marks_the_current_culture_for_assistive_tech()
    {
        var cut = RenderPicker();

        cut.Find("button.omni-culture-picker").Click();

        var items = cut.FindAll("button.omni-culture-picker-item");
        Assert.Equal(3, items.Count);
        Assert.Equal("true", items[0].GetAttribute("aria-selected"));
        Assert.Equal("false", items[1].GetAttribute("aria-selected"));
        Assert.Contains("is-active", items[0].ClassName);
    }

    [Fact]
    public void An_option_falls_back_to_the_culture_for_its_own_labels()
    {
        var cut = Render<OmniCulturePicker>(p => p
            .Add(c => c.Cultures, new List<OmniCultureOption> { new() { Name = "fr-FR" } })
            .Add(c => c.Value, "fr-FR")
            .Add(c => c.ShowLabel, true));

        // Not supplied by the consumer, so it comes from CultureInfo — and in French,
        // which is how a French reader finds their language in a list.
        Assert.Contains("français", cut.Find(".omni-culture-picker-label").TextContent, StringComparison.OrdinalIgnoreCase);
    }
}
