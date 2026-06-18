using Omni.Blazor.ManifestGen;
using Xunit;

namespace Omni.Blazor.ManifestGen.Tests;

public class RazorScanTests
{
    [Fact]
    public void LeadComment_extracts_first_sentence()
    {
        string razor = """
        @namespace Omni.Blazor.Components
        @inherits OmniComponent

        @*
            Pure-CSS circular spinner. Inherits currentColor.
            Usage: <OmniSpinner Size="Sm" />
        *@

        <span class="@RootCss"></span>
        """;
        Assert.Equal("Pure-CSS circular spinner.", RazorScan.LeadComment(razor));
    }

    [Fact]
    public void LeadComment_single_line()
        => Assert.Equal("A button.", RazorScan.LeadComment("@inherits X\n@* A button. *@\n<button></button>"));

    [Fact]
    public void LeadComment_null_when_no_comment()
        => Assert.Null(RazorScan.LeadComment("@inherits X\n<button></button>"));

    [Fact]
    public void LeadComment_null_when_comment_after_markup()
        => Assert.Null(RazorScan.LeadComment("<button></button>\n@* too late *@"));

    [Fact]
    public void LeadComment_stops_before_usage_line()
    {
        string razor = "@* Does a thing\nUsage: ... *@\n<div></div>";
        Assert.Equal("Does a thing", RazorScan.LeadComment(razor));
    }

    [Fact]
    public void Tokens_collects_declarations_skips_var_refs_and_dedupes()
    {
        string scss = """
        :root {
          --omni-bg: #fff;
          --omni-fg: #000;
          color: var(--omni-bg);
          --omni-bg: #eee;
        }
        """;
        Assert.Equal(["--omni-bg", "--omni-fg"], RazorScan.Tokens(scss));
    }

    [Fact]
    public void Tokens_empty_when_none() => Assert.Empty(RazorScan.Tokens("body { color: red; }"));
}
