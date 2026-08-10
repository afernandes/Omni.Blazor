using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;
using System.Diagnostics.CodeAnalysis;

namespace Omni.Blazor.Services;

public class DialogService
{
    private readonly List<DialogReference> _openDialogs = new();
    private DialogReference? _openSideDialog;
    private int _sequence;   // monotonic — define ordem de "topmost" entre main e side dialogs

    public event Action? OnChange;

    internal IReadOnlyList<DialogReference> OpenDialogs => _openDialogs;
    internal DialogReference? OpenSideDialog => _openSideDialog;

    public Task<object?> OpenAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        string? title,
        Dictionary<string, object?>? parameters = null,
        DialogOptions? options = null) where TComponent : ComponentBase
        => OpenAsync(title, typeof(TComponent), parameters, options);

    public Task<object?> OpenAsync(
        string? title,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        Dictionary<string, object?>? parameters = null,
        DialogOptions? options = null)
    {
        var dialog = new DialogReference
        {
            Title = title,
            ComponentType = componentType,
            Parameters = parameters,
            Options = options ?? new DialogOptions(),
            Tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously),
            Sequence = ++_sequence
        };
        _openDialogs.Add(dialog);
        OnChange?.Invoke();
        return dialog.Tcs.Task;
    }

    public Task<object?> OpenSideAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        string? title,
        Dictionary<string, object?>? parameters = null,
        SideDialogOptions? options = null) where TComponent : ComponentBase
        => OpenSideAsync(title, typeof(TComponent), parameters, options);

    public Task<object?> OpenSideAsync(
        string? title,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
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
            Tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously),
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
        // O primeiro argumento de OpenAsync é o TÍTULO da barra do dialog — passar o
        // nome do tipo fazia a barra exibir "Omni.Blazor.Components.ConfirmDialog"
        // e descartava o título informado por quem chamou.
        var raw = await OpenAsync(title, typeof(Components.ConfirmDialog), pars,
            new DialogOptions
            {
                ShowTitle = !string.IsNullOrEmpty(title),
                CloseDialogOnOverlayClick = false,
                Width = "400px"
            });
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
        // Ver a nota em Confirm: o primeiro argumento é o título, não o nome do tipo.
        await OpenAsync(title, typeof(Components.AlertDialog), pars,
            new DialogOptions
            {
                ShowTitle = !string.IsNullOrEmpty(title),
                CloseDialogOnOverlayClick = false,
                Width = "400px"
            });
    }

    /// <summary>
    /// Fecha o dialog "topmost" (mais recente em ordem de abertura) — pode ser
    /// um main ou o side, dependendo de qual foi aberto por último. Isso permite
    /// que componentes plugados via <c>DynamicComponent</c> chamem
    /// <c>Dialog.Close(result)</c> sem saber se estão sendo renderizados num
    /// modal central ou num drawer lateral.
    /// </summary>
    public void Close(object? result = null)
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
    public void CloseSide(object? result = null)
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

    public Task<object?> OpenAsync(
        string title,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
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
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public required Type ComponentType { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
    public required DialogOptionsBase Options { get; set; }
    public required TaskCompletionSource<object?> Tcs { get; set; }
    public bool IsSide { get; set; }

    /// <summary>Monotonic open-order counter. Used by <c>Close()</c> to decide
    /// which dialog is "topmost" when both a main and a side dialog are open.</summary>
    public int Sequence { get; set; }
}

// DialogServiceExtensions.With foi removido: definia apenas ShowTitle e descartava
// o título recebido, o que mascarava o bug de Alert/Confirm passarem o nome do tipo
// como título. Os dois call sites agora passam o título para OpenAsync diretamente.
