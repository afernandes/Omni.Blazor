namespace Omni.Blazor.Tests;

/// <summary>
/// Repo-wide convention guards, enforced by scanning the component <c>.razor</c>
/// files and reflecting over the assembly. These catch a new component that skips
/// a non-negotiable rule (base class, missing test).
///
/// Deliberately NOT enforced here — too many legitimate exceptions to assert
/// cleanly (a noisy allow-list would defeat the purpose): the root <c>@attributes</c>
/// splat (host/portal and root-less sub-components don't have a splattable root),
/// "DI services over IJSRuntime" (widely and intentionally used by data/overlay
/// components), and <c>@key</c> in every foreach (SVG segments, transient error
/// lists, and intentionally-unkeyed lists like OmniSuggestionChips).
/// </summary>
public class ComponentConventionTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly string[] ComponentRazors =
        Directory.Exists(ComponentsDir)
            ? Directory.GetFiles(ComponentsDir, "*.razor", SearchOption.AllDirectories)
            : [];

    // simple type name -> public, non-abstract ComponentBase subclass in the library
    private static readonly Dictionary<string, Type> ComponentTypes =
        typeof(OmniComponent).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(ComponentBase).IsAssignableFrom(t))
            .GroupBy(t => StripArity(t.Name))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    // Deliberate base-class exceptions: framework-ish components that don't (and
    // shouldn't) inherit OmniComponent — OmniForm wraps EditForm, OmniTheme injects
    // the <head> stylesheet, OmniSpeechToText is headless, OmniOverlayHosts is a host
    // aggregator, plus internal sub-renderers. Adding a NEW component here should be
    // a conscious decision.
    private static readonly HashSet<string> NotOmniComponent = new(StringComparer.Ordinal)
    {
        "OmniForm", "OmniTheme", "OmniSpeechToText", "OmniOverlayHosts",
        "OmniHtmlEditorButton", "OmniTreeLevel", "SchedulerTimeView", "SchedulerYearGrid",
    };

    // Sub-components (items / config / views / internal renderers) that are rendered
    // and tested through their parent component, so a 1:1 <Name>Tests.cs is not expected.
    private static readonly HashSet<string> TestedViaParent = new(StringComparer.Ordinal)
    {
        "OmniDataFilterItem", "OmniDataFilterProperty", "OmniDayView", "OmniGanttColumn",
        "OmniMonthView", "OmniMultiDayView", "OmniTreeItem", "OmniWeekView",
        "OmniYearPlannerView", "OmniYearTimelineView", "OmniYearView", "OmniCarouselItem",
        "OmniDescriptionItem", "OmniTimelineItem", "OmniPanelMenuSection", "OmniStep",
        "OmniTabItem", "OmniTourStep",
        "OmniHtmlEditorButton", "OmniTreeLevel", "SchedulerTimeView", "SchedulerYearGrid",
    };

    [Fact]
    public void There_are_components_to_check()
    {
        // Guards against the scan silently finding nothing (wrong RepoRoot, etc.).
        Assert.True(ComponentRazors.Length > 100, $"expected to scan the component .razor files, found {ComponentRazors.Length} under {ComponentsDir}");
    }

    [Fact]
    public void Every_component_inherits_OmniComponent()
    {
        var offenders = PublicComponents()
            .Where(t => !typeof(OmniComponent).IsAssignableFrom(t))
            .Select(t => StripArity(t.Name))
            .Where(name => !NotOmniComponent.Contains(name))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Components must inherit OmniComponent / OmniComponentWithChildren / FormComponent<T> (or be allow-listed). Offenders: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Every_component_has_a_matching_test_file()
    {
        var testDir = Path.Combine(RepoRoot, "test", "Omni.Blazor.Tests", "Components");
        var testStems = Directory.Exists(testDir)
            ? Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension)
                .ToHashSet(StringComparer.Ordinal)
            : [];

        var missing = PublicComponents()
            .Select(t => StripArity(t.Name))
            .Where(name => !TestedViaParent.Contains(name) && !testStems.Contains(name + "Tests"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            $"Every component needs a <Name>Tests.cs (or be allow-listed as tested-via-parent). Missing: {string.Join(", ", missing)}");
    }

    // The public component types that have a matching .razor file under Components/.
    private static IEnumerable<Type> PublicComponents()
    {
        foreach (string razor in ComponentRazors)
        {
            string name = Path.GetFileNameWithoutExtension(razor);
            if (ComponentTypes.TryGetValue(name, out Type? type))
                yield return type;
        }
    }

    private static string ComponentsDir => Path.Combine(RepoRoot, "src", "Omni.Blazor", "Components");

    private static string StripArity(string name)
    {
        int i = name.IndexOf('`');
        return i < 0 ? name : name[..i];
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Omni.Blazor.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
