using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniSecurityCode"/>: cell count, per-cell
/// attributes (type/inputmode/maxlength), disabled, the JS→.NET value commit, and
/// the cross-cutting splat. Focus advance/paste is JS (Loose mode) — verified at
/// the C# boundary.
/// </summary>
public class OmniSecurityCodeTests : TestContextBase
{
    private IRenderedComponent<OmniSecurityCode> Render(
        Action<ComponentParameterCollectionBuilder<OmniSecurityCode>>? extra = null)
        => RenderComponent<OmniSecurityCode>(p => extra?.Invoke(p));

    [Fact]
    public void Renders_default_four_cells()
    {
        var cut = Render();
        Assert.Equal(4, cut.FindAll(".omni-seccode-input").Count);
        Assert.NotNull(cut.Find("div.omni-seccode"));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public void Renders_custom_cell_count(int count)
    {
        var cut = Render(p => p.Add(c => c.Count, count));
        Assert.Equal(count, cut.FindAll(".omni-seccode-input").Count);
    }

    [Fact]
    public void Cells_have_maxlength_one_and_otp_autocomplete()
    {
        var cut = Render();
        var cell = cut.Find(".omni-seccode-input");
        Assert.Equal("1", cell.GetAttribute("maxlength"));
        Assert.Equal("one-time-code", cell.GetAttribute("autocomplete"));
    }

    [Theory]
    [InlineData(SecurityCodeType.Text, "text", null)]
    [InlineData(SecurityCodeType.Numeric, "text", "numeric")]
    [InlineData(SecurityCodeType.Password, "password", null)]
    public void Type_sets_input_type_and_inputmode(SecurityCodeType type, string expectedType, string? expectedInputMode)
    {
        var cut = Render(p => p.Add(c => c.Type, type));
        var cell = cut.Find(".omni-seccode-input");
        Assert.Equal(expectedType, cell.GetAttribute("type"));
        Assert.Equal(expectedInputMode, cell.GetAttribute("inputmode"));
    }

    [Fact]
    public void Disabled_disables_all_cells()
    {
        var cut = Render(p => p.Add(c => c.Disabled, true));
        Assert.All(cut.FindAll(".omni-seccode-input"), c => Assert.NotNull(c.GetAttribute("disabled")));
        Assert.Contains("omni-seccode-disabled", cut.Find("div.omni-seccode").ClassName);
    }

    [Fact]
    public void Cells_have_aria_labels()
    {
        var cut = Render(p => p.Add(c => c.AriaLabel, "Dígito"));
        var cells = cut.FindAll(".omni-seccode-input");
        Assert.Equal("Dígito 1", cells[0].GetAttribute("aria-label"));
        Assert.Equal("Dígito 4", cells[3].GetAttribute("aria-label"));
    }

    [Fact]
    public void Applies_gap_css_var()
    {
        var cut = Render(p => p.Add(c => c.Gap, "12px"));
        Assert.Contains("--omni-seccode-gap:12px", cut.Find("div.omni-seccode").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_Class_and_splats_attributes()
    {
        var cut = Render(p => p.Add(c => c.Class, "otp").AddUnmatched("data-testid", "sc1"));
        var root = cut.Find("div.omni-seccode");
        Assert.Contains("otp", root.ClassName);
        Assert.Equal("sc1", root.GetAttribute("data-testid"));
    }

    [Fact]
    public async Task OnCodeChanged_commits_value()
    {
        string? value = null;
        var cut = Render(p => p
            .Add(c => c.Count, 4)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v)));

        await cut.InvokeAsync(() => cut.Instance.OnCodeChanged("1234"));
        Assert.Equal("1234", value);
    }
}
