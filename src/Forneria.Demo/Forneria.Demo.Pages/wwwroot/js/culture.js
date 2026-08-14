const storageKey = "omni-showcase-culture";

export function getCulture() {
    try {
        return globalThis.localStorage?.getItem(storageKey) ?? null;
    } catch {
        return null;
    }
}

export function setCulture(cultureName) {
    try {
        globalThis.localStorage?.setItem(storageKey, cultureName);
    } catch {
        // Storage may be disabled by the browser. The active reload still uses
        // the host default; no partially stored value is left behind.
    }
}

export function applyDocumentCulture(cultureName, rightToLeft) {
    document.documentElement.lang = cultureName;
    document.documentElement.dir = rightToLeft ? "rtl" : "ltr";
}
