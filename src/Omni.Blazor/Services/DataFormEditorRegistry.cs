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

    private static Type ValidateComponentType(Type componentType, Type valueType)
    {
        Type resolved = componentType;
        if (componentType.IsGenericTypeDefinition)
        {
            Type[] arguments = componentType.GetGenericArguments();
            if (arguments.Length != 1)
            {
                throw new InvalidOperationException(
                    $"DataForm editor '{componentType.Name}' must have exactly one generic type parameter.");
            }
            resolved = componentType.MakeGenericType(valueType);
        }

        if (!typeof(IComponent).IsAssignableFrom(resolved))
        {
            throw new InvalidOperationException(
                $"DataForm editor '{resolved.FullName}' must implement {nameof(IComponent)}.");
        }

        HashSet<string> parameters = resolved.GetProperties()
            .Where(static property => property.IsDefined(typeof(ParameterAttribute), inherit: true))
            .Select(static property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        string[] required = ["Value", "ValueChanged", "ValueExpression"];
        foreach (string parameter in required)
        {
            if (!parameters.Contains(parameter))
            {
                throw new InvalidOperationException(
                    $"DataForm editor '{resolved.FullName}' must expose a [Parameter] named '{parameter}'.");
            }
        }

        return resolved;
    }
}

/// <summary>One immutable value-type to component mapping used by DataForm.</summary>
public sealed record DataFormEditorRegistration(Type ValueType, Type ComponentType);

