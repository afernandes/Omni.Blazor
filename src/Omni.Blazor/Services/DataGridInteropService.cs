using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>Typed JavaScript boundary for DataGrid browser interactions.</summary>
public sealed class DataGridInteropService
{
    private readonly IOmniDataJsModule _js;

    internal DataGridInteropService(IOmniDataJsModule js) => _js = js;

    /// <summary>Starts one browser-owned column-resize gesture.</summary>
    public async ValueTask StartColumnResizeAsync<TGrid>(
        string columnId,
        DotNetObjectReference<TGrid> receiver,
        int visibleIndex,
        double clientX,
        double minimumWidth,
        CancellationToken cancellationToken = default)
        where TGrid : class
    {
        await _js.InvokeVoidAsync(
            "gridStartColumnResize",
            cancellationToken,
            columnId,
            receiver,
            visibleIndex,
            clientX,
            minimumWidth);
    }

    /// <summary>
    /// Starts one browser-owned drag of a column header toward the group panel.
    /// Pointer-event based — HTML5 Drag and Drop never delivers the drop inside
    /// WebView2 hosts (MAUI/Photino on Windows). JS calls back
    /// <c>OnGroupGripDropped</c> on <paramref name="receiver"/> only when the
    /// pointer is released over the panel.
    /// </summary>
    public async ValueTask StartGroupDragAsync<TGrid>(
        string panelId,
        DotNetObjectReference<TGrid> receiver,
        string columnTitle,
        CancellationToken cancellationToken = default)
        where TGrid : class
    {
        await _js.InvokeVoidAsync(
            "gridStartGroupDrag",
            cancellationToken,
            panelId,
            receiver,
            columnTitle);
    }
}
