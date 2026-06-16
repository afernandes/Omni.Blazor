namespace Omni.Blazor.Components;

/// <summary>
/// Inline SVG icon catalog (Lucide-style, stroke 1.8) so the library has zero
/// external font/icon dependencies. Path data is intentionally compact —
/// each icon is referenced by lowercase kebab name.
/// </summary>
public static class OmniIconLibrary
{
    // Each path is the inner content of a 24x24 SVG.
    // stroke="currentColor" fill="none" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
    private static readonly Dictionary<string, string> _icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["menu"]               = """<path d="M4 6h16M4 12h16M4 18h16"/>""",
        ["x"]                  = """<path d="M6 6l12 12M18 6l-12 12"/>""",
        ["chevron-right"]      = """<path d="M9 6l6 6-6 6"/>""",
        ["chevron-left"]       = """<path d="M15 6l-6 6 6 6"/>""",
        ["chevron-down"]       = """<path d="M6 9l6 6 6-6"/>""",
        ["chevron-up"]         = """<path d="M6 15l6-6 6 6"/>""",
        ["chevrons-right"]     = """<path d="M6 6l6 6-6 6M12 6l6 6-6 6"/>""",
        ["chevrons-left"]      = """<path d="M18 6l-6 6 6 6M12 6l-6 6 6 6"/>""",
        ["chevrons-down"]      = """<path d="M6 6l6 6 6-6M6 12l6 6 6-6"/>""",
        ["chevrons-up"]        = """<path d="M6 18l6-6 6 6M6 12l6-6 6 6"/>""",
        ["arrow-right"]        = """<path d="M5 12h14M13 6l6 6-6 6"/>""",
        ["arrow-left"]         = """<path d="M19 12H5M11 6l-6 6 6 6"/>""",
        ["arrow-up"]           = """<path d="M12 19V5M6 11l6-6 6 6"/>""",
        ["arrow-down"]         = """<path d="M12 5v14M18 13l-6 6-6-6"/>""",
        ["check"]              = """<path d="M5 12l5 5L20 7"/>""",
        ["plus"]               = """<path d="M12 5v14M5 12h14"/>""",
        ["minus"]              = """<path d="M5 12h14"/>""",
        ["search"]             = """<circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3"/>""",
        ["bell"]               = """<path d="M6 8a6 6 0 0 1 12 0c0 7 3 8 3 8H3s3-1 3-8"/><path d="M9 19a3 3 0 0 0 6 0"/>""",
        ["user"]               = """<circle cx="12" cy="8" r="4"/><path d="M4 21v-2a4 4 0 0 1 4-4h8a4 4 0 0 1 4 4v2"/>""",
        ["users"]              = """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>""",
        ["settings"]           = """<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>""",
        ["home"]               = """<path d="M3 11l9-8 9 8"/><path d="M5 10v10h14V10"/>""",
        ["layout-dashboard"]   = """<rect x="3" y="3" width="7" height="9" rx="1"/><rect x="14" y="3" width="7" height="5" rx="1"/><rect x="14" y="12" width="7" height="9" rx="1"/><rect x="3" y="16" width="7" height="5" rx="1"/>""",
        ["database"]           = """<ellipse cx="12" cy="5" rx="9" ry="3"/><path d="M3 5v6c0 1.66 4 3 9 3s9-1.34 9-3V5"/><path d="M3 11v6c0 1.66 4 3 9 3s9-1.34 9-3v-6"/>""",
        ["shopping-cart"]      = """<circle cx="9" cy="21" r="1"/><circle cx="20" cy="21" r="1"/><path d="M1 1h4l2.7 13.4a2 2 0 0 0 2 1.6h9.7a2 2 0 0 0 2-1.6L23 6H6"/>""",
        ["package"]            = """<path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.27 6.96L12 12.01l8.73-5.05M12 22.08V12"/>""",
        ["calendar"]           = """<rect x="3" y="4" width="18" height="18" rx="2"/><path d="M16 2v4M8 2v4M3 10h18"/>""",
        ["credit-card"]        = """<rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/>""",
        ["file-text"]          = """<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><path d="M14 2v6h6M9 13h6M9 17h6"/>""",
        ["star"]               = """<polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>""",
        ["heart"]              = """<path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 1 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>""",
        ["log-out"]            = """<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="M16 17l5-5-5-5M21 12H9"/>""",
        ["log-in"]             = """<path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/><path d="M10 17l5-5-5-5M15 12H3"/>""",
        ["edit"]               = """<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>""",
        ["trash"]              = """<path d="M3 6h18M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6M10 11v6M14 11v6M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>""",
        ["filter"]             = """<polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"/>""",
        ["download"]           = """<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3"/>""",
        ["upload"]             = """<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12"/>""",
        ["alert-triangle"]     = """<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><path d="M12 9v4M12 17h.01"/>""",
        ["alert-circle"]       = """<circle cx="12" cy="12" r="10"/><path d="M12 8v4M12 16h.01"/>""",
        ["check-circle"]       = """<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><path d="M22 4l-10 10-3-3"/>""",
        ["info"]               = """<circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/>""",
        ["help-circle"]        = """<circle cx="12" cy="12" r="10"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3M12 17h.01"/>""",
        ["x-circle"]           = """<circle cx="12" cy="12" r="10"/><path d="m15 9-6 6M9 9l6 6"/>""",
        ["shield"]             = """<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>""",
        ["ban"]                = """<circle cx="12" cy="12" r="10"/><path d="m4.9 4.9 14.2 14.2"/>""",
        ["inbox"]              = """<path d="M22 12h-6l-2 3h-4l-2-3H2"/><path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/>""",
        ["more-horizontal"]    = """<circle cx="5" cy="12" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/>""",
        ["more-vertical"]      = """<circle cx="12" cy="5" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="12" cy="19" r="1"/>""",
        ["external-link"]      = """<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/><path d="M15 3h6v6M10 14L21 3"/>""",
        ["mail"]               = """<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><path d="M22 6l-10 7L2 6"/>""",
        ["phone"]              = """<path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.86 19.86 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.86 19.86 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 13 13 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 13 13 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>""",
        ["lock"]               = """<rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>""",
        ["unlock"]             = """<rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 9.9-1"/>""",
        ["eye"]                = """<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>""",
        ["eye-off"]            = """<path d="M17.94 17.94A10.94 10.94 0 0 1 12 20c-7 0-11-8-11-8a18.46 18.46 0 0 1 5.06-5.94"/><path d="M9.9 4.24A10.94 10.94 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24M1 1l22 22"/>""",
        ["clock"]              = """<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>""",
        ["tag"]                = """<path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><circle cx="7" cy="7" r="1.5"/>""",
        ["zap"]                = """<polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>""",
        ["sun"]                = """<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/>""",
        ["moon"]               = """<path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>""",
        // Monitor — usado pelo AppearanceToggle pra modo "System" (segue OS).
        ["monitor"]            = """<rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>""",
        ["pizza"]              = """<path d="M12 2L2 22h20z"/><circle cx="10" cy="14" r="0.7"/><circle cx="14" cy="14" r="0.7"/><circle cx="12" cy="18" r="0.7"/>""",
        ["circle"]             = """<circle cx="12" cy="12" r="10"/>""",
        ["circle-dot"]         = """<circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="2.5" fill="currentColor"/>""",
        ["droplet"]            = """<path d="M12 2.5c-3 4-7 8-7 12a7 7 0 0 0 14 0c0-4-4-8-7-12z"/>""",
        ["percent"]            = """<line x1="19" y1="5" x2="5" y2="19"/><circle cx="6.5" cy="6.5" r="2.5"/><circle cx="17.5" cy="17.5" r="2.5"/>""",
        ["coffee"]             = """<path d="M18 8h1a4 4 0 0 1 0 8h-1"/><path d="M2 8h16v9a4 4 0 0 1-4 4H6a4 4 0 0 1-4-4z"/><path d="M6 1v3M10 1v3M14 1v3"/>""",
        ["bar-chart"]          = """<line x1="12" y1="20" x2="12" y2="10"/><line x1="18" y1="20" x2="18" y2="4"/><line x1="6" y1="20" x2="6" y2="16"/>""",
        ["trending-up"]        = """<polyline points="23 6 13.5 15.5 8.5 10.5 1 18"/><polyline points="17 6 23 6 23 12"/>""",
        ["grid"]               = """<rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/>""",
        ["list"]               = """<line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/><line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/>""",
        ["save"]               = """<path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/>""",
        ["refresh"]            = """<polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>""",
        ["copy"]               = """<rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>""",
        ["dollar-sign"]        = """<line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>""",
        ["map-pin"]            = """<path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/>""",
        ["printer"]            = """<polyline points="6 9 6 2 18 2 18 9"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/>""",
        // Audio / speech (Lucide mic + stop-circle + mic-off pra estado disabled)
        ["mic"]                = """<path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/><line x1="12" y1="19" x2="12" y2="23"/><line x1="8" y1="23" x2="16" y2="23"/>""",
        ["mic-off"]            = """<line x1="1" y1="1" x2="23" y2="23"/><path d="M9 9v3a3 3 0 0 0 5.12 2.12M15 9.34V5a3 3 0 0 0-5.94-.6"/><path d="M17 16.95A7 7 0 0 1 5 12v-2m14 0v2a7 7 0 0 1-.11 1.23"/><line x1="12" y1="19" x2="12" y2="23"/><line x1="8" y1="23" x2="16" y2="23"/>""",
        ["stop-circle"]        = """<circle cx="12" cy="12" r="10"/><rect x="9" y="9" width="6" height="6"/>""",
        ["volume-2"]           = """<polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"/><path d="M15.54 8.46a5 5 0 0 1 0 7.07M19.07 4.93a10 10 0 0 1 0 14.14"/>""",
        ["square"]             = """<rect x="3" y="3" width="18" height="18" rx="2"/>""",
        // Loader — Lucide spinner shape: 12 radiating lines de comprimento crescente.
        // CSS pode animar via `animation: spin`. Usado em estados de transição
        // (Starting/Stopping do Speech-to-Text).
        ["loader"]             = """<line x1="12" y1="2" x2="12" y2="6"/><line x1="12" y1="18" x2="12" y2="22"/><line x1="4.93" y1="4.93" x2="7.76" y2="7.76"/><line x1="16.24" y1="16.24" x2="19.07" y2="19.07"/><line x1="2" y1="12" x2="6" y2="12"/><line x1="18" y1="12" x2="22" y2="12"/><line x1="4.93" y1="19.07" x2="7.76" y2="16.24"/><line x1="16.24" y1="7.76" x2="19.07" y2="4.93"/>""",
        // Storefront — awning + body + door. Used by the inventory / store
        // section of admin sidebars.
        ["store"]              = """<path d="M3 9h18l-2-5H5zM4 9v12h16V9M10 21v-6h4v6"/>""",
        // Delivery truck — cab + cargo body + wheels.
        ["truck"]              = """<path d="M1 3h15v13H1z"/><path d="M16 8h4l3 3v5h-7z"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/>""",
        // Sliders — three horizontal tracks with knobs (settings/preferences).
        ["sliders"]            = """<line x1="4" y1="6" x2="20" y2="6"/><line x1="4" y1="12" x2="20" y2="12"/><line x1="4" y1="18" x2="20" y2="18"/><circle cx="14" cy="6" r="2"/><circle cx="8" cy="12" r="2"/><circle cx="16" cy="18" r="2"/>""",
        // Sparkles — used for "new" / promotional accents.
        ["sparkle"]            = """<path d="M12 3l1.5 5L19 9.5 13.5 11 12 16l-1.5-5L5 9.5 10.5 8 12 3z"/><path d="M19 16l.7 2.3L22 19l-2.3.7L19 22l-.7-2.3L16 19l2.3-.7L19 16z"/>""",
        // Command key glyph — for keyboard shortcuts.
        ["command"]            = """<path d="M18 3a3 3 0 0 0-3 3v12a3 3 0 0 0 3 3 3 3 0 0 0 3-3 3 3 0 0 0-3-3H6a3 3 0 0 0-3 3 3 3 0 0 0 3 3 3 3 0 0 0 3-3V6a3 3 0 0 0-3-3 3 3 0 0 0-3 3 3 3 0 0 0 3 3h12a3 3 0 0 0 3-3 3 3 0 0 0-3-3z"/>""",

