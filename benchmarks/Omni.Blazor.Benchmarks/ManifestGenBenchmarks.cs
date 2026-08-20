using System.Reflection;
using BenchmarkDotNet.Attributes;
using Omni.Blazor.Components;
using Omni.Blazor.ManifestGen;

namespace Omni.Blazor.Benchmarks;

/// <summary>
/// The manifest generator reflects over the whole library to produce
/// <c>docs/components.json</c>, <c>llms.txt</c> and <c>llms-full.txt</c>. It runs
/// in CI on every push (the drift check), so its cost is paid constantly even
/// though no user ever waits on it.
///
/// <see cref="BuildWholeLibrary"/> is the real workload — reflection over every
/// public component in <c>Omni.Blazor</c>. The per-type helpers are measured
/// separately because they run once per type per parameter and dominate the
/// inner loop.
/// </summary>
[MemoryDiagnoser]
public class ManifestGenBenchmarks
{
    private Assembly _library = null!;
    private Dictionary<string, string> _categories = [];
    private Dictionary<string, string> _sources = [];
    private Dictionary<string, string> _descriptions = [];
    private readonly Dictionary<string, string> _xmlDocs = new(StringComparer.Ordinal);
    private Type[] _types = [];

    [GlobalSetup]
    public void Setup()
    {
        _library = typeof(OmniComponent).Assembly;

        // The generator normally fills these by scanning .razor files on disk.
        // Reading the filesystem here would measure the disk, not the generator,
        // so they are pre-populated from the same type set.
        _types = _library.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract
                && typeof(Microsoft.AspNetCore.Components.ComponentBase).IsAssignableFrom(t))
            .ToArray();

        _categories = _types.ToDictionary(StripArityKey, _ => "Inputs", StringComparer.Ordinal);
        _sources = _types.ToDictionary(
            StripArityKey,
            t => $"src/Omni.Blazor/Components/{StripArityKey(t)}.razor",
            StringComparer.Ordinal);
        _descriptions = _types.ToDictionary(StripArityKey, _ => "A component.", StringComparer.Ordinal);
    }

    private static string StripArityKey(Type type) => TypeNames.StripArity(type.Name);

    [Benchmark]
    public int BuildWholeLibrary()
    {
        // Empty rather than null: an XML-doc lookup that always misses still
        // exercises the same code path, and Build does not accept null.
        var components = ManifestBuilder.Build(
            _library,
            docs: _xmlDocs,
            _categories,
            _sources,
            _descriptions);
        return components.Count;
    }

    /// <summary>Runs once per generic parameter and return type across the catalog.</summary>
    [Benchmark]
    public int FriendlyTypeNames()
    {
        int length = 0;
        foreach (Type type in _types)
            length += TypeNames.Friendly(type).Length;
        return length;
    }

    /// <summary>Runs once per type to build the XML documentation lookup key.</summary>
    [Benchmark]
    public int XmlIds()
    {
        int length = 0;
        foreach (Type type in _types)
            length += TypeNames.XmlId(type).Length;
        return length;
    }
}
