using System.Xml.Linq;
using Omni.Blazor.ManifestGen;
using Xunit;

namespace Omni.Blazor.ManifestGen.Tests;

public class XmlDocTextTests
{
    [Theory]
    [InlineData("M:Omni.Blazor.X.GroupByAsync(System.String)", "GroupByAsync")]
    [InlineData("T:Omni.Blazor.Components.OmniButton", "OmniButton")]
    [InlineData("P:Omni.Blazor.X.Prop", "Prop")]
    [InlineData("NoPrefixOrDot", "NoPrefixOrDot")]
    [InlineData("Just.A.Dotted.Path", "Path")]
    public void SimplifyCref(string cref, string expected) => Assert.Equal(expected, XmlDocText.SimplifyCref(cref));

    [Fact]
    public void Flatten_resolves_see_and_c_and_collapses_whitespace()
    {
        var el = XElement.Parse("<summary>Shows a   <see cref=\"T:Ns.OmniBadge\"/> next to <c>OmniButton</c>.\n  Wraps text.</summary>");
        Assert.Equal("Shows a OmniBadge next to OmniButton. Wraps text.", XmlDocText.Flatten(el));
    }

    [Fact]
    public void Flatten_paramref_uses_name()
    {
        var el = XElement.Parse("<summary>Uses <paramref name=\"value\"/> directly.</summary>");
        Assert.Equal("Uses value directly.", XmlDocText.Flatten(el));
    }

    [Fact]
    public void Load_reads_member_summaries()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="P:Ns.Type.Prop"><summary>The prop.</summary></member>
                <member name="T:Ns.Type"><summary>The type.</summary></member>
                <member name="F:Ns.Enum.Value"></member>
              </members>
            </doc>
            """);
            var docs = XmlDocText.Load(path);
            Assert.Equal("The prop.", XmlDocText.Get(docs, "P:Ns.Type.Prop"));
            Assert.Equal("The type.", XmlDocText.Get(docs, "T:Ns.Type"));
            Assert.Null(XmlDocText.Get(docs, "F:Ns.Enum.Value")); // no summary element → not stored
            Assert.Null(XmlDocText.Get(docs, "P:Missing.Member"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_missing_file_returns_empty() => Assert.Empty(XmlDocText.Load(Path.Combine(Path.GetTempPath(), "does-not-exist-xyz.xml")));
}
