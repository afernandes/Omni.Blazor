using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Trim-safe DataAnnotations evaluation. The framework's object-wide Validator
/// helpers discover members dynamically, so Omni performs the same traversal
/// from statically rooted generic model properties instead.
/// </summary>
internal static class DataAnnotationsValidation
{
    internal static void ValidateObject<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel>(
        TModel model,
        ICollection<ValidationResult> results)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(results);

        foreach (PropertyInfo property in typeof(TModel).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0 || property.GetMethod is null) continue;
            ValidateProperty(model, property, property.GetValue(model), results);
        }

        if (model is IValidatableObject validatable)
        {
            ValidationContext context = CreateContext(model, typeof(TModel).Name, memberName: null);
            foreach (ValidationResult result in validatable.Validate(context))
            {
                if (result != ValidationResult.Success) results.Add(result);
            }
        }
    }

    internal static bool ValidateProperty(
        object model,
        PropertyInfo property,
        object? value,
        ICollection<ValidationResult> results)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(results);

        bool valid = true;
        string displayName = property.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? property.Name;
        ValidationContext context = CreateContext(model, displayName, property.Name);
        foreach (ValidationAttribute attribute in property.GetCustomAttributes<ValidationAttribute>(inherit: true))
        {
            ValidationResult? result = attribute.GetValidationResult(value, context);
            if (result is null || result == ValidationResult.Success) continue;
            results.Add(result);
            valid = false;
        }
        return valid;
    }

    private static ValidationContext CreateContext(object model, string displayName, string? memberName)
        => new(model, displayName, serviceProvider: null, items: null)
        {
            MemberName = memberName
        };
}
