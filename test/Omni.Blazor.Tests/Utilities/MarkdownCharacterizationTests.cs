namespace Omni.Blazor.Tests.Utilities;

/// <summary>
/// Pins the exact HTML <see cref="MarkdownRenderer"/> produces for
/// <see cref="MarkdownCorpus"/> — one case per supported feature, plus the edge
/// cases that tell the individual patterns apart.
///
/// Written to make the <c>[GeneratedRegex]</c> conversion safe: swapping ~31
/// patterns from the static <c>Regex</c> overloads to source-generated ones is a
/// mechanical change that must not alter a single character of output, and a
/// subtly different pattern would otherwise fail silently on inputs no
/// hand-written test happens to cover.
///
/// A diff here is not automatically a bug — it means renderer output changed.
/// Read the diff, decide whether the change is intended, and if it is, refresh
/// <c>Baselines/markdown-corpus.txt</c> in the same commit as the change that
/// caused it.
/// </summary>
public class MarkdownCharacterizationTests
{
    private const string Separator = "\n---\n";

    [Fact]
    public void Renderer_output_matches_the_recorded_baseline()
    {
        string baselinePath = Path.Combine(
            FindRepoRoot(),
            "test", "Omni.Blazor.Tests", "Utilities", "Baselines", "markdown-corpus.txt");

        string expected = Normalize(File.ReadAllText(baselinePath));
        string actual = Normalize(RenderCorpus());

        // Compared case by case: a whole-file assert on 54 cases reports one
        // enormous diff, while this names the case that moved.
        string[] expectedCases = expected.Split(Separator);
        string[] actualCases = actual.Split(Separator);

        Assert.Equal(expectedCases.Length, actualCases.Length);
        for (int i = 0; i < expectedCases.Length; i++)
        {
            Assert.Equal(expectedCases[i], actualCases[i]);
        }
    }

    [Fact]
    public void Corpus_covers_every_case_in_the_baseline()
    {
        // Guards the guard: a corpus entry silently dropped would otherwise make
        // the comparison above pass over fewer cases than it appears to.
        Assert.Equal(54, MarkdownCorpus.Cases.Length);
    }

    private static string RenderCorpus()
    {
        var outputs = new List<string>(MarkdownCorpus.Cases.Length);
        foreach ((string name, string source, bool allowHtml) in MarkdownCorpus.Cases)
        {
            outputs.Add($"### {name}\n{MarkdownRenderer.ToHtml(source, allowHtml)}");
        }
        return string.Join(Separator, outputs);
    }

    /// <summary>Line endings differ between the checked-out file and the renderer's output.</summary>
    private static string Normalize(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }
}
