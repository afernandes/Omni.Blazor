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

/// <summary>Reflects the fluent schema and provider APIs used to configure complex components.</summary>
public static class ConfigurationApiBuilder
{
    /// <summary>Builds a stable catalog for typed fluent component and provider APIs.</summary>
    public static List<ConfigurationApiInfo> Build(
        Assembly assembly,
        Dictionary<string, string> docs,
        IReadOnlyDictionary<string, string> sourceByName)
    {
        List<ConfigurationApiInfo> apis = [];
        foreach (Type type in TypeNames.SafeGetTypes(assembly))
        {
            if (!type.IsPublic || !IsCatalogApi(type)) continue;
            string name = TypeNames.StripArity(type.Name);
            if (!sourceByName.TryGetValue(name, out string? source)) continue;

            List<ApiMemberInfo> members = [];
            const BindingFlags declaredPublic = BindingFlags.Public | BindingFlags.Instance |
                                                BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (ConstructorInfo constructor in type.GetConstructors(declaredPublic))
            {
                members.Add(new ApiMemberInfo(
                    name,
                    "constructor",
                    $"{name}({FormatParameters(constructor.GetParameters())})",
                    GetMemberSummary(docs, type, "#ctor")));
            }
            foreach (PropertyInfo property in type.GetProperties(declaredPublic))
            {
                members.Add(new ApiMemberInfo(
                    property.Name,
                    "property",
                    $"{TypeNames.Friendly(property.PropertyType)} {property.Name}",
                    XmlDocText.Get(docs, $"P:{TypeNames.XmlId(type)}.{property.Name}")));
            }
            foreach (MethodInfo method in type.GetMethods(declaredPublic))
            {
                if (method.IsSpecialName) continue;
                string generic = method.IsGenericMethodDefinition
                    ? $"<{string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name))}>"
                    : string.Empty;
                members.Add(new ApiMemberInfo(
                    method.Name,
                    "method",
                    $"{(method.IsStatic ? "static " : string.Empty)}{TypeNames.Friendly(method.ReturnType)} {method.Name}{generic}({FormatParameters(method.GetParameters())})",
                    GetMemberSummary(docs, type, method.Name)));
            }
            if (type.IsEnum)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    members.Add(new ApiMemberInfo(
                        field.Name,
                        "enumValue",
                        field.Name,
                        XmlDocText.Get(docs, $"F:{TypeNames.XmlId(type)}.{field.Name}")));
                }
            }

            apis.Add(new ConfigurationApiInfo(
                TypeNames.Friendly(type),
                name.StartsWith("DataForm", StringComparison.Ordinal)
                    ? "Forms"
                    : name.StartsWith("Chart", StringComparison.Ordinal) ? "Display" : "Data",
                type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "valueType" : "class",
                XmlDocText.Get(docs, $"T:{TypeNames.XmlId(type)}"),
                source,
                [.. members.OrderBy(member => member.Name, StringComparer.Ordinal)
                    .ThenBy(member => member.Signature, StringComparer.Ordinal)]));
        }
        return [.. apis.OrderBy(api => api.Category, StringComparer.Ordinal)
            .ThenBy(api => api.Name, StringComparer.Ordinal)];
    }

    private static bool IsCatalogApi(Type type)
    {
        string name = TypeNames.StripArity(type.Name);
        bool family = name.StartsWith("DataForm", StringComparison.Ordinal)
                       || name.StartsWith("DataFilter", StringComparison.Ordinal)
                       || name.StartsWith("DataGrid", StringComparison.Ordinal)
                       || name.StartsWith("DataGridForm", StringComparison.Ordinal)
                       || name.StartsWith("IDataGridForm", StringComparison.Ordinal)
                       || name.StartsWith("DelegateDataGridForm", StringComparison.Ordinal)
                       || name.StartsWith("DataImport", StringComparison.Ordinal)
                       || name.StartsWith("Gantt", StringComparison.Ordinal)
                       || name.StartsWith("Scheduler", StringComparison.Ordinal)
                       || name.StartsWith("Kanban", StringComparison.Ordinal)
                       || name.StartsWith("Chart", StringComparison.Ordinal)
                       || name.StartsWith("Diagram", StringComparison.Ordinal);
        return family && (name.Contains("Schema", StringComparison.Ordinal)
                          || name.Contains("Builder", StringComparison.Ordinal)
                          || name.Contains("Definition", StringComparison.Ordinal)
                          || name.Contains("Query", StringComparison.Ordinal)
                          || name is "DataFilterField" or "DataFilterValueKind"
                          || name.EndsWith("Value", StringComparison.Ordinal)
                          || name.Contains("Provider", StringComparison.Ordinal)
                          || name.Contains("Mutation", StringComparison.Ordinal));
    }

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
        => string.Join(", ", parameters.Select(parameter =>
            $"{TypeNames.Friendly(parameter.ParameterType)} {parameter.Name}"));

    private static string? GetMemberSummary(
        IReadOnlyDictionary<string, string> docs,
        Type type,
        string memberName)
    {
        string prefix = $"M:{TypeNames.XmlId(type)}.{memberName}";
        foreach ((string id, string summary) in docs)
        {
            if (id.Equals(prefix, StringComparison.Ordinal)
                || id.StartsWith(prefix + "(", StringComparison.Ordinal)
                || id.StartsWith(prefix + "``", StringComparison.Ordinal))
                return summary;
        }
        return null;
    }
}
