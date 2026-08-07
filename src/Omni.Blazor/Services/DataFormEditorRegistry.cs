using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Resolves application-defined DataForm editors from immutable startup registrations
/// and optional scoped resolvers. Later registrations have precedence.
/// </summary>
public sealed class DataFormEditorRegistry
{
    private readonly DataFormEditorRegistration[] _registrations;
    private readonly IDataFormEditorResolver[] _resolvers;

    /// <summary>Creates a registry from dependency-injection registrations.</summary>
    public DataFormEditorRegistry(
        IEnumerable<DataFormEditorRegistration> registrations,
        IEnumerable<IDataFormEditorResolver> resolvers)
    {
        _registrations = registrations.ToArray();
        _resolvers = resolvers.ToArray();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    internal Type? Resolve(DataFormEditorResolverContext context)
    {
        for (int index = _resolvers.Length - 1; index >= 0; index--)
        {
            Type? resolved = _resolvers[index].Resolve(context);
            if (resolved is not null) return ValidateComponentType(resolved, context.ValueType);
        }

        for (int index = _registrations.Length - 1; index >= 0; index--)
        {
            DataFormEditorRegistration registration = _registrations[index];
            if (registration.ValueType != context.ValueType) continue;
            return ValidateComponentType(registration.ComponentType, context.ValueType);
        }

        return null;
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    private static Type ValidateComponentType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType,
        Type valueType)
    {
        if (componentType.ContainsGenericParameters)
        {
            throw new InvalidOperationException(
                $"DataForm editor '{componentType.Name}' must be a closed component type. " +
                "Register each TValue/TComponent pair through AddOmniDataFormEditor<TValue, TComponent>().");
        }

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new InvalidOperationException(
                $"DataForm editor '{componentType.FullName}' must implement {nameof(IComponent)}.");
        }

        HashSet<string> parameters = componentType.GetProperties()
            .Where(static property => property.IsDefined(typeof(ParameterAttribute), inherit: true))
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        string[] required = ["Value", "ValueChanged", "ValueExpression"];
        foreach (string parameter in required)
        {
            if (!parameters.Contains(parameter))
            {
                throw new InvalidOperationException(
                    $"DataForm editor '{componentType.FullName}' must expose a [Parameter] named '{parameter}'.");
            }
        }

        return componentType;
    }
}

/// <summary>One immutable value-type to component mapping used by DataForm.</summary>
public sealed record DataFormEditorRegistration(
    Type ValueType,
    [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type ComponentType);
