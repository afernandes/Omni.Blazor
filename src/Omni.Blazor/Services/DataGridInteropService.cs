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
}
