using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Services;

/// <summary>
/// Dirige tours guiados (coachmarks). Mesmo padrão de <c>DialogService</c>/<c>ContextMenuService</c>:
/// serviço Scoped que detém o estado e dispara <see cref="OnChange"/>; o <c>OmniTourHost</c>
/// (portal único) renderiza o spotlight + coachmark do passo atual.
///
/// <para>API programática: <see cref="StartAsync"/> devolve uma <c>Task&lt;bool&gt;</c> que completa
/// com <c>true</c> (concluído) ou <c>false</c> (pulado/dispensado). O <c>OmniTour</c> declarativo
/// usa internamente o mesmo <see cref="StartAsync"/>.</para>
/// </summary>
public sealed class TourService : IDisposable
{
    private readonly IOmniCoreJsModule _js;
    private readonly ILogger<TourService>? _logger;
    private List<TourStep> _steps = new();
    private TaskCompletionSource<bool>? _tcs;
    private TourOptions _options = new();
    private int _disposeState;

    internal TourService(IOmniCoreJsModule js)
        : this(js, null)
    {
    }

    internal TourService(IOmniCoreJsModule js, ILogger<TourService>? logger)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _logger = logger;
    }

    /// <summary>True enquanto um tour está em andamento.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Índice (0-based) do passo atual.</summary>
    public int CurrentStepIndex { get; private set; }

    /// <summary>Quantidade de passos do tour ativo.</summary>
    public int StepCount => _steps.Count;

    /// <summary>Passo atual (ou <c>null</c> se inativo / índice inválido).</summary>
    public TourStep? CurrentStep =>
        IsActive && CurrentStepIndex >= 0 && CurrentStepIndex < _steps.Count ? _steps[CurrentStepIndex] : null;

    /// <summary>Opções do tour ativo.</summary>
    public TourOptions Options => _options;

    /// <summary>Disparado a cada mudança de estado (início, passo, fim).</summary>
    public event Action? OnChange;

    /// <summary>
    /// Inicia um tour. Se <see cref="TourOptions.Persist"/> e já houver dispensa salva para o
    /// <see cref="TourOptions.TourId"/>, NÃO inicia e retorna <c>false</c>. Caso contrário, ativa e
    /// devolve uma Task que completa quando o tour é concluído (<c>true</c>) ou pulado (<c>false</c>).
    /// </summary>
    public async Task<bool> StartAsync(IEnumerable<TourStep> steps, TourOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var list = steps?.ToList() ?? new();
        if (list.Count == 0) return false;
        _options = options ?? new();

        if (_options.Persist && !string.IsNullOrEmpty(_options.TourId))
        {
            try
            {
                var dismissed = await _js.InvokeAsync<string?>("storageGet", DismissKey(_options.TourId!));
                if (dismissed == "1") return false;
            }
            catch { /* SSR / private mode — não suprime */ }
        }

        // Encerra um tour anterior (resolve sua Task como "não concluído").
        if (IsActive)
        {
            var prev = _tcs;
            _tcs = null;
            prev?.TrySetResult(false);
        }

        _steps = list;
        CurrentStepIndex = 0;
        IsActive = true;
        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            OnChange?.Invoke();
        }
        catch
        {
            IsActive = false;
            _steps = new();
            var failed = _tcs;
            _tcs = null;
            failed?.TrySetResult(false);
            throw;
        }
        return await _tcs.Task;
    }

    /// <summary>Avança um passo; no último, conclui o tour.</summary>
    public void Next()
    {
        if (!IsActive) return;
        if (CurrentStepIndex < _steps.Count - 1) { CurrentStepIndex++; OnChange?.Invoke(); }
        else TaskObserver.Observe(CompleteSafelyAsync(), operation: "TourService.Complete");
    }

    /// <summary>Volta um passo (no-op no primeiro).</summary>
    public void Prev()
    {
        if (!IsActive || CurrentStepIndex <= 0) return;
        CurrentStepIndex--;
        OnChange?.Invoke();
    }

    /// <summary>Vai para um passo específico (no-op se índice inválido).</summary>
    public void GoTo(int index)
    {
        if (!IsActive || index < 0 || index >= _steps.Count) return;
        CurrentStepIndex = index;
        OnChange?.Invoke();
    }

    /// <summary>Pula o tour (resolve com <c>false</c>).</summary>
    public Task SkipAsync() => EndAsync(false);

    /// <summary>Conclui o tour (resolve com <c>true</c>).</summary>
    public Task CompleteAsync() => EndAsync(true);

    private async Task EndAsync(bool completed)
    {
        if (!IsActive) return;
        IsActive = false;
        var opts = _options;
        var tcs = _tcs;
        _tcs = null;
        _steps = new();
        CurrentStepIndex = 0;
        // Libera o consumer imediatamente — a persistência (abaixo) não deve atrasar a Task.
        tcs?.TrySetResult(completed);

        try
        {
            OnChange?.Invoke();
        }
        finally
        {
            if (opts.Persist && !string.IsNullOrEmpty(opts.TourId))
            {
                try { await _js.InvokeVoidAsync("storageSet", DismissKey(opts.TourId!), "1"); }
                catch { }
            }
        }
    }

    /// <summary>Remove a dispensa salva de um tour (faz ele reaparecer). Útil em "ver tour de novo".</summary>
    public async Task ClearDismissalAsync(string tourId)
    {
        try { await _js.InvokeVoidAsync("storageRemove", DismissKey(tourId)); }
        catch { }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;

        IsActive = false;
        CurrentStepIndex = 0;
        _steps = new();
        var completion = _tcs;
        _tcs = null;
        OnChange = null;
        completion?.TrySetResult(false);
        GC.SuppressFinalize(this);
    }

    private async Task CompleteSafelyAsync()
    {
        try
        {
            await CompleteAsync();
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "An exception occurred while completing a tour.");
        }
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private static string DismissKey(string tourId) => $"omni.tour.dismissed.{tourId}";
}
