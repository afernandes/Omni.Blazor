using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>Typed browser-download boundary supporting incrementally produced streams.</summary>
public sealed class FileDownloadService
{
    private readonly IJSRuntime _js;

    public FileDownloadService(IJSRuntime js) => _js = js;

    /// <summary>Downloads a stream without first materializing it as a managed string.</summary>
    public async ValueTask DownloadAsync(
        string filename,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        using var reference = new DotNetStreamReference(content, leaveOpen: true);
        await _js.InvokeVoidAsync(
            "omniBlazor.downloadStream",
            cancellationToken,
            filename,
            reference,
            contentType);
    }
}
