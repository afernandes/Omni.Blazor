using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Runtime theme toggle (accent + dark mode). Applies the <c>data-accent</c> and
/// <c>data-theme</c> attributes on <c>&lt;html&gt;</c> so any CSS variable
/// override under those selectors takes effect immediately.
///
/// <para>
/// Initialization is two-phase:
/// </para>
/// <list type="number">
/// <item><description>The inline script in <c>OmniTheme</c> runs first (in &lt;head&gt;)
/// and applies the right accent/dark BEFORE the body paints — preventing FOUC.
/// It reads localStorage, then <c>prefers-color-scheme</c> as fallback.</description></item>
/// <item><description><see cref="InitializeAsync"/> runs after Blazor connects:
/// reads back what the inline script applied (so the service is in sync with
/// the DOM), and subscribes to <c>prefers-color-scheme</c> changes so the app
/// follows the OS when the user hasn't picked manually.</description></item>
/// </list>
/// </summary>
public class ThemeService : IAsyncDisposable
{
    private const string StorageKey = "omni.theme";

    private readonly IJSRuntime _js;
    private DotNetObjectReference<ThemeService>? _selfRef;
    private bool _colorSchemeSubscribed;
    private bool _userPicked;     // true when user explicitly chose (persists to localStorage)
    private bool _disposed;

    public string Accent { get; private set; } = "amber";
    public bool   Dark   { get; private set; }
    public LayoutDensity Density { get; private set; } = LayoutDensity.Comfortable;
    public bool   IsInitialized { get; private set; }

    public event Action? OnChange;

    public ThemeService(IJSRuntime js) { _js = js; }

