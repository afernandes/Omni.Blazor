using Omni.Blazor.Components;
using Omni.Blazor.ManifestGen;
using Xunit;

namespace Omni.Blazor.ManifestGen.Tests;

public class ManifestBuilderTests
{
    private static readonly System.Reflection.Assembly Lib = typeof(OmniComponent).Assembly;

    // Only types with a source entry are treated as components — so we drive Build
    // with a controlled two-component map and assert the reflected shape.
    private static List<ComponentInfo> Build(
        Dictionary<string, string>? docs = null,
        Dictionary<string, string>? desc = null)
    {
        var cats = new Dictionary<string, string> { ["OmniButton"] = "Buttons", ["OmniTextBox"] = "Inputs" };
        var src = new Dictionary<string, string>
        {
            ["OmniButton"] = "src/Omni.Blazor/Components/Buttons/OmniButton.razor",
            ["OmniTextBox"] = "src/Omni.Blazor/Components/Inputs/OmniTextBox.razor",
        };
        return ManifestBuilder.Build(Lib, docs ?? [], cats, src, desc ?? []);
    }

    [Fact]
    public void Build_includes_only_sourced_components_sorted()
    {
        var comps = Build();
        Assert.Equal(2, comps.Count);
        Assert.Equal("OmniButton", comps[0].Name);   // Buttons < Inputs
        Assert.Equal("OmniTextBox", comps[1].Name);
    }

    [Fact]
    public void Build_reflects_button_surface()
    {
        ComponentInfo btn = Build().Single(c => c.Name == "OmniButton");
        Assert.Equal("Buttons", btn.Category);
        Assert.Equal("OmniComponentWithChildren", btn.BaseType);
        Assert.True(btn.HasChildContent);
        Assert.False(btn.IsInput);

        ParamInfo variant = Assert.Single(btn.Parameters, p => p.Name == "Variant");
        Assert.Equal("parameter", variant.Kind);
        Assert.NotNull(variant.EnumValues);
        Assert.Equal("Default", variant.Default);          // read by instantiation

        Assert.Single(btn.Parameters, p => p.Name == "OnClick" && p.Kind == "event");
        // inherited surface is captured with InheritedFrom set
        Assert.Single(btn.Parameters, p => p.Name == "Class" && p.InheritedFrom == "OmniComponent");
    }

    [Fact]
    public void Build_marks_form_input()
    {
        ComponentInfo tb = Build().Single(c => c.Name == "OmniTextBox");
        Assert.True(tb.IsInput);
        Assert.Equal("FormComponent<T>", tb.BaseType);
    }

    [Fact]
    public void Build_uses_description_fallback_when_no_xml_summary()
    {
        var comps = Build(desc: new() { ["OmniButton"] = "A button." });
        Assert.Equal("A button.", comps.Single(c => c.Name == "OmniButton").Summary);
    }

    [Fact]
    public void Build_empty_when_no_sources()
        => Assert.Empty(ManifestBuilder.Build(Lib, [], new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>()));

    [Fact]
    public void Build_resolves_summary_for_param_inherited_from_generic_base()
    {
        // `Value` is declared on FormComponent<string> (a closed generic). Its XML
        // summary must still resolve via the open-generic id — this regressed before
        // the XmlId generic fix (closed-generic FullName != XML doc id).
        var docs = XmlDocText.Load(System.IO.Path.ChangeExtension(Lib.Location, ".xml"));
        Assert.NotEmpty(docs); // guard: XML doc file present next to the assembly
        ComponentInfo tb = Build(docs).Single(c => c.Name == "OmniTextBox");
        ParamInfo value = Assert.Single(tb.Parameters, p => p.Name == "Value");
        Assert.Equal("FormComponent", value.InheritedFrom);
        Assert.False(string.IsNullOrWhiteSpace(value.Summary));
    }
}
