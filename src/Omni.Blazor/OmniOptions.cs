using Omni.Blazor.Localization;

namespace Omni.Blazor;

/// <summary>
/// Startup options for the library, configured through
/// <see cref="ServiceCollectionExtensions.AddOmniComponents(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{OmniOptions}?)"/>.
/// </summary>
public sealed class OmniOptions
{
    /// <summary>
    /// The strings components fall back to when the consumer does not pass an explicit
    /// parameter. Defaults to the built-in pt-BR set; assign <see cref="OmniTexts.English"/>
    /// (or your own instance) to translate the whole library at once.
    /// </summary>
    public OmniTexts Texts { get; set; } = OmniTexts.Default;
}
