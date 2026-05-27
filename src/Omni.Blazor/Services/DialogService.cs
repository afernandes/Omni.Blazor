using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

public class DialogService
{
    private readonly List<DialogReference> _openDialogs = new();
    private DialogReference? _openSideDialog;
    private int _sequence;   // monotonic — define ordem de "topmost" entre main e side dialogs

    public event Action? OnChange;

    internal IReadOnlyList<DialogReference> OpenDialogs => _openDialogs;
    internal DialogReference? OpenSideDialog => _openSideDialog;

    public Task<dynamic?> OpenAsync<TComponent>(
        string? title,
        Dictionary<string, object?>? parameters = null,
        DialogOptions? options = null) where TComponent : ComponentBase
        => OpenAsync(title, typeof(TComponent), parameters, options);

    public Task<dynamic?> OpenAsync(
        string? title,
        Type componentType,
        Dictionary<string, object?>? parameters = null,
        DialogOptions? options = null)
    {
        var dialog = new DialogReference
        {
            Title = title,
            ComponentType = componentType,
            Parameters = parameters,
            Options = options ?? new DialogOptions(),
            Tcs = new TaskCompletionSource<dynamic?>(),
            Sequence = ++_sequence
        };
        _openDialogs.Add(dialog);
        OnChange?.Invoke();
        return dialog.Tcs.Task;
    }

    public Task<dynamic?> OpenSideAsync<TComponent>(
        string? title,
        Dictionary<string, object?>? parameters = null,
        SideDialogOptions? options = null) where TComponent : ComponentBase
        => OpenSideAsync(title, typeof(TComponent), parameters, options);

    public Task<dynamic?> OpenSideAsync(
        string? title,
        Type componentType,
        Dictionary<string, object?>? parameters = null,
        SideDialogOptions? options = null)
    {
        CloseSide();
        var dialog = new DialogReference
        {
            Title = title,
            ComponentType = componentType,
            Parameters = parameters,
            Options = options ?? new SideDialogOptions(),
            Tcs = new TaskCompletionSource<dynamic?>(),
            IsSide = true,
            Sequence = ++_sequence
        };
        _openSideDialog = dialog;
        OnChange?.Invoke();
        return dialog.Tcs.Task;
    }

    public async Task<bool?> Confirm(
        string message,
        string? title = "Confirmar",
        ConfirmOptions? options = null)
    {
        options ??= new ConfirmOptions();
        var pars = new Dictionary<string, object?>
        {
            ["Message"] = message,
            ["Options"] = options
        };
        var raw = await OpenAsync("Omni.Blazor.Components.ConfirmDialog", typeof(Components.ConfirmDialog), pars,
            new DialogOptions
            {
                ShowTitle = !string.IsNullOrEmpty(title),
                CloseDialogOnOverlayClick = false,
                Width = "400px"
            }.With(title));
        if (raw is bool b) return b;
        return null;
    }

    public async Task Alert(
        string message,
        string? title = "Aviso",
        AlertOptions? options = null)
    {
        options ??= new AlertOptions();
        var pars = new Dictionary<string, object?>
        {
            ["Message"] = message,
            ["Options"] = options
        };
        await OpenAsync("Omni.Blazor.Components.AlertDialog", typeof(Components.AlertDialog), pars,
            new DialogOptions
            {
                ShowTitle = !string.IsNullOrEmpty(title),
                CloseDialogOnOverlayClick = false,
                Width = "400px"
            }.With(title));
    }

    /// <summary>
    /// Fecha o dialog "topmost" (mais recente em ordem de abertura) — pode ser
    /// um main ou o side, dependendo de qual foi aberto por último. Isso permite
    /// que componentes plugados via <c>DynamicComponent</c> chamem
    /// <c>Dialog.Close(result)</c> sem saber se estão sendo renderizados num
    /// modal central ou num drawer lateral.
    /// </summary>
    public void Close(dynamic? result = null)
    {
        var topmost = Topmost();
        if (topmost is null) return;
        if (topmost.IsSide)
        {
            _openSideDialog = null;
        }
        else
        {
            _openDialogs.Remove(topmost);
        }
        topmost.Tcs.TrySetResult(result);
        OnChange?.Invoke();
    }

    /// <summary>Fecha explicitamente o side dialog (independente de qual é o
    /// topmost). Útil para programatic close por código de fora do componente.</summary>
    public void CloseSide(dynamic? result = null)
    {
        if (_openSideDialog is null) return;
        var dlg = _openSideDialog;
        _openSideDialog = null;
        dlg.Tcs.TrySetResult(result);
        OnChange?.Invoke();
    }

    /// <summary>Returns the topmost dialog by open-order. Compares the last
    /// main dialog's sequence with the side dialog's sequence; whichever was
    /// opened later "wins". Null when no dialog is open.</summary>
    private DialogReference? Topmost()
    {
        var lastMain = _openDialogs.Count > 0 ? _openDialogs[^1] : null;
        var side = _openSideDialog;
        if (lastMain is null) return side;
        if (side is null) return lastMain;
        return side.Sequence > lastMain.Sequence ? side : lastMain;
    }

    public Task<dynamic?> OpenAsync(
        string title,
        Type componentType,
        Dictionary<string, object?>? parameters,
        DialogOptions options,
        bool _)
        => OpenAsync(title, componentType, parameters, options);
}

internal class DialogReference
{
    /// <summary>Stable id used by the host for DOM targeting (focus trap, scroll lock).
    /// Generated once when the reference is created — survives re-renders.</summary>
    public string Id { get; } = "omni-dlg-" + Guid.NewGuid().ToString("N")[..8];

    public string? Title { get; set; }
    public required Type ComponentType { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public required DialogOptionsBase Options { get; set; }
    public required TaskCompletionSource<dynamic?> Tcs { get; set; }
    public bool IsSide { get; set; }

    /// <summary>Monotonic open-order counter. Used by <c>Close()</c> to decide
    /// which dialog is "topmost" when both a main and a side dialog are open.</summary>
    public int Sequence { get; set; }
}

internal static class DialogServiceExtensions
{
    public static DialogOptions With(this DialogOptions opts, string? title)
    {
        opts.ShowTitle = !string.IsNullOrEmpty(title);
        return opts;
    }
}
