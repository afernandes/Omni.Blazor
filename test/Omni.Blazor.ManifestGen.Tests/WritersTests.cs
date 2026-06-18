using Omni.Blazor.ManifestGen;
using Xunit;

namespace Omni.Blazor.ManifestGen.Tests;

public class WritersTests
{
    private static ComponentInfo Button() => new(
        "OmniButton", "Buttons", "OmniComponentWithChildren", IsInput: false, HasChildContent: true,
        Summary: "Clickable button.", Source: "src/Omni.Blazor/Components/Buttons/OmniButton.razor",
        Parameters:
        [
            new("Variant", "parameter", "ButtonVariant", null,
                [new EnumVal("Default", null), new EnumVal("Primary", "High emphasis")], "Default", false, "Visual variant.", null),
            new("OnClick", "event", "EventCallback<MouseEventArgs>", null, null, null, false, "Click handler.", null),
            new("ChildContent", "slot", "RenderFragment", null, null, null, false, null, "OmniComponentWithChildren"),
        ]);

    private static ComponentInfo Card() => new(
        "OmniCard", "Display", "OmniComponent", false, false, null,
        "src/Omni.Blazor/Components/Display/OmniCard.razor", []);

    [Fact]
    public void LlmsIndex_groups_and_links()
    {
        string s = Writers.LlmsIndex([Button(), Card()]);
        Assert.Contains("# Omni.Blazor", s);
        Assert.Contains("## Buttons", s);
        Assert.Contains("- [OmniButton](https://github.com/afernandes/Omni.Blazor/blob/main/src/Omni.Blazor/Components/Buttons/OmniButton.razor): Clickable button.", s);
        Assert.Contains("## Display", s);
        Assert.Contains("- [OmniCard](https://github.com/afernandes/Omni.Blazor/blob/main/src/Omni.Blazor/Components/Display/OmniCard.razor)", s);
        Assert.DoesNotContain("OmniCard.razor):", s); // no summary → no ": " suffix
        Assert.Contains("## Optional", s);
    }

    [Fact]
    public void LlmsFull_renders_sections_and_tokens()
    {
        string s = Writers.LlmsFull([Button()], ["--omni-bg", "--omni-fg"]);
        Assert.Contains("### OmniButton", s);
        Assert.Contains("Clickable button.", s);
        Assert.Contains("Parameters:", s);
        Assert.Contains("- `Variant`: ButtonVariant {Default | Primary} = Default — Visual variant.", s);
        Assert.Contains("Events:", s);
        Assert.Contains("- `OnClick`: EventCallback<MouseEventArgs> — Click handler.", s);
        // ChildContent is inherited → filtered out of the Slots section
        Assert.DoesNotContain("Slots:", s);
        Assert.Contains("## Theme tokens", s);
        Assert.Contains("- `--omni-bg`", s);
    }

    [Fact]
    public void LlmsFull_required_marker()
    {
        var c = new ComponentInfo("OmniAlert", "Display", "OmniComponent", false, false, "Alert.",
            "src/x.razor", [new("Severity", "parameter", "NotificationSeverity", null, null, null, Required: true, "Sev.", null)]);
        Assert.Contains("*required*", Writers.LlmsFull([c], []));
    }
}
