using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Models;

/// <summary>Optional operations exposed by an <c>OmniFileManager</c> provider.</summary>
[Flags]
public enum FileManagerCapabilities
{
    /// <summary>Read-only browsing.</summary>
    Browse = 0,

    /// <summary>Create folders.</summary>
    CreateFolder = 1,

    /// <summary>Rename files and folders.</summary>
    Rename = 2,

    /// <summary>Delete files and folders.</summary>
    Delete = 4,

    /// <summary>Upload browser files.</summary>
    Upload = 8,

    /// <summary>Request a download through the consumer callback.</summary>
    Download = 16,

    /// <summary>All optional operations.</summary>
    All = CreateFolder | Rename | Delete | Upload | Download
}

/// <summary>Visual layout used by <c>OmniFileManager</c>.</summary>
public enum FileManagerView
{
    /// <summary>Compact tabular rows.</summary>
    List,

    /// <summary>Responsive icon cards.</summary>
    Grid
}

/// <summary>One file-system entry returned by an <see cref="IOmniFileManagerProvider"/>.</summary>
public sealed record FileManagerEntry(string Id, string Name, string Path, bool IsDirectory)
{
    /// <summary>File size in bytes, or null for directories/unknown values.</summary>
    public long? Size { get; init; }

    /// <summary>Last modification timestamp.</summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <summary>Optional MIME content type.</summary>
    public string? ContentType { get; init; }

    /// <summary>Optional Omni icon name overriding the built-in file/folder icon.</summary>
    public string? Icon { get; init; }

    /// <summary>Prevents rename and delete operations for this entry.</summary>
    public bool IsReadOnly { get; init; }
}

/// <summary>Immutable browse request sent to a file-manager provider.</summary>
public sealed record FileManagerRequest(string Path, string? SearchText, int Take);

/// <summary>Bounded page returned by a file-manager provider.</summary>
public sealed record FileManagerPage(IReadOnlyList<FileManagerEntry> Items, int TotalCount);

/// <summary>
/// Backend abstraction used by <c>OmniFileManager</c>. Browse is required;
/// optional mutations have safe default implementations.
/// </summary>
public interface IOmniFileManagerProvider
{
    /// <summary>Returns a bounded directory listing.</summary>
    ValueTask<FileManagerPage> GetItemsAsync(
        FileManagerRequest request,
        CancellationToken cancellationToken);

    /// <summary>Creates a folder below <paramref name="parentPath"/>.</summary>
    ValueTask CreateFolderAsync(
        string parentPath,
        string name,
        CancellationToken cancellationToken)
        => ValueTask.FromException(new NotSupportedException("Folder creation is not supported."));

    /// <summary>Renames an existing entry.</summary>
    ValueTask RenameAsync(
        FileManagerEntry entry,
        string newName,
        CancellationToken cancellationToken)
        => ValueTask.FromException(new NotSupportedException("Rename is not supported."));

    /// <summary>Deletes one entry.</summary>
    ValueTask DeleteAsync(
        FileManagerEntry entry,
        CancellationToken cancellationToken)
        => ValueTask.FromException(new NotSupportedException("Delete is not supported."));

    /// <summary>Uploads browser files into <paramref name="path"/>.</summary>
    ValueTask UploadAsync(
        string path,
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken)
        => ValueTask.FromException(new NotSupportedException("Upload is not supported."));
}

