using System.Reflection;
using System.Text.RegularExpressions;

namespace Omni.Blazor.Tests;

/// <summary>
/// Convention guards for the <c>.razor</c>-backed components under
/// <c>src/Omni.Blazor/Components</c> (matched to their types by reflection). They catch
/// a new component that skips a non-negotiable rule (base class, missing test, missing
/// showcase usage — CONTRIBUTING.md requires one per component).
/// Scope: only <c>.razor</c> components are scanned — pure-<c>.cs</c> components without
/// a <c>.razor</c> file (e.g. validators) are out of scope.
///
/// Deliberately NOT enforced here — too many legitimate exceptions to assert
/// cleanly (a noisy allow-list would defeat the purpose): the root <c>@attributes</c>
/// splat (host/portal and root-less sub-components don't have a splattable root),
/// <c>@key</c> in every foreach (SVG segments, transient error
/// lists, and intentionally-unkeyed lists like OmniSuggestionChips).
/// </summary>
public class ComponentConventionTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static readonly string[] ComponentRazors =
        Directory.Exists(ComponentsDir)
            ? Directory.GetFiles(ComponentsDir, "*.razor", SearchOption.AllDirectories)
            : [];

    // simple type name -> public, non-abstract ComponentBase subclasses in the library.
    // A lookup (not a dictionary) so same-named types in different namespaces are all checked.
    private static readonly ILookup<string, Type> ComponentTypes =
        typeof(OmniComponent).Assembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && typeof(ComponentBase).IsAssignableFrom(t))
            .ToLookup(t => StripArity(t.Name), StringComparer.Ordinal);

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
        "OmniDataFilterItem", "OmniDayView", "OmniGanttColumn",
        "OmniMonthView", "OmniMultiDayView", "OmniTreeItem", "OmniWeekView",
        "OmniYearPlannerView", "OmniYearTimelineView", "OmniYearView", "OmniCarouselItem",
        "OmniDescriptionItem", "OmniTimelineItem", "OmniPanelMenuSection", "OmniStep",
        "OmniTabItem", "OmniTourStep",
        "OmniTreeGridColumn",
        "OmniDataFormCollectionEditor", "OmniDataFormFieldRenderer", "OmniDataGridFormEditor",
        "OmniDataFormGroupRenderer", "OmniDataFormLookupEditor",
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
                .OfType<string>()
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

    // Components with no live usage in the showcase — every one is deliberate, not an
    // oversight allow-list. Two shapes recur:
    //   • rendered only by another component's own markup, never placed directly by a
    //     showcase author (the parent's showcase page exercises it transitively);
    //   • activated by DialogService via typeof() rather than a markup tag, so its name
    //     never appears as a literal <Tag> anywhere.
    private static readonly HashSet<string> NotDirectlyShowcased = new(StringComparer.Ordinal)
    {
        // DialogService.Alert/Confirm open these by typeof(), not by tag — exercised via
        // DialogPage.razor's Dialog.Alert(...)/Dialog.Confirm(...) calls.
        "AlertDialog", "ConfirmDialog",
        // Rendered only inside OmniDatePicker/OmniDateRangePicker (both showcased).
        "OmniCalendar", "OmniTimePicker",
        // Placed once via <OmniOverlayHosts /> (itself already exempt as a host aggregator),
        // never shown individually.
        "OmniContextMenuHost", "OmniDialogHost", "OmniNotificationHost",
        "OmniTooltipHost", "OmniTourHost",
        // Rendered only inside OmniDropZone (showcased).
        "OmniDropZoneItem",
        // Rendered only inside OmniGanttForm/OmniKanbanForm/OmniSchedulerForm (all showcased).
        "OmniEntityEditorHost",
        // Already TestedViaParent for the same reason: internal sub-renderers a showcase
        // page never places directly, only their owning composite component does.
        "OmniDataFilterItem", "OmniDataFormCollectionEditor", "OmniDataFormFieldRenderer",
        "OmniDataFormGroupRenderer", "OmniDataFormLookupEditor", "OmniDataGridFormEditor",
        "SchedulerTimeView", "SchedulerYearGrid",
    };

    [Fact]
    public void Every_component_has_a_showcase_usage()
    {
        var showcaseDir = Path.Combine(
            RepoRoot, "src", "Forneria.Demo", "Forneria.Demo.Pages", "Pages", "Showcase");
        var showcaseText = Directory.Exists(showcaseDir)
            ? string.Join('\n', Directory.GetFiles(showcaseDir, "*.razor", SearchOption.AllDirectories)
                .Select(File.ReadAllText))
            : "";

        var missing = PublicComponents()
            .Select(t => StripArity(t.Name))
            .Distinct(StringComparer.Ordinal)
            .Where(name => !NotDirectlyShowcased.Contains(name)
                && !Regex.IsMatch(showcaseText, $"<{Regex.Escape(name)}(?=[\\s/>])"))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(missing.Count == 0,
            "Every component needs a live <Tag> usage under Forneria.Demo.Pages/Pages/Showcase "
            + $"(or be allow-listed as shown via its parent/service). Missing: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Components_do_not_inject_IJSRuntime_directly()
    {
        var offenders = ComponentRazors
            .Where(path => File.ReadAllText(path).Contains("@inject IJSRuntime", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepoRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Library components must use typed interop services. Offenders: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Public_API_does_not_expose_IJSRuntime()
    {
        Type jsRuntime = typeof(Microsoft.JSInterop.IJSRuntime);
        var offenders = typeof(OmniComponent).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Where(member => MemberExposesType(member, jsRuntime))
            .Select(member => $"{member.DeclaringType?.FullName}.{member.Name}")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Public APIs must not expose IJSRuntime. Offenders: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Public_API_contains_no_obsolete_members_before_v1()
    {
        string libraryDir = Path.Combine(RepoRoot, "src", "Omni.Blazor");
        var offenders = Directory.GetFiles(libraryDir, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("[Obsolete", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(RepoRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Pre-v1 obsolete shims must be removed instead of retained. Offenders: {string.Join(", ", offenders)}");
    }

    // The public component types that have a matching .razor file under Components/.
    private static IEnumerable<Type> PublicComponents()
    {
        foreach (string razor in ComponentRazors)
        {
            string? name = Path.GetFileNameWithoutExtension(razor);
            if (name is null) continue;
            foreach (Type type in ComponentTypes[name]) // empty when no match
                yield return type;
        }
    }

    private static string ComponentsDir => Path.Combine(RepoRoot, "src", "Omni.Blazor", "Components");

    private static string StripArity(string name)
    {
        int i = name.IndexOf('`');
        return i < 0 ? name : name[..i];
    }

    private static bool MemberExposesType(MemberInfo member, Type sought)
    {
        return member switch
        {
            MethodInfo method => TypeContains(method.ReturnType, sought)
                || method.GetParameters().Any(parameter => TypeContains(parameter.ParameterType, sought)),
            ConstructorInfo constructor => constructor.GetParameters()
                .Any(parameter => TypeContains(parameter.ParameterType, sought)),
            PropertyInfo property => TypeContains(property.PropertyType, sought),
            FieldInfo field => TypeContains(field.FieldType, sought),
            EventInfo eventInfo => TypeContains(eventInfo.EventHandlerType, sought),
            _ => false,
        };
    }

    private static bool TypeContains(Type? candidate, Type sought)
    {
        if (candidate is null)
            return false;
        if (candidate == sought)
            return true;
        if (candidate.HasElementType)
            return TypeContains(candidate.GetElementType(), sought);
        return candidate.IsGenericType
            && candidate.GetGenericArguments().Any(argument => TypeContains(argument, sought));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Omni.Blazor.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
