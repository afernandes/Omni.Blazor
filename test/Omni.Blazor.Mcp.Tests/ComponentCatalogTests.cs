using Omni.Blazor.Mcp;
using Xunit;

namespace Omni.Blazor.Mcp.Tests;

public class ComponentCatalogTests
{
    // Small, stable manifest fixture exercising every kind/branch:
    // - OmniButton: ChildContent component, enum param, event, inherited slot.
    // - OmniTextBox: form input, no required params.
    // - OmniAlert: required enum param (drives Example), summary used for search ranking.
    private const string Fixture = """
    {
      "package": "AndersonN.Omni.Blazor",
      "version": "9.9.9-test",
      "repository": "https://example/repo",
      "count": 3,
      "components": [
        {
          "name": "OmniButton", "category": "Buttons", "baseType": "OmniComponentWithChildren",
          "isInput": false, "hasChildContent": true, "summary": "Clickable button.",
          "source": "src/Omni.Blazor/Components/Buttons/OmniButton.razor",
          "parameters": [
            { "name": "Variant", "kind": "parameter", "type": "ButtonVariant",
              "enumValues": [ { "name": "Default" }, { "name": "Primary", "summary": "High emphasis" } ],
              "default": "Default", "required": false, "summary": "Visual variant." },
            { "name": "OnClick", "kind": "event", "type": "EventCallback<MouseEventArgs>",
              "required": false, "summary": "Click handler." },
            { "name": "ChildContent", "kind": "slot", "type": "RenderFragment",
              "required": false, "inheritedFrom": "OmniComponentWithChildren" }
          ]
        },
        {
          "name": "OmniTextBox", "category": "Inputs", "baseType": "FormComponent<T>",
          "isInput": true, "hasChildContent": false, "summary": "Text input field.",
          "source": "src/Omni.Blazor/Components/Inputs/OmniTextBox.razor",
          "parameters": [
            { "name": "Placeholder", "kind": "parameter", "type": "string",
              "required": false, "summary": "Placeholder text." }
          ]
        },
        {
          "name": "OmniAlert", "category": "Display", "baseType": "OmniComponent",
          "isInput": false, "hasChildContent": false, "summary": "Button-like inline alert.",
          "source": "src/Omni.Blazor/Components/Display/OmniAlert.razor",
          "parameters": [
            { "name": "Severity", "kind": "parameter", "type": "NotificationSeverity",
              "enumValues": [ { "name": "Info" } ], "default": "Info", "required": true,
              "summary": "Severity." }
          ]
        }
      ]
    }
    """;

    private static ComponentCatalog Catalog() => ComponentCatalog.FromJson(Fixture);

    [Fact]
    public void FromJson_parses_components()
    {
        ComponentCatalog c = Catalog();
        Assert.Equal(3, c.Count);
        Assert.Equal(["Buttons", "Inputs", "Display"], c.Categories);
    }

    [Fact]
    public void FromJson_invalid_throws()
        => Assert.Throws<InvalidOperationException>(() => ComponentCatalog.FromJson("null"));

    [Fact]
    public void List_null_returns_all() => Assert.Equal(3, Catalog().List(null).Count);

    [Theory]
    [InlineData("Inputs")]
    [InlineData("inputs")]   // case-insensitive
    [InlineData(" Inputs ")] // trimmed
    public void List_byCategory_filters(string category)
    {
        IReadOnlyList<Component> items = Catalog().List(category);
        Assert.Equal("OmniTextBox", Assert.Single(items).Name);
    }

    [Fact]
    public void List_unknownCategory_empty() => Assert.Empty(Catalog().List("Nope"));

    [Theory]
    [InlineData("OmniButton")]
    [InlineData("omnibutton")]   // case-insensitive
    [InlineData(" OmniButton ")] // trimmed
    public void Get_finds(string name) => Assert.NotNull(Catalog().Get(name));

    [Fact]
    public void Get_unknown_isNull() => Assert.Null(Catalog().Get("Nope"));

    [Fact]
    public void Get_null_isNull() => Assert.Null(Catalog().Get(null));

    [Fact]
    public void Search_byName_and_byCategory_and_bySummary()
    {
        ComponentCatalog c = Catalog();
        Assert.Equal("OmniTextBox", Assert.Single(c.Search("field")).Name);  // summary "Text input field."
        Assert.Equal("OmniTextBox", Assert.Single(c.Search("Inputs")).Name);  // category
        Assert.Equal("OmniAlert", Assert.Single(c.Search("inline")).Name);    // summary
    }

