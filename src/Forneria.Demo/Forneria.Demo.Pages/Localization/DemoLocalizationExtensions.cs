using Microsoft.Extensions.DependencyInjection;
using Omni.Blazor.Localization;
using Omni.Localization;

namespace Forneria.Demo.Pages.Localization;

/// <summary>Application-owned resource for the component showcase shell and documentation.</summary>
public sealed class DemoResource;

/// <summary>Stable localization keys owned by the demo application.</summary>
public static class DemoTranslationKeys
{
    public const string Search = "Shell.Search";
    public const string Showcase = "Shell.Showcase";
    public const string Components = "Shell.Components";
    public const string Wireframes = "Shell.Wireframes";
    public const string LocalizationTitle = "Localization.Title";
    public const string LocalizationSubtitle = "Localization.Subtitle";
    public const string LocalizationOverview = "Localization.Overview";
    public const string LocalizationGlobalCulture = "Localization.GlobalCulture";
    public const string LocalizationGlobalCultureDescription = "Localization.GlobalCultureDescription";
    public const string LocalizationDemoLanguage = "Localization.DemoLanguage";
    public const string LocalizationIndependentCultures = "Localization.IndependentCultures";
    public const string LocalizationIndependentCulturesDescription = "Localization.IndependentCulturesDescription";
    public const string LocalizationRtl = "Localization.Rtl";
    public const string LocalizationRtlDescription = "Localization.RtlDescription";
    public const string LocalizationPseudo = "Localization.Pseudo";
    public const string LocalizationPseudoDescription = "Localization.PseudoDescription";
    public const string LocalizationAddLanguage = "Localization.AddLanguage";
    public const string LocalizationAddLanguageDescription = "Localization.AddLanguageDescription";
}