        // Text formatting (rich-text/markdown toolbars).
        ["bold"]               = """<path d="M14 12a4 4 0 0 0 0-8H6v8"/><path d="M15 20a4 4 0 0 0 0-8H6v8z"/>""",
        ["italic"]             = """<path d="M19 4h-9M14 20H5M15 4L9 20"/>""",
        ["underline"]          = """<path d="M6 4v6a6 6 0 0 0 12 0V4M4 20h16"/>""",
        ["strikethrough"]      = """<path d="M16 4H9a3 3 0 0 0-2.83 4M14 12a4 4 0 0 1 0 8H6M4 12h16"/>""",
        ["code"]               = """<polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>""",
        ["layout-grid"]        = """<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/>""",
        ["circle-check"]       = """<circle cx="12" cy="12" r="10"/><path d="M9 12l2 2 4-4"/>""",

        // Text alignment (block/paragraph editors).
        ["align-left"]         = """<path d="M21 6H3M15 12H3M17 18H3"/>""",
        ["align-center"]       = """<path d="M21 6H3M17 12H7M19 18H5"/>""",
        ["align-right"]        = """<path d="M21 6H3M21 12H9M21 18H7"/>""",
        ["align-justify"]      = """<path d="M21 6H3M21 12H3M21 18H3"/>""",

