using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Componente "headless" (sem render visual próprio) que escuta uma CSS
/// media query e dispara um callback / two-way bind quando o estado muda.
///
/// <para>Equivalente ao <c>RadzenMediaQuery</c>, mas com APIs adicionais:</para>
/// <list type="bullet">
///   <item><description>Two-way <c>@bind-Matches</c> além do <c>Change</c> callback.</description></item>
///   <item><description>Atalho <see cref="Breakpoint"/> que traduz para <c>(min-width: …)</c>
///     usando os mesmos thresholds do <c>OmniHidden</c>/<c>BreakpointService</c>.</description></item>
///   <item><description><see cref="ChildContent"/> como render-prop opcional —
///     recebe o <c>bool</c> atual pra você renderizar condicionalmente sem
///     precisar de um campo no <c>@code</c>.</description></item>
/// </list>
///
/// <para>Usage — callback:</para>
/// <code>
/// &lt;OmniMediaQuery Query="(max-width: 768px)" Change="OnMobileChange" /&gt;
/// @code {
///     void OnMobileChange(bool isMobile) { _isMobile = isMobile; }
/// }
/// </code>
///
/// <para>Usage — two-way bind:</para>
/// <code>
/// &lt;OmniMediaQuery Query="(prefers-color-scheme: dark)" @bind-Matches="_isDark" /&gt;
/// </code>
///
/// <para>Usage — render-prop:</para>
/// <code>
/// &lt;OmniMediaQuery Breakpoint="Breakpoint.Md" Context="atMd"&gt;
///     @if (atMd) { &lt;DesktopNav /&gt; } else { &lt;MobileNav /&gt; }
/// &lt;/OmniMediaQuery&gt;
/// </code>
/// </summary>
/// <remarks>
/// <para>Por baixo do capô usa <c>window.matchMedia</c> + listener — a fonte
/// canônica do browser pra avaliar <c>@media</c> rules. Reflete devtools
/// device-emulation, page zoom e resize de janela em tempo real.</para>
/// <para>Não renderiza nada quando <see cref="ChildContent"/> é nulo (zero footprint
/// no DOM). Quando definido, renderiza o ChildContent passando o booleano atual.</para>
/// </remarks>
public class OmniMediaQuery : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Media query CSS literal. Exemplo: <c>"(max-width: 768px)"</c>,
    /// <c>"(prefers-color-scheme: dark)"</c>, <c>"(orientation: portrait)"</c>.
    /// Tem precedência sobre <see cref="Breakpoint"/> quando ambos estão definidos.
    /// </summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>
    /// Atalho conveniente: traduz para <c>(min-width: Npx)</c> usando os mesmos
    /// thresholds dos outros componentes (sm=576, md=768, lg=992, xl=1200, xxl=1400).
    /// Útil quando você quer "viewport ≥ Md". Ignorado quando <see cref="Query"/> está definido.
    /// </summary>
    [Parameter] public Breakpoint? Breakpoint { get; set; }

    /// <summary>
    /// Two-way bind do resultado: <c>true</c> quando a query corresponde, <c>false</c> caso contrário.
    /// Use junto com <c>@bind-Matches</c> em vez do callback <see cref="Change"/> quando você só
    /// precisa do valor declarativamente.
    /// </summary>
    [Parameter] public bool Matches { get; set; }

    /// <summary>Disparado quando o <c>Matches</c> muda (escrita do two-way bind).</summary>
    [Parameter] public EventCallback<bool> MatchesChanged { get; set; }

    /// <summary>
    /// Callback opcional disparado quando o resultado da query muda. Recebe o novo
    /// valor (<c>true</c> = corresponde). Fora do bind: serve pra rodar side effects
    /// (logging, scroll lock, etc.) quando o estado muda.
    /// </summary>
    [Parameter] public EventCallback<bool> Change { get; set; }

    /// <summary>
    /// Render-prop opcional: quando definido, o conteúdo é renderizado recebendo
    /// o <c>bool</c> atual via <c>Context</c>. Sem ChildContent, o componente é
    /// totalmente headless (zero DOM).
    /// </summary>
    [Parameter] public RenderFragment<bool>? ChildContent { get; set; }

    private string _id = $"mq-{Guid.NewGuid():N}";
    private string? _lastQuery;
    private DotNetObjectReference<OmniMediaQuery>? _selfRef;
    private bool _initialized;

    /// <summary>Resolve a query final — explícita ou derivada do <see cref="Breakpoint"/>.</summary>
    private string? ResolvedQuery => Query ?? (Breakpoint is { } bp ? $"(min-width: {BreakpointPx(bp)}px)" : null);

    private static int BreakpointPx(Breakpoint bp) => bp switch
    {
        Models.Breakpoint.Sm  => 576,
        Models.Breakpoint.Md  => 768,
        Models.Breakpoint.Lg  => 992,
        Models.Breakpoint.Xl  => 1200,
        Models.Breakpoint.Xxl => 1400,
        _                      => 0,    // Xs = sempre verdadeiro
    };

    /// <inheritdoc />
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        // Render-prop opcional. Quando ChildContent é null, não emite nada — o
        // componente vira 100% headless (sem nó no DOM além do que o Blazor
        // costura internamente).
        if (ChildContent is not null)
        {
            builder.AddContent(0, ChildContent(Matches));
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var resolved = ResolvedQuery;

        // (Re)subscribe quando: 1ª render OU query mudou via parâmetro.
        if (resolved is not null && resolved != _lastQuery)
        {
            _lastQuery = resolved;
            _selfRef ??= DotNetObjectReference.Create(this);

            try
            {
                var initial = await JS.InvokeAsync<bool>(
                    "omniBlazor.subscribeMediaQuery",
                    _id, resolved, _selfRef, nameof(OnMediaQueryChanged));
                _initialized = true;

                if (initial != Matches)
                {
                    await NotifyAsync(initial);
                }
            }
            catch
            {
                // JS indisponível (pre-render / SSR puro) — ignora silenciosamente.
                // A próxima OnAfterRenderAsync vai re-tentar quando o Blazor
                // anexar o circuito.
            }
        }
    }

    /// <summary>Chamado pelo JS quando o media query flipa.</summary>
    [JSInvokable]
    public Task OnMediaQueryChanged(bool matches) => NotifyAsync(matches);

    private async Task NotifyAsync(bool matches)
    {
        if (matches == Matches) return;
        Matches = matches;
        if (MatchesChanged.HasDelegate) await MatchesChanged.InvokeAsync(matches);
        if (Change.HasDelegate) await Change.InvokeAsync(matches);
        StateHasChanged();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_initialized)
        {
            try { await JS.InvokeVoidAsync("omniBlazor.unsubscribeMediaQuery", _id); }
            catch { /* circuito morto durante navegação */ }
        }
        _selfRef?.Dispose();
        _selfRef = null;
        GC.SuppressFinalize(this);
    }
}