/// <summary>Registers both application-owned and component-library translations.</summary>
public static class DemoLocalizationExtensions
{
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        [DemoTranslationKeys.Search] = "Search",
        [DemoTranslationKeys.Showcase] = "Showcase",
        [DemoTranslationKeys.Components] = "Components",
        [DemoTranslationKeys.Wireframes] = "Wireframes",
        [DemoTranslationKeys.LocalizationTitle] = "Localization & RTL",
        [DemoTranslationKeys.LocalizationSubtitle] = "Extensible translations, culture-aware formatting, pluralization and RTL direction.",
        [DemoTranslationKeys.LocalizationOverview] = "The demo uses its own typed resource independently from the Omni.Blazor component catalog.",
        [DemoTranslationKeys.LocalizationGlobalCulture] = "Application culture",
        [DemoTranslationKeys.LocalizationGlobalCultureDescription] = "Persists the choice in a Server cookie or WebAssembly local storage and reloads the same route.",
        [DemoTranslationKeys.LocalizationDemoLanguage] = "Demo language",
        [DemoTranslationKeys.LocalizationIndependentCultures] = "Independent cultures in the same render",
        [DemoTranslationKeys.LocalizationIndependentCulturesDescription] = "OmniCultureScope separates formatting culture from UI culture and emits lang and dir.",
        [DemoTranslationKeys.LocalizationRtl] = "RTL inferred from culture",
        [DemoTranslationKeys.LocalizationRtlDescription] = "The ar-SA culture produces dir=rtl while controls keep the same API.",
        [DemoTranslationKeys.LocalizationPseudo] = "Pseudolocalization without a manual catalog",
        [DemoTranslationKeys.LocalizationPseudoDescription] = "en-XA expands text to expose clipping; ar-XB exercises bidi and RTL while preserving placeholders.",
        [DemoTranslationKeys.LocalizationAddLanguage] = "Add a language without rebuilding the library",
        [DemoTranslationKeys.LocalizationAddLanguageDescription] = "Application and library resources remain isolated and use the same deterministic fallback engine."
    };

    private static readonly Dictionary<string, string> Portuguese = new(StringComparer.Ordinal)
    {
        [DemoTranslationKeys.Search] = "Buscar",
        [DemoTranslationKeys.Showcase] = "Demonstração",
        [DemoTranslationKeys.Components] = "Componentes",
        [DemoTranslationKeys.Wireframes] = "Wireframes",
        [DemoTranslationKeys.LocalizationTitle] = "Localização e RTL",
        [DemoTranslationKeys.LocalizationSubtitle] = "Traduções extensíveis, formatação por cultura, pluralização e direção RTL.",
        [DemoTranslationKeys.LocalizationOverview] = "O site demo usa um recurso tipado próprio, independente do catálogo dos componentes Omni.Blazor.",
        [DemoTranslationKeys.LocalizationGlobalCulture] = "Cultura da aplicação",
        [DemoTranslationKeys.LocalizationGlobalCultureDescription] = "Persiste a escolha em cookie no Server ou localStorage no WebAssembly e recarrega a mesma rota.",
        [DemoTranslationKeys.LocalizationDemoLanguage] = "Idioma da demonstração",
        [DemoTranslationKeys.LocalizationIndependentCultures] = "Culturas independentes no mesmo render",
        [DemoTranslationKeys.LocalizationIndependentCulturesDescription] = "OmniCultureScope separa a cultura de formatação da cultura de UI e emite lang e dir.",
        [DemoTranslationKeys.LocalizationRtl] = "RTL inferido pela cultura",
        [DemoTranslationKeys.LocalizationRtlDescription] = "A cultura ar-SA produz dir=rtl enquanto os controles preservam a mesma API.",
        [DemoTranslationKeys.LocalizationPseudo] = "Pseudolocalização sem catálogo manual",
        [DemoTranslationKeys.LocalizationPseudoDescription] = "en-XA expande o texto para revelar cortes; ar-XB exercita bidi e RTL preservando placeholders.",
        [DemoTranslationKeys.LocalizationAddLanguage] = "Adicionar um idioma sem recompilar a biblioteca",
        [DemoTranslationKeys.LocalizationAddLanguageDescription] = "Os recursos da aplicação e da biblioteca ficam isolados e usam o mesmo fallback determinístico."
    };

    /// <summary>Adds demo-owned resources plus French and Arabic component overrides.</summary>
    public static IServiceCollection AddDemoLocalization(this IServiceCollection services)
    {
        services.AddOmniLocalizationResource<DemoResource>("pt-BR", "en", English);
        services.AddOmniTranslations<DemoResource>("pt-BR", Portuguese);
        services.AddOmniTranslations<DemoResource>("fr", new Dictionary<string, string>
        {
            [DemoTranslationKeys.Search] = "Rechercher",
            [DemoTranslationKeys.Showcase] = "Démonstration",
            [DemoTranslationKeys.Components] = "Composants",
            [DemoTranslationKeys.Wireframes] = "Maquettes",
            [DemoTranslationKeys.LocalizationTitle] = "Localisation et RTL",
            [DemoTranslationKeys.LocalizationSubtitle] = "Traductions extensibles, formatage culturel, pluriels et direction RTL.",
            [DemoTranslationKeys.LocalizationOverview] = "Le site de démonstration utilise sa propre ressource typée, indépendante du catalogue Omni.Blazor.",
            [DemoTranslationKeys.LocalizationGlobalCulture] = "Culture de l’application",
            [DemoTranslationKeys.LocalizationGlobalCultureDescription] = "Conserve le choix dans un cookie Server ou le stockage local WebAssembly et recharge la même route.",
            [DemoTranslationKeys.LocalizationDemoLanguage] = "Langue de la démonstration",
            [DemoTranslationKeys.LocalizationIndependentCultures] = "Cultures indépendantes dans le même rendu",
            [DemoTranslationKeys.LocalizationIndependentCulturesDescription] = "OmniCultureScope sépare la culture de formatage de la culture d’interface et émet lang et dir.",
            [DemoTranslationKeys.LocalizationRtl] = "RTL déduit de la culture",
            [DemoTranslationKeys.LocalizationRtlDescription] = "La culture ar-SA produit dir=rtl sans modifier l’API des contrôles.",
            [DemoTranslationKeys.LocalizationPseudo] = "Pseudolocalisation sans catalogue manuel",
            [DemoTranslationKeys.LocalizationPseudoDescription] = "en-XA étend le texte pour révéler les coupures ; ar-XB vérifie bidi et RTL en préservant les paramètres.",
            [DemoTranslationKeys.LocalizationAddLanguage] = "Ajouter une langue sans reconstruire la bibliothèque",
            [DemoTranslationKeys.LocalizationAddLanguageDescription] = "Les ressources de l’application et de la bibliothèque restent isolées et utilisent le même fallback déterministe."
        });
        services.AddOmniTranslations<DemoResource>("ar", new Dictionary<string, string>
        {
            [DemoTranslationKeys.Search] = "بحث",
            [DemoTranslationKeys.Showcase] = "عرض",
            [DemoTranslationKeys.Components] = "المكونات",
            [DemoTranslationKeys.Wireframes] = "المخططات",
            [DemoTranslationKeys.LocalizationTitle] = "التعريب واتجاه RTL",
            [DemoTranslationKeys.LocalizationSubtitle] = "ترجمات قابلة للتوسعة وتنسيق حسب الثقافة وصيغ جمع واتجاه RTL.",
            [DemoTranslationKeys.LocalizationOverview] = "يستخدم موقع العرض مورداً مستقلاً عن كتالوج مكونات Omni.Blazor.",
            [DemoTranslationKeys.LocalizationGlobalCulture] = "ثقافة التطبيق",
            [DemoTranslationKeys.LocalizationGlobalCultureDescription] = "يحفظ الاختيار في ملف تعريف ارتباط للخادم أو التخزين المحلي لـ WebAssembly ثم يعيد تحميل المسار نفسه.",
            [DemoTranslationKeys.LocalizationDemoLanguage] = "لغة العرض",
            [DemoTranslationKeys.LocalizationIndependentCultures] = "ثقافات مستقلة في العرض نفسه",
            [DemoTranslationKeys.LocalizationIndependentCulturesDescription] = "يفصل OmniCultureScope ثقافة التنسيق عن ثقافة الواجهة ويحدد lang وdir.",
            [DemoTranslationKeys.LocalizationRtl] = "استنتاج RTL من الثقافة",
            [DemoTranslationKeys.LocalizationRtlDescription] = "تنتج ثقافة ar-SA الاتجاه rtl مع بقاء واجهة عناصر التحكم كما هي.",
            [DemoTranslationKeys.LocalizationPseudo] = "تعريب زائف دون كتالوج يدوي",
            [DemoTranslationKeys.LocalizationPseudoDescription] = "يوسع en-XA النص لاكتشاف القص ويختبر ar-XB اتجاه RTL مع الحفاظ على المعلمات.",
            [DemoTranslationKeys.LocalizationAddLanguage] = "إضافة لغة دون إعادة بناء المكتبة",
            [DemoTranslationKeys.LocalizationAddLanguageDescription] = "تبقى موارد التطبيق والمكتبة معزولة وتستخدم آلية fallback حتمية واحدة."
        });
        services.AddOmniPseudoLocalization<DemoResource>();

        Omni.Blazor.Localization.OmniLocalizationServiceCollectionExtensions.AddOmniTranslations(
            services,
            "fr",
            new Dictionary<string, string>
            {
                [OmniTranslationKeys.Close] = "Fermer",
                [OmniTranslationKeys.Save] = "Enregistrer",
                [OmniTranslationKeys.Clear] = "Effacer",
                [OmniTranslationKeys.Today] = "Aujourd’hui",
                [OmniTranslationKeys.OpenCalendar] = "Ouvrir le calendrier",
                [OmniTranslationKeys.MessagePlaceholder] = "Écrivez un message…",
                [OmniTranslationKeys.Send] = "Envoyer"
            });
        Omni.Blazor.Localization.OmniLocalizationServiceCollectionExtensions.AddOmniTranslations(
            services,
            "ar",
            new Dictionary<string, string>
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