    /// <summary>
    /// Restore previously saved theme from localStorage and apply it. Safe to
    /// call multiple times — subsequent calls are no-ops once initialized.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            var saved = await _js.InvokeAsync<string?>("omniBlazor.storageGet", StorageKey);
            if (!string.IsNullOrEmpty(saved))
            {
                _userPicked = true;
                var parts = saved.Split('|');
                if (parts.Length >= 1 && !string.IsNullOrEmpty(parts[0])) Accent = parts[0];
                if (parts.Length >= 2) Dark = parts[1] == "1";
                if (parts.Length >= 3 && Enum.TryParse<LayoutDensity>(parts[2], true, out var d)) Density = d;
                await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-accent", Accent);
                await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-theme", Dark ? "dark" : null);
                await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-density",
                    Density == LayoutDensity.Comfortable ? null : Density.ToString().ToLowerInvariant());
            }
            else
            {
                _userPicked = false;
                // Adopt whatever the inline script already applied (localStorage
                // was empty, so the script used prefers-color-scheme). Reading
                // back from DOM keeps the service in sync with the initial paint.
                var domAccent = await _js.InvokeAsync<string?>("omniBlazor.getAttr", "html", "data-accent");
                var domTheme  = await _js.InvokeAsync<string?>("omniBlazor.getAttr", "html", "data-theme");
                if (!string.IsNullOrEmpty(domAccent)) Accent = domAccent;
                Dark = domTheme == "dark";

                // Subscribe to OS color-scheme changes so the app follows the
                // system theme until the user makes a manual choice (which will
                // unsubscribe below). The handler fires when the user toggles
                // dark/light in Settings while the app is running.
                await SubscribeColorSchemeAsync();
            }
        }
        catch
        {
            // Prerender / no-JS scenarios — fall back silently to defaults.
        }
        finally
        {
            IsInitialized = true;
            OnChange?.Invoke();
        }
    }

    public async Task SetAccentAsync(string accent)
    {
        Accent = accent;
        _userPicked = true;
        await UnsubscribeColorSchemeAsync(); // Once user picks, stop following the OS.
        await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-accent", accent);
        await PersistAsync();
        OnChange?.Invoke();
    }

    public async Task SetDarkAsync(bool dark)
    {
        Dark = dark;
        _userPicked = true;
        await UnsubscribeColorSchemeAsync(); // Once user picks, stop following the OS.
        await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-theme", dark ? "dark" : null);
        await PersistAsync();
        OnChange?.Invoke();
    }

    public async Task ToggleDarkAsync() => await SetDarkAsync(!Dark);

    /// <summary>Define a densidade global do layout (Compact, Comfortable,
    /// Spacious). Aplica <c>data-density</c> em <c>&lt;html&gt;</c> e
    /// persiste em localStorage. Comfortable é o default — não emite atributo
    /// (mantém os tokens base). Padrão Salesforce/Material design density.</summary>
    public async Task SetDensityAsync(LayoutDensity density)
    {
        Density = density;
        _userPicked = true;
        var value = density == LayoutDensity.Comfortable ? null : density.ToString().ToLowerInvariant();
        try { await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-density", value); }
        catch { /* SSR */ }
        await PersistAsync();
        OnChange?.Invoke();
    }

    /// <summary>
    /// Stop following the user's explicit preference and revert to the OS's
    /// prefers-color-scheme. Clears the localStorage key and re-subscribes
    /// to the OS color-scheme observer. Useful for a "Reset to system default"
    /// option in a settings menu.
    /// </summary>
    public async Task UseSystemColorSchemeAsync()
    {
        try
        {
            _userPicked = false;
            await _js.InvokeVoidAsync("omniBlazor.storageRemove", StorageKey);
            // Read current OS preference and apply.
            var prefersDark = await _js.InvokeAsync<bool>("omniBlazor.prefersColorSchemeDark");
            Dark = prefersDark;
            await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-theme", Dark ? "dark" : null);
            await SubscribeColorSchemeAsync();
            OnChange?.Invoke();
        }
        catch { /* SSR / no-JS */ }
    }

    /// <summary>Indicates whether the user explicitly picked a theme (true) or
    /// is following the OS's <c>prefers-color-scheme</c> (false).</summary>
    public bool IsUserPicked => _userPicked;

    /// <summary>Invoked by JS when the OS color-scheme changes. Only active
    /// while the user has NOT made an explicit pick.</summary>
    [JSInvokable]
    public async Task OnColorSchemeChanged(bool prefersDark)
    {
        if (_userPicked || _disposed) return;
        if (Dark == prefersDark) return;
        Dark = prefersDark;
        try { await _js.InvokeVoidAsync("omniBlazor.setAttr", "html", "data-theme", Dark ? "dark" : null); }
        catch { /* JS gone */ }
        OnChange?.Invoke();
    }

    private async Task SubscribeColorSchemeAsync()
    {
        if (_colorSchemeSubscribed || _disposed) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            await _js.InvokeAsync<bool>("omniBlazor.subscribeColorScheme",
                "themeService", _selfRef, nameof(OnColorSchemeChanged));
            _colorSchemeSubscribed = true;
        }
        catch { /* SSR */ }
    }

    private async Task UnsubscribeColorSchemeAsync()
    {
        if (!_colorSchemeSubscribed) return;
        try { await _js.InvokeVoidAsync("omniBlazor.unsubscribeColorScheme", "themeService"); }
        catch { /* JS gone */ }
        _colorSchemeSubscribed = false;
    }

    private async Task PersistAsync()
    {
        try
        {
            // Format: accent|dark|density (densidade adicionada em P3.3).
            // Migration-safe: leituras antigas (2 partes) caem no default
            // Density=Comfortable; gravações novas (3 partes) são lidas
            // corretamente pelo Parse no InitializeAsync.
            var value = $"{Accent}|{(Dark ? "1" : "0")}|{Density.ToString().ToLowerInvariant()}";
            await _js.InvokeVoidAsync("omniBlazor.storageSet", StorageKey, value);
        }
        catch
        {
            // Silently ignore in prerender / no-JS scenarios.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await UnsubscribeColorSchemeAsync();
        _selfRef?.Dispose();
        _selfRef = null;
    }
}
