using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

public abstract class DialogOptionsBase
{
    public bool ShowTitle { get; set; } = true;
    public bool ShowClose { get; set; } = true;
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? Style { get; set; }
    public string? CssClass { get; set; }
    public bool CloseDialogOnOverlayClick { get; set; } = true;
    public bool CloseDialogOnEsc { get; set; } = true;
    public Func<Task<bool>>? CanClose { get; set; }
    public RenderFragment? TitleContent { get; set; }
}

public class DialogOptions : DialogOptionsBase
{
}

public class SideDialogOptions : DialogOptionsBase
{
    public SidePosition Position { get; set; } = SidePosition.Right;
    public bool ShowMask { get; set; } = true;
}

public class ConfirmOptions : DialogOptionsBase
{
    public string OkButtonText { get; set; } = "Confirmar";
    public string CancelButtonText { get; set; } = "Cancelar";
    public string? Icon { get; set; } = "alert-triangle";
}

public class AlertOptions : DialogOptionsBase
{
    public string OkButtonText { get; set; } = "Entendi";
    public string? Icon { get; set; }
}
