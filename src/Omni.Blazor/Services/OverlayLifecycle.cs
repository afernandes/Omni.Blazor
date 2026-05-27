using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>
/// Encapsula o ciclo de vida "overlay ativo" compartilhado por
/// <c>OmniOverlay</c>, <c>OmniDrawer</c>, <c>OmniSplitView</c> e
/// <c>OmniDialogHost</c>: body scroll lock + JS interop pra setup/teardown
/// do overlay (focus trap + Esc handler + autofocus inteligente + stack-aware
/// key handling).
///
/// <para>Antes desse helper, cada um desses 4 componentes tinha ~30 linhas
/// idênticas de <c>Activate/DeactivateOverlayAsync</c>. Agora a lógica vive
/// num lugar só — bug fixes ficam em 1 ponto, comportamento não pode
/// driftear entre componentes.</para>
///
/// <para>Uso típico (em componente Blazor):</para>
/// <code>
/// private OverlayLifecycle? _overlay;
/// private DotNetObjectReference&lt;MyComponent&gt;? _selfRef;
///
/// protected override void OnInitialized()
/// {
///     _overlay = new OverlayLifecycle(JS, Scroll, Id);
/// }
///
/// private async Task OpenAsync()
/// {
///     // ... your open logic ...
///     _selfRef ??= DotNetObjectReference.Create(this);
///     await _overlay!.ActivateAsync(_selfRef);
/// }
///
/// private async Task CloseAsync()
/// {
///     // ... your close logic ...
///     await _overlay!.DeactivateAsync();
/// }
///
/// [JSInvokable]
/// public Task OnEscape() =&gt; CloseAsync();
///
/// public async ValueTask DisposeAsync()
/// {
///     if (_overlay is not null) await _overlay.DisposeAsync();
///     _selfRef?.Dispose();
/// }
/// </code>
///
/// <para>O que o helper NÃO faz (responsabilidade do componente):</para>
/// <list type="bullet">
///   <item>Gerenciar visibilidade (Open/Close state)</item>
///   <item>Renderizar backdrop / scrim — isso é CSS do componente</item>
///   <item>LocationChanged auto-close — só Drawer/SplitView fazem</item>
///   <item>BreakpointService subscription — específico de cada componente</item>
///   <item>Stack management de dialogs — responsabilidade do DialogService</item>
///   <item>Criar o DotNetObjectReference — componente cria, helper só usa</item>
/// </list>
/// </summary>
public sealed class OverlayLifecycle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ScrollManager _scroll;
    private readonly string _id;
    private readonly string _selector;
    private readonly string _scrollTarget;
    private bool _isActive;

    /// <summary>True quando o overlay está com scroll lock + JS handlers ativos.</summary>
    public bool IsActive => _isActive;

    /// <param name="js">JSRuntime injetado no componente.</param>
    /// <param name="scroll">ScrollManager injetado no componente (singleton).</param>
    /// <param name="id">Id único do overlay — mesmo passado pro elemento DOM.
    /// Usado como chave no overlayStack do JS.</param>
    /// <param name="selector">Selector CSS pra encontrar o elemento overlay no DOM.
    /// Default <c>"#" + id</c> (assume que o elemento tem <c>id="@Id"</c>).</param>
    /// <param name="scrollTarget">Elemento alvo do scroll lock. Default <c>"html"</c>
    /// (correto pra overlays viewport-level). Use <c>"body"</c> ou um selector
    /// específico se o overlay é local.</param>
    public OverlayLifecycle(IJSRuntime js, ScrollManager scroll, string id,
        string? selector = null, string scrollTarget = "html")
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _selector = selector ?? $"#{id}";
        _scrollTarget = scrollTarget;
    }

    /// <summary>
    /// Engaja: lock scroll (opcional) + registra Esc handler / focus trap /
    /// autofocus inteligente via <c>omniBlazor.setupOverlay</c>. Idempotente —
    /// chamadas repetidas são no-op enquanto já ativo.
    /// </summary>
    /// <typeparam name="T">Tipo do componente Blazor que recebe o callback Esc.</typeparam>
    /// <param name="dotnetRef">Reference pro componente — owns dispose dele.</param>
    /// <param name="escMethod">Nome do método [JSInvokable] a chamar quando Esc.
    /// Default <c>"OnEscape"</c>.</param>
    /// <param name="lockScroll">Se <c>true</c> (default), bloqueia scroll. Setar
    /// false pra overlays decorativos que não querem prender o scroll.</param>
    public async Task ActivateAsync<T>(DotNetObjectReference<T> dotnetRef,
        string escMethod = "OnEscape", bool lockScroll = true) where T : class
    {
        if (_isActive) return;
        _isActive = true;

        if (lockScroll)
        {
            try { await _scroll.LockScrollAsync(_scrollTarget); } catch { /* SSR / JS gone */ }
        }

        try
        {
            await _js.InvokeAsync<bool>("omniBlazor.setupOverlay", _id, _selector,
                new { dotnet = dotnetRef, method = escMethod });
        }
        catch { /* SSR / JS gone */ }
    }

    /// <summary>
    /// Libera: unlock scroll + teardown JS handlers. Idempotente.
    /// </summary>
    /// <param name="unlockScroll">Se <c>true</c> (default), desbloqueia scroll.
    /// Deve bater com o valor passado em <see cref="ActivateAsync"/>.</param>
    public async Task DeactivateAsync(bool unlockScroll = true)
    {
        if (!_isActive) return;
        _isActive = false;

        try { await _js.InvokeAsync<bool>("omniBlazor.teardownOverlay", _id); }
        catch { /* SSR / JS gone */ }

        if (unlockScroll)
        {
            try { await _scroll.UnlockScrollAsync(_scrollTarget); } catch { /* SSR / JS gone */ }
        }
    }

    /// <summary>Garantia de cleanup quando o componente dispõe.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_isActive) await DeactivateAsync();
    }
}
