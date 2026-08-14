using System.Globalization;

namespace Forneria.Demo.Pages.Localization;

/// <summary>Culture choices shared by the Server and WebAssembly showcase hosts.</summary>
public static class DemoCultures
{
    /// <summary>Supported culture choices in UI order.</summary>
    public static IReadOnlyList<DemoCultureOption> All { get; } =
    [
        new("pt-BR", "Português (Brasil)"),
        new("en-US", "English (United States)"),
        new("fr-FR", "Français (France)"),
        new("ar-SA", "العربية (السعودية)"),
        new("en-XA", "Pseudo — expanded LTR"),
        new("ar-XB", "Pseudo — RTL")
    ];

    /// <summary>
    /// The same choices shaped for <c>OmniCulturePicker</c>. The pseudo-locales carry no
    /// region, so the picker draws their code badge rather than borrowing a flag.
    /// </summary>
    public static IReadOnlyList<Omni.Blazor.Models.OmniCultureOption> PickerOptions { get; } =
    [
        new() { Name = "pt-BR", DisplayName = "Português", Description = "Brasil · pt-BR" },
        new() { Name = "en-US", DisplayName = "English", Description = "United States · en-US" },
        new() { Name = "fr-FR", DisplayName = "Français", Description = "France · fr-FR" },
        new() { Name = "ar-SA", DisplayName = "العربية", Description = "السعودية · ar-SA" },
        new() { Name = "en-XA", DisplayName = "Pseudo LTR", Description = "Texto expandido · en-XA" },
        new() { Name = "ar-XB", DisplayName = "Pseudo RTL", Description = "Espelhado · ar-XB" }
    ];

    /// <summary>Returns whether a culture is allowed by the showcase.</summary>
    public static bool IsSupported(string? cultureName)
        => cultureName is not null && All.Any(option =>
            string.Equals(option.Name, cultureName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the canonical supported culture.</summary>
    public static CultureInfo Get(string cultureName)
    {
        if (!IsSupported(cultureName))
            throw new CultureNotFoundException(nameof(cultureName), cultureName, "The showcase does not support this culture.");
        return CultureInfo.GetCultureInfo(cultureName);
    }
}

/// <summary>One culture displayed by the showcase selector.</summary>
public sealed record DemoCultureOption(string Name, string DisplayName);

/// <summary>Host-specific persistence for changing the showcase culture.</summary>
public interface IDemoCultureManager
{
    /// <summary>Changes and persists the culture, then reloads the current route.</summary>
    ValueTask SetCultureAsync(string cultureName);
}