        // List variants (rich-text editor companions to align-*).
        ["list-ordered"]       = """<line x1="10" y1="6" x2="21" y2="6"/><line x1="10" y1="12" x2="21" y2="12"/><line x1="10" y1="18" x2="21" y2="18"/><path d="M4 6h1v4M4 10h2M6 18H4c0-1 2-2 2-3s-1-1.5-2-1"/>""",
        ["list-unordered"]     = """<line x1="9" y1="6" x2="21" y2="6"/><line x1="9" y1="12" x2="21" y2="12"/><line x1="9" y1="18" x2="21" y2="18"/><circle cx="4" cy="6" r="1"/><circle cx="4" cy="12" r="1"/><circle cx="4" cy="18" r="1"/>""",

        // Archive (alternative para trash quando a ação é "guardar", não "destruir").
        ["archive"]            = """<polyline points="21 8 21 21 3 21 3 8"/><rect x="1" y="3" width="22" height="5" rx="1"/><line x1="10" y1="12" x2="14" y2="12"/>""",

        // Layers / Link / File alt — completam o "kit comum" de toolbar.
        ["layers"]             = """<polygon points="12 2 2 7 12 12 22 7 12 2"/><polyline points="2 17 12 22 22 17"/><polyline points="2 12 12 17 22 12"/>""",
        ["link"]               = """<path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.72-1.71"/>""",
        ["file"]               = """<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/>""",
        // Media / comm icons — usados por FAB showcase (compor, mídia, etc.)
        ["folder"]             = """<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>""",
        ["image"]              = """<rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>""",
        ["message-square"]     = """<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>""",
        ["camera"]             = """<path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/><circle cx="12" cy="13" r="4"/>""",
        ["video"]              = """<polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/>""",
        ["arrow-up-right"]     = """<line x1="7" y1="17" x2="17" y2="7"/><polyline points="7 7 17 7 17 17"/>""",

