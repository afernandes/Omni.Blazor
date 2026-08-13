using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Localization;

namespace Forneria.Demo.Pages.Localization;

/// <summary>Translations used by the localization showcase in both Server and WebAssembly hosts.</summary>
public static class DemoLocalizationExtensions
{
    /// <summary>Adds small French and Arabic catalogs that demonstrate consumer-owned languages.</summary>
    public static IServiceCollection AddDemoOmniTranslations(this IServiceCollection services)
    {
        services.AddOmniTranslations("fr", new Dictionary<string, string>
        {
            [OmniTranslationKeys.Close] = "Fermer",
            [OmniTranslationKeys.Save] = "Enregistrer",
            [OmniTranslationKeys.Clear] = "Effacer",
            [OmniTranslationKeys.Today] = "Aujourd’hui",
            [OmniTranslationKeys.OpenCalendar] = "Ouvrir le calendrier",
            [OmniTranslationKeys.MessagePlaceholder] = "Écrivez un message…",
            [OmniTranslationKeys.Send] = "Envoyer"
        });

        services.AddOmniTranslations("ar", new Dictionary<string, string>
        {
            [OmniTranslationKeys.Close] = "إغلاق",
            [OmniTranslationKeys.Save] = "حفظ",
            [OmniTranslationKeys.Clear] = "مسح",
            [OmniTranslationKeys.Today] = "اليوم",
            [OmniTranslationKeys.OpenCalendar] = "فتح التقويم",
            [OmniTranslationKeys.MessagePlaceholder] = "اكتب رسالة…",
            [OmniTranslationKeys.Send] = "إرسال"
        });

        return services;
    }
}
