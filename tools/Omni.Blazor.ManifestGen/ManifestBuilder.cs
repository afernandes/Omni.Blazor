using System.Reflection;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.ManifestGen;

/// <summary>
/// Builds the component list by reflecting over the Omni.Blazor assembly. A type
/// counts as a component when it is a public, non-abstract
/// <see cref="ComponentBase"/> subclass with a matching component source and is
/// not marked with <see cref="OmniCatalogIgnoreAttribute"/>. Kept separate from
/// IO so it is unit-testable against the real assembly with controlled
/// name→source/category/description maps.
/// </summary>
public static class ManifestBuilder
{
    public static List<ComponentInfo> Build(
        Assembly assembly,
        Dictionary<string, string> docs,
        IReadOnlyDictionary<string, string> categoryByName,
        IReadOnlyDictionary<string, string> sourceByName,
        IReadOnlyDictionary<string, string> descByName)
    {
        Type componentBaseType = typeof(ComponentBase);
        List<ComponentInfo> components = [];

        foreach (Type t in TypeNames.SafeGetTypes(assembly))
        {
            if (!t.IsClass || t.IsAbstract || !t.IsPublic) continue;
            if (!componentBaseType.IsAssignableFrom(t)) continue;
            if (t.GetCustomAttribute<OmniCatalogIgnoreAttribute>() is not null) continue;

            string simpleName = TypeNames.StripArity(t.Name);
            if (!sourceByName.TryGetValue(simpleName, out string? source)) continue;

            bool isInput = TypeNames.IsFormInput(t);
            bool hasChildren = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Any(p => p.Name == "ChildContent"
                    && p.GetCustomAttribute<ParameterAttribute>() is not null
                    && TypeNames.Classify(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType).kind == "slot");
            string baseLabel = isInput
                ? "FormComponent<T>"
                : typeof(OmniComponentWithChildren).IsAssignableFrom(t)
                    ? "OmniComponentWithChildren"
                    : typeof(OmniComponent).IsAssignableFrom(t)
                        ? "OmniComponent"
                        : TypeNames.Friendly(t.BaseType ?? componentBaseType);

            object? instance = TypeNames.TryInstantiate(t);

            List<ParamInfo> ps = [];
            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (p.GetCustomAttribute<ParameterAttribute>() is null) continue;

                Type pt = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                (string kind, string? ctx) = TypeNames.Classify(pt);
                string? contextType = kind == "slot" ? ctx : null;

                EnumVal[]? enumValues = null;
                if (kind == "parameter" && pt.IsEnum)
                {
                    enumValues = pt.GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Select(f => new EnumVal(f.Name, XmlDocText.Get(docs, $"F:{TypeNames.XmlId(pt)}.{f.Name}")))
                        .ToArray();
                }

                string? def = null;
                if (kind == "parameter" && instance is not null && TypeNames.IsSimple(pt))
                {
                    try { def = TypeNames.DefaultToString(p.GetValue(instance)); }
                    catch { /* getter threw without DI — leave default unknown */ }
                }

                bool required = p.GetCustomAttribute<EditorRequiredAttribute>() is not null;
                string? inheritedFrom = p.DeclaringType is { } dt && dt != t ? TypeNames.StripArity(dt.Name) : null;
                string? summary = XmlDocText.Get(docs, $"P:{TypeNames.XmlId(p.DeclaringType!)}.{p.Name}");

                ps.Add(new ParamInfo(p.Name, kind, TypeNames.Friendly(p.PropertyType), contextType, enumValues, def, required, summary, inheritedFrom));
            }

            // Stable order: own params first (alpha), then inherited (alpha).
            ps = [.. ps.OrderBy(p => p.InheritedFrom is not null).ThenBy(p => p.Name, StringComparer.Ordinal)];

            components.Add(new ComponentInfo(
                Name: simpleName,
                Category: categoryByName.GetValueOrDefault(simpleName, "Other"),
                BaseType: baseLabel,
                IsInput: isInput,
                HasChildContent: hasChildren,
                Summary: XmlDocText.Get(docs, $"T:{TypeNames.XmlId(t)}") ?? descByName.GetValueOrDefault(simpleName),
                Source: source,
                Parameters: [.. ps]));
        }

        return [.. components.OrderBy(c => c.Category, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal)];
    }
}