        // Transport / canvas controls (diagram editors, players, debuggers).
        ["play"]               = """<polygon points="6 4 20 12 6 20 6 4" fill="currentColor" stroke="none"/>""",
        ["play-line"]          = """<polygon points="6 4 20 12 6 20 6 4"/>""",
        ["pause"]              = """<rect x="6" y="5" width="4" height="14" rx="1" fill="currentColor" stroke="none"/><rect x="14" y="5" width="4" height="14" rx="1" fill="currentColor" stroke="none"/>""",
        ["pause-circle"]       = """<circle cx="12" cy="12" r="10"/><line x1="10" y1="15" x2="10" y2="9"/><line x1="14" y1="15" x2="14" y2="9"/>""",
        ["stop"]               = """<rect x="6" y="6" width="12" height="12" rx="2" fill="currentColor" stroke="none"/>""",
        ["step"]               = """<polygon points="5 4 15 12 5 20 5 4" fill="currentColor" stroke="none"/><line x1="19" y1="5" x2="19" y2="19"/>""",
        ["undo"]               = """<path d="M3 7v6h6"/><path d="M3 13a9 9 0 1 0 3-7.7L3 8"/>""",
        ["redo"]               = """<path d="M21 7v6h-6"/><path d="M21 13a9 9 0 1 1-3-7.7L21 8"/>""",
        ["zoom-in"]            = """<circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3M11 8v6M8 11h6"/>""",
        ["zoom-out"]           = """<circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3M8 11h6"/>""",
        ["maximize"]           = """<path d="M8 3H5a2 2 0 0 0-2 2v3M21 8V5a2 2 0 0 0-2-2h-3M16 21h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3"/>""",
        ["layout"]             = """<path d="M3 6h7v12H3zM14 6h7v5h-7zM14 15h7v3h-7z"/>""",
        ["grip"]               = """<circle cx="9" cy="6" r="1"/><circle cx="15" cy="6" r="1"/><circle cx="9" cy="12" r="1"/><circle cx="15" cy="12" r="1"/><circle cx="9" cy="18" r="1"/><circle cx="15" cy="18" r="1"/>""",

