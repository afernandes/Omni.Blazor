using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor;
using Omni.Blazor.Localization;

namespace Omni.Blazor.Tests.Localization;

/// <summary>
/// The localization seam: components fall back to <see cref="OmniTexts"/> for the strings
/// they render themselves. Registering a set translates the whole library at once; passing
/// nothing must keep the historical pt-BR output byte-for-byte.
/// </summary>
public class OmniTextsTests : TestContextBase
{
    // ── The text set itself ───────────────────────────────────────────────

    [Fact]
    public void Default_is_pt_br()
    {
        Assert.Equal("Fechar", OmniTexts.Default.Close);
        Assert.Equal("Limpar", OmniTexts.Default.Clear);
        Assert.Equal("Ações", OmniTexts.Default.Actions);
    }

    [Fact]
    public void English_translates_every_key()
    {
        var en = OmniTexts.English();
        var pt = OmniTexts.Default;

        foreach (var p in typeof(OmniTexts).GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            var enValue = (string?)p.GetValue(en);
            var ptValue = (string?)p.GetValue(pt);
            Assert.False(string.IsNullOrWhiteSpace(enValue), $"{p.Name} has no English value");
            Assert.True(enValue != ptValue, $"{p.Name} was not translated (still '{ptValue}')");
        }
    }

    [Fact]
    public void English_returns_a_fresh_instance_each_call()
    {
        var a = OmniTexts.English();
        a.Close = "mutated";
        Assert.Equal("Close", OmniTexts.English().Close); // not shared state
    }

    // ── DI registration ───────────────────────────────────────────────────

    [Fact]
    public void AddOmniComponents_registers_the_default_texts()
    {
        var services = new ServiceCollection();
        services.AddOmniComponents();
        using var provider = services.BuildServiceProvider();

        Assert.Same(OmniTexts.Default, provider.GetService<OmniTexts>());
    }

    [Fact]
    public void AddOmniComponents_registers_the_configured_texts()
    {
        var en = OmniTexts.English();
        var services = new ServiceCollection();
        services.AddOmniComponents(o => o.Texts = en);
        using var provider = services.BuildServiceProvider();

        Assert.Same(en, provider.GetService<OmniTexts>());
    }

    [Fact]
    public void A_previously_registered_OmniTexts_wins()
    {
        // e.g. a consumer registering a scoped set fed by their own IStringLocalizer
        var mine = new OmniTexts { Close = "Dismiss" };
        var services = new ServiceCollection();
        services.AddSingleton(mine);
        services.AddOmniComponents(o => o.Texts = OmniTexts.English());
        using var provider = services.BuildServiceProvider();

        Assert.Same(mine, provider.GetService<OmniTexts>());
    }

    // ── Components actually use it ────────────────────────────────────────

    [Fact]
    public void Component_uses_the_registered_texts()
    {
        Services.AddSingleton(OmniTexts.English());

        var cut = Render<OmniAlert>(p => p.Add(c => c.Dismissible, true));

        Assert.Equal("Close", cut.Find(".omni-alert-close").GetAttribute("aria-label"));
    }

    [Fact]
    public void Component_falls_back_to_pt_br_when_nothing_is_registered()
    {
        // Historical behaviour must be untouched for apps that never configure texts.
        var cut = Render<OmniAlert>(p => p.Add(c => c.Dismissible, true));

        Assert.Equal("Fechar", cut.Find(".omni-alert-close").GetAttribute("aria-label"));
    }

    // ── [Parameter] defaults now come from the seam too ───────────────────

    [Fact]
    public void Parameter_default_follows_the_registered_texts()
    {
        Services.AddSingleton(OmniTexts.English());

        var cut = Render<OmniLayout>(p => p.Add(c => c.SkipTarget, "#content"));

        Assert.Contains("Skip to content", cut.Find("a.omni-skip-link").TextContent);
    }

    [Fact]
    public void Parameter_default_is_pt_br_without_registration()
    {
        var cut = Render<OmniLayout>(p => p.Add(c => c.SkipTarget, "#content"));

        Assert.Contains("Pular para o conteúdo", cut.Find("a.omni-skip-link").TextContent);
    }

    [Fact]
    public void An_explicit_parameter_still_wins_over_the_registered_texts()
    {
        Services.AddSingleton(OmniTexts.English());

        var cut = Render<OmniLayout>(p => p
            .Add(c => c.SkipLabel, "Ir para o conteúdo")
            .Add(c => c.SkipTarget, "#content"));

        Assert.Contains("Ir para o conteúdo", cut.Find("a.omni-skip-link").TextContent);
    }
}
