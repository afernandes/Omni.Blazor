using System.Text.Json;
using Microsoft.JSInterop;
using Omni.Blazor.Models;
using Omni.Blazor.Serialization;

namespace Omni.Blazor.Services;

/// <summary>Typed, trimming-safe local-storage boundary for DataGrid view preferences.</summary>
public sealed class DataGridStateStorageService
{
    private const string Prefix = "omni.grid.";
    private readonly IOmniCoreJsModule _js;

    internal DataGridStateStorageService(IOmniCoreJsModule js) => _js = js;

    /// <summary>Loads a persisted state, returning null for unavailable or malformed browser storage.</summary>
    public async ValueTask<DataGridViewState?> LoadAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        try
        {
            string? json = await _js.InvokeAsync<string?>(
                "storageGet",
                cancellationToken,
                Prefix + key);
            DataGridViewState? state = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize(json, OmniJsonSerializerContext.Default.DataGridViewState);
            return state?.Version == DataGridViewState.CurrentVersion ? state : null;
        }
        catch (Exception exception) when (IsUnavailableStorage(exception))
        {
            return null;
        }
    }

    /// <summary>Saves a state. Browser storage failures are treated as unavailable persistence.</summary>
    public async ValueTask<bool> SaveAsync(
        string key,
        DataGridViewState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(state);
        try
        {
            string json = JsonSerializer.Serialize(state, OmniJsonSerializerContext.Default.DataGridViewState);
            await _js.InvokeVoidAsync("storageSet", cancellationToken, Prefix + key, json);
            return true;
        }
        catch (Exception exception) when (IsUnavailableStorage(exception))
        {
            return false;
        }
    }

    /// <summary>Removes a persisted state.</summary>
    public async ValueTask<bool> RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        try
        {
            await _js.InvokeVoidAsync("storageRemove", cancellationToken, Prefix + key);
            return true;
        }
        catch (Exception exception) when (IsUnavailableStorage(exception))
        {
            return false;
        }
    }

    private static bool IsUnavailableStorage(Exception exception)
        => exception is JSException
            or JSDisconnectedException
            or InvalidOperationException
            or ObjectDisposedException
            or JsonException;
}