        // Flow / graph glyphs (workflow designers, pipelines).
        ["workflow"]           = """<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/><path d="M10 6.5h4a2 2 0 0 1 2 2V14"/>""",
        ["activity"]           = """<polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>""",
        ["git-branch"]         = """<line x1="6" y1="3" x2="6" y2="15"/><circle cx="18" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M18 9a9 9 0 0 1-9 9"/>""",
        ["shuffle"]            = """<path d="M16 3h5v5M4 20L21 3M21 16v5h-5M15 15l6 6M4 4l5 5"/>""",
        ["repeat"]             = """<path d="M17 1l4 4-4 4"/><path d="M3 11V9a4 4 0 0 1 4-4h14"/><path d="M7 23l-4-4 4-4"/><path d="M21 13v2a4 4 0 0 1-4 4H3"/>""",
        ["parallel"]           = """<rect x="3" y="4" width="4" height="16" rx="1"/><rect x="10" y="4" width="4" height="16" rx="1"/><rect x="17" y="4" width="4" height="16" rx="1"/>""",
        ["merge"]              = """<path d="M8 18L18 8M18 8h-6M18 8v6"/><circle cx="6" cy="6" r="3"/><circle cx="6" cy="18" r="3"/>""",
        ["timer"]              = """<line x1="10" y1="2" x2="14" y2="2"/><circle cx="12" cy="14" r="8"/><line x1="12" y1="14" x2="15" y2="11"/>""",
        ["flag"]               = """<path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/><line x1="4" y1="22" x2="4" y2="15"/>""",
        ["flag-checkered"]     = """<path d="M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z"/><line x1="4" y1="22" x2="4" y2="15"/>""",
        ["globe"]              = """<circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>""",
        ["send"]               = """<line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/>""",
        ["message-circle"]     = """<path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8z"/>""",
        ["variable"]           = """<path d="M8 21s-4-3-4-9 4-9 4-9M16 3s4 3 4 9-4 9-4 9"/><path d="M15 9l-6 6M9 9l6 6"/>""",
        ["braces"]             = """<path d="M8 3H7a2 2 0 0 0-2 2v5a2 2 0 0 1-2 2 2 2 0 0 1 2 2v5a2 2 0 0 0 2 2h1"/><path d="M16 3h1a2 2 0 0 1 2 2v5a2 2 0 0 0 2 2 2 2 0 0 0-2 2v5a2 2 0 0 1-2 2h-1"/>""",
        ["receipt"]            = """<path d="M4 2v20l2-1 2 1 2-1 2 1 2-1 2 1 2-1 2 1V2l-2 1-2-1-2 1-2-1-2 1-2-1-2 1z"/><path d="M8 7h8M8 11h8M8 15h5"/>""",
        ["chef-hat"]           = """<path d="M6 13.87V19a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1v-5.13"/><path d="M17 11a3 3 0 1 0 0-6 4 4 0 0 0-7.6-1.5A3.5 3.5 0 1 0 7 11"/><path d="M6 14h12"/>""",
        ["flame"]              = """<path d="M8.5 14.5A2.5 2.5 0 0 0 11 12c0-1.38-.5-2-1-3-1.072-2.143-.224-4.054 2-6 .5 2.5 2 4.9 4 6.5 2 1.6 3 3.5 3 5.5a7 7 0 1 1-14 0c0-1.153.433-2.294 1-3a2.5 2.5 0 0 0 2.5 2.5z"/>""",
        ["webhook"]            = """<path d="M18 16.98h-5.99c-1.1 0-1.95.94-2.48 1.9A4 4 0 0 1 2 17a4 4 0 0 1 7.52-1.9"/><path d="M6 17l3.13-5.78c.53-.97.1-2.18-.5-3.1a4 4 0 1 1 6.89-4.06"/><path d="M12 6l3.13 5.73C15.66 12.7 16.9 13 18 13a4 4 0 1 1-3.92 4.74"/>""",
        ["terminal"]           = """<polyline points="4 17 10 11 4 5"/><line x1="12" y1="19" x2="20" y2="19"/>""",
        ["scroll-text"]        = """<path d="M8 21h12a2 2 0 0 0 2-2v-2H10v2a2 2 0 1 1-4 0V5a2 2 0 1 0-4 0v3h4"/><path d="M19 17V5a2 2 0 0 0-2-2H4M15 8h-5M15 12h-5"/>""",
        ["box"]                = """<path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><path d="M3.27 6.96L12 12.01l8.73-5.05M12 22.08V12"/>""",
    };

    public static string? GetPath(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _icons.TryGetValue(name, out var path) ? path : null;
    }

    public static IEnumerable<string> Names => _icons.Keys;
}
