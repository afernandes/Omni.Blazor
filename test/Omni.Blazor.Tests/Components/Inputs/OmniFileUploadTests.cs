using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniFileUpload"/>: dropzone + InputFile
/// rendering, label/hint, and the cross-cutting splat. Actual file upload
/// behavior goes through Blazor's InputFile + JS, not covered by unit tests.
/// </summary>
public class OmniFileUploadTests : TestContextBase
{
    [Fact]
    public void Renders_dropzone_with_base_class()
    {
        var cut = Render<OmniFileUpload>();

        Assert.NotNull(cut.Find("div.omni-upload"));
        Assert.NotNull(cut.Find("label.omni-upload-zone"));
        Assert.NotNull(cut.Find("input[type=file]"));
    }

    [Fact]
    public void Renders_LabelText_and_default_hint()
    {
        var cut = Render<OmniFileUpload>(p => p
            .Add(c => c.LabelText, "Solte aqui"));

        Assert.Contains("Solte aqui", cut.Find("div.omni-upload-label").TextContent);
        // Default hint shows the MaxFileSize formatted (10 MB).
        Assert.Contains("MB", cut.Find("div.omni-upload-hint").TextContent);
    }

    [Fact]
    public void Honors_custom_HintText()
    {
        var cut = Render<OmniFileUpload>(p => p
            .Add(c => c.HintText, "Apenas imagens"));

        Assert.Contains("Apenas imagens", cut.Find("div.omni-upload-hint").TextContent);
    }

    [Fact]
    public void Multiple_attribute_propagates_to_input()
    {
        var cut = Render<OmniFileUpload>(p => p.Add(c => c.Multiple, true));
        Assert.True(cut.Find("input[type=file]").HasAttribute("multiple"));
    }

    [Fact]
    public void Accept_forwarded_to_input()
    {
        var cut = Render<OmniFileUpload>(p => p.Add(c => c.Accept, "image/*"));
        Assert.Equal("image/*", cut.Find("input[type=file]").GetAttribute("accept"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniFileUpload>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-upload").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniFileUpload>(p => p.Add(c => c.Style, "margin: 4px"));
        Assert.Equal("margin: 4px", cut.Find("div.omni-upload").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniFileUpload>(p => p
            .AddUnmatched("data-testid", "up1"));

        Assert.Equal("up1", cut.Find("div.omni-upload").GetAttribute("data-testid"));
    }
}
