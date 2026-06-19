using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniPasswordStrength"/>: configurable rule
/// set (Identity-style toggles + custom Rules), scoring, ParameterState recompute
/// discipline, and the cross-cutting splat.
/// </summary>
public class OmniPasswordStrengthTests : TestContextBase
{
    [Fact]
    public void Renders_four_segments_and_five_default_rules()
    {
        var cut = Render<OmniPasswordStrength>();
        Assert.Equal(4, cut.FindAll(".omni-pwstrength-seg").Count);
        // length + uppercase + lowercase + digit + symbol
        Assert.Equal(5, cut.FindAll(".omni-pwstrength-rule").Count);
    }

    [Fact]
    public void Empty_password_fills_no_segments()
    {
        var cut = Render<OmniPasswordStrength>(p => p.Add(c => c.Password, ""));
        Assert.Empty(cut.FindAll(".omni-pwstrength-seg-weak, .omni-pwstrength-seg-fair, .omni-pwstrength-seg-good, .omni-pwstrength-seg-strong"));
    }

    [Fact]
    public void All_rules_met_fills_all_four_strong_segments()
    {
        var cut = Render<OmniPasswordStrength>(p => p
            .Add(c => c.Password, "Abcdef123!@#")
            .Add(c => c.MinLength, 8));
        Assert.Equal(4, cut.FindAll(".omni-pwstrength-seg-strong").Count);
        Assert.Equal(5, cut.FindAll(".omni-pwstrength-rule-met").Count);
    }

    [Fact]
    public void Weak_password_marks_only_satisfied_rules()
    {
        // "abc": only the lowercase rule passes (length<8, no upper/digit/symbol)
        var cut = Render<OmniPasswordStrength>(p => p
            .Add(c => c.Password, "abc")
            .Add(c => c.MinLength, 8));
        Assert.Single(cut.FindAll(".omni-pwstrength-rule-met"));
    }

    [Fact]
    public void Disabling_a_toggle_drops_that_rule()
    {
        var cut = Render<OmniPasswordStrength>(p => p
            .Add(c => c.RequireSymbol, false)
            .Add(c => c.RequireDigit, false));
        // length + uppercase + lowercase = 3 rules
        Assert.Equal(3, cut.FindAll(".omni-pwstrength-rule").Count);
        Assert.DoesNotContain("símbolo", cut.Markup);
        Assert.DoesNotContain("número", cut.Markup);
    }

    [Fact]
    public void RequiredUniqueChars_adds_a_rule()
    {
        var cut = Render<OmniPasswordStrength>(p => p.Add(c => c.RequiredUniqueChars, 4));
        Assert.Equal(6, cut.FindAll(".omni-pwstrength-rule").Count);
        Assert.Contains("distintos", cut.Markup);
    }

    [Fact]
    public void MinLength_zero_drops_the_length_rule()
    {
        var cut = Render<OmniPasswordStrength>(p => p.Add(c => c.MinLength, 0));
        // uppercase + lowercase + digit + symbol = 4 rules (no length)
        Assert.Equal(4, cut.FindAll(".omni-pwstrength-rule").Count);
        Assert.DoesNotContain("caracteres", cut.Markup);
    }

    [Fact]
    public void Custom_Rules_replace_the_defaults()
    {
        var rules = new[]
        {
            new OmniPasswordRule("Pelo menos 12 caracteres", p => p.Length >= 12),
            new OmniPasswordRule("Não conter 'senha'", p => !p.Contains("senha")),
        };
        var cut = Render<OmniPasswordStrength>(p => p
            .Add(c => c.Rules, rules)
            .Add(c => c.Password, "abcdefghijkl"));   // 12 chars, no 'senha' -> both met
        var ruleEls = cut.FindAll(".omni-pwstrength-rule");
        Assert.Equal(2, ruleEls.Count);
        Assert.Contains("Pelo menos 12 caracteres", cut.Markup);
        Assert.Equal(2, cut.FindAll(".omni-pwstrength-rule-met").Count);
        // all custom rules met -> strong
        Assert.Equal(4, cut.FindAll(".omni-pwstrength-seg-strong").Count);
    }

    [Fact]
    public void ShowRules_false_hides_checklist()
    {
        var cut = Render<OmniPasswordStrength>(p => p
            .Add(c => c.Password, "abc")
            .Add(c => c.ShowRules, false));
        Assert.Empty(cut.FindAll(".omni-pwstrength-rule"));
    }

    [Fact]
    public void Recompute_does_NOT_run_when_only_Class_changes()
    {
        var cut = Render<OmniPasswordStrength>(p => p.Add(c => c.Password, "abc"));
        var before = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.Class, "new"));
        Assert.Equal(before, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_runs_when_Password_or_policy_changes()
    {
        var cut = Render<OmniPasswordStrength>(p => p.Add(c => c.Password, "abc"));
        var before = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.Password, "Abcdef123!"));
        Assert.True(cut.Instance.RecomputeCount > before);

        var afterPw = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.RequireSymbol, false));
        Assert.True(cut.Instance.RecomputeCount > afterPw);
    }

    [Fact]
    public void Appends_Class_Style_and_splats_attributes()
    {
        var cut = Render<OmniPasswordStrength>(p => p
            .Add(c => c.Class, "x")
            .Add(c => c.Style, "margin:4px")
            .AddUnmatched("data-testid", "p1"));
        var root = cut.Find(".omni-pwstrength");
        Assert.Contains("x", root.ClassName);
        Assert.Contains("margin:4px", root.GetAttribute("style") ?? "");
        Assert.Equal("p1", root.GetAttribute("data-testid"));
    }
}