    [Fact]
    public void Search_empty_or_blank_returnsEmpty()
    {
        ComponentCatalog c = Catalog();
        Assert.Empty(c.Search(""));
        Assert.Empty(c.Search("   "));
        Assert.Empty(c.Search(null));
    }

    [Fact]
    public void Search_ranks_nameMatches_first()
    {
        // "button" matches OmniButton by name and OmniAlert by summary.
        IReadOnlyList<Component> hits = Catalog().Search("button");
        Assert.Equal(2, hits.Count);
        Assert.Equal("OmniButton", hits[0].Name); // name match ranks first
        Assert.Equal("OmniAlert", hits[1].Name);
    }

    [Fact]
    public void Example_childContent_component_wraps()
        => Assert.Equal("<OmniButton>...</OmniButton>", Catalog().Example("OmniButton"));

    [Fact]
    public void Example_leaf_component_selfCloses()
        => Assert.Equal("<OmniTextBox />", Catalog().Example("OmniTextBox"));

    [Fact]
    public void Example_required_enum_param_isIncluded()
        => Assert.Equal("<OmniAlert Severity=\"NotificationSeverity.Info\" />", Catalog().Example("OmniAlert"));

    [Fact]
    public void Example_unknown_throws()
        => Assert.Throws<KeyNotFoundException>(() => Catalog().Example("Nope"));

    [Fact]
    public void ListText_renders_bullets()
    {
        string text = Catalog().ListText(null);
        Assert.Contains("- OmniButton [Buttons]: Clickable button.", text);
        Assert.Contains("- OmniTextBox [Inputs]: Text input field.", text);
    }

    [Fact]
    public void ListText_unknownCategory_listsCategories()
    {
        string text = Catalog().ListText("Zzz");
        Assert.Contains("Categories:", text);
        Assert.Contains("Buttons", text);
    }

    [Fact]
    public void SearchText_noMatch_message()
        => Assert.Equal("No components match 'zzz'.", Catalog().SearchText("zzz"));

    [Fact]
    public void Describe_found_hasAllSections_and_example()
    {
        string text = Catalog().Describe("OmniButton");
        Assert.Contains("# OmniButton  (Buttons)", text);
        Assert.Contains("Clickable button.", text);
        Assert.Contains("## Parameters", text);
        Assert.Contains("Variant: ButtonVariant {Default | Primary}", text);
        Assert.Contains("= Default", text);
        Assert.Contains("## Events", text);
        Assert.Contains("OnClick", text);
        Assert.Contains("## Example", text);
        Assert.Contains("<OmniButton>...</OmniButton>", text);
    }

    [Fact]
    public void Describe_input_mentionsFormSurface()
        => Assert.Contains("inputs also expose Value/ValueChanged", Catalog().Describe("OmniTextBox"));

    [Fact]
    public void Describe_unknown_suggests_nearMatch()
    {
        string text = Catalog().Describe("OmniButt");
        Assert.Contains("not found", text);
        Assert.Contains("Did you mean", text);
        Assert.Contains("OmniButton", text);
    }

    [Fact]
    public void Describe_unknown_noNearMatch_plainMessage()
        => Assert.Equal("Component 'Zzz' not found.", Catalog().Describe("Zzz"));

    [Fact]
    public void Load_embedded_manifest_is_selfContained()
    {
        // No path → loads the components.json embedded at build time (the real one).
        ComponentCatalog c = ComponentCatalog.Load(null);
        Assert.True(c.Count > 50, $"expected the embedded manifest to have many components, got {c.Count}");
        Assert.NotNull(c.Get("OmniButton"));
    }

    [Fact]
    public void Load_fromPath_reads_file()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, Fixture);
            Assert.Equal(3, ComponentCatalog.Load(path).Count);
        }
        finally { File.Delete(path); }
    }
}

public class OmniCatalogToolsTests
{
    private static ComponentCatalog Catalog() => ComponentCatalog.Load(null); // embedded real manifest

    [Fact]
    public void ListComponents_delegates_toCatalog()
    {
        ComponentCatalog c = Catalog();
        Assert.Equal(c.ListText("Buttons"), OmniCatalogTools.ListComponents(c, "Buttons"));
    }

    [Fact]
    public void GetComponent_delegates_toCatalog()
    {
        ComponentCatalog c = Catalog();
        Assert.Equal(c.Describe("OmniButton"), OmniCatalogTools.GetComponent(c, "OmniButton"));
    }

    [Fact]
    public void SearchComponents_delegates_toCatalog()
    {
        ComponentCatalog c = Catalog();
        Assert.Equal(c.SearchText("grid"), OmniCatalogTools.SearchComponents(c, "grid"));
    }
}
