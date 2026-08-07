using Omni.Blazor.Components;
using Omni.Blazor.ManifestGen;
using Omni.Blazor.Models;
using Xunit;

namespace Omni.Blazor.ManifestGen.Tests;

public class ManifestBuilderTests
{
    private static readonly System.Reflection.Assembly Lib = typeof(OmniComponent).Assembly;

    // Only types with a source entry are treated as components — so we drive Build
    // with a controlled map and assert the reflected shape.
    private static List<ComponentInfo> Build(
        Dictionary<string, string>? docs = null,
        Dictionary<string, string>? desc = null)
    {
        var cats = new Dictionary<string, string>
        {
            ["OmniButton"] = "Buttons",
            ["OmniForm"] = "Forms",
            ["OmniTextBox"] = "Inputs",
        };
        var src = new Dictionary<string, string>
        {
            ["OmniButton"] = "src/Omni.Blazor/Components/Buttons/OmniButton.razor",
            ["OmniForm"] = "src/Omni.Blazor/Components/Forms/OmniForm.razor",
            ["OmniTextBox"] = "src/Omni.Blazor/Components/Inputs/OmniTextBox.razor",
        };
        return ManifestBuilder.Build(Lib, docs ?? [], cats, src, desc ?? []);
    }

    [Fact]
    public void Build_includes_only_sourced_components_sorted()
    {
        var comps = Build();
        Assert.Equal(3, comps.Count);
        Assert.Equal("OmniButton", comps[0].Name);   // Buttons < Inputs
        Assert.Equal("OmniForm", comps[1].Name);
        Assert.Equal("OmniTextBox", comps[2].Name);
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
    public void Build_includes_direct_ComponentBase_component()
    {
        ComponentInfo form = Build().Single(c => c.Name == "OmniForm");

        Assert.Equal("ComponentBase", form.BaseType);
        Assert.True(form.HasChildContent);
        Assert.False(form.IsInput);
        Assert.Single(form.Parameters, p => p.Name == "Model");
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

    [Fact]
    public void Configuration_apis_reflect_real_builders_providers_enums_and_xml_docs()
    {
        Dictionary<string, string> sources = new()
        {
            ["DataImportSchema"] = "src/Omni.Blazor/Models/DataImportModels.cs",
            ["DataImportSchemaBuilder"] = "src/Omni.Blazor/Models/DataImportModels.cs",
            ["DataImportColumnBuilder"] = "src/Omni.Blazor/Models/DataImportModels.cs",
            ["DataFormWizardSchemaBuilder"] = "src/Omni.Blazor/Models/DataFormWizardModels.cs",
            ["DataGridFormMutationStatus"] = "src/Omni.Blazor/Models/DataGridFormModels.cs",
            ["IDataGridFormProvider"] = "src/Omni.Blazor/Models/DataGridFormModels.cs",
            ["DelegateDataGridFormProvider"] = "src/Omni.Blazor/Models/DataGridFormModels.cs",
        };
        Dictionary<string, string> docs = XmlDocText.Load(Path.ChangeExtension(Lib.Location, ".xml"));

        List<ConfigurationApiInfo> apis = ConfigurationApiBuilder.Build(Lib, docs, sources);

        Assert.Equal(7, apis.Count);
        Assert.Equal(apis.OrderBy(api => api.Category, StringComparer.Ordinal)
            .ThenBy(api => api.Name, StringComparer.Ordinal), apis);

        ConfigurationApiInfo import = Assert.Single(apis, api => api.Name == "DataImportSchemaBuilder<TItem>");
        Assert.Equal("Data", import.Category);
        Assert.Equal("class", import.Kind);
        Assert.False(string.IsNullOrWhiteSpace(import.Summary));
        Assert.Contains(import.Members, member => member.Kind == "constructor");
        Assert.Contains(import.Members, member => member.Kind == "method"
                                                  && member.Signature.Contains("Column<TValue>", StringComparison.Ordinal)
                                                  && !string.IsNullOrWhiteSpace(member.Summary));

        ConfigurationApiInfo wizard = Assert.Single(apis, api => api.Name == "DataFormWizardSchemaBuilder<TModel>");
        Assert.Equal("Forms", wizard.Category);

        ConfigurationApiInfo provider = Assert.Single(apis, api => api.Name == "IDataGridFormProvider<TItem, TKey>");
        Assert.Equal("interface", provider.Kind);
        Assert.Contains(provider.Members, member => member.Name == "CreateAsync");

        ConfigurationApiInfo mutation = Assert.Single(apis, api => api.Name == nameof(DataGridFormMutationStatus));
        Assert.Equal("enum", mutation.Kind);
        Assert.Contains(mutation.Members, member => member.Kind == "enumValue" && member.Name == "Conflict");
    }

    [Fact]
    public void Configuration_apis_support_value_types_and_require_a_source_entry()
    {
        Dictionary<string, string> sources = new()
        {
            [nameof(DataImportFixtureSchema)] = "fixture.cs",
        };

        ConfigurationApiInfo fixture = Assert.Single(ConfigurationApiBuilder.Build(
            typeof(DataImportFixtureSchema).Assembly,
            [],
            sources));

        Assert.Equal("valueType", fixture.Kind);
        Assert.Equal("fixture.cs", fixture.Source);
        Assert.Contains(fixture.Members, member => member.Kind == "property" && member.Name == nameof(DataImportFixtureSchema.Count));
        Assert.Contains(fixture.Members, member => member.Kind == "method" && member.Signature.StartsWith("static ", StringComparison.Ordinal));
        Assert.Empty(ConfigurationApiBuilder.Build(typeof(DataImportFixtureSchema).Assembly, [], new Dictionary<string, string>()));
    }
}

public readonly struct DataImportFixtureSchema
{
    public int Count { get; init; }
    public static DataImportFixtureSchema Create() => new();
}
