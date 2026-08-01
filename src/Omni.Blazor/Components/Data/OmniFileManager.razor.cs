using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Models;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Provider-backed file manager with bounded loading, navigation and guarded mutations.</summary>
public partial class OmniFileManager
{
    private readonly object _loadSync = new();
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly CancellationToken _lifetimeToken;
    private readonly List<FileManagerEntry> _items = [];
    private CancellationTokenSource? _loadCts;
    private ParameterState<(IOmniFileManagerProvider? Provider, string Path, string Search, int MaxItems)> _sourceState = null!;
    private IOmniFileManagerProvider? _lastProvider;
    private string? _lastPath;
    private string? _lastSearch;
    private int _lastMaxItems;
    private string? _error;
    private string _editorValue = string.Empty;
    private long _loadVersion;
    private int _disposeState;
    private int _mutationState;
    private int _totalCount;
    private bool _loading;
    private bool _mutating;
    private bool _confirmDelete;
    private bool _sourceInitialized;
    private EditorMode _editorMode;

    /// <summary>Initializes a file manager with an owned lifetime cancellation token.</summary>
    public OmniFileManager() => _lifetimeToken = _lifetimeCts.Token;

    /// <summary>Backend responsible for browsing and optional mutations.</summary>
    [Parameter, EditorRequired] public IOmniFileManagerProvider? Provider { get; set; }

    /// <summary>Current logical path for two-way binding. Paths use forward slashes.</summary>
    [Parameter] public string Path { get; set; } = "/";

    /// <summary>Raised when <see cref="Path"/> changes.</summary>
    [Parameter] public EventCallback<string> PathChanged { get; set; }

    /// <summary>Current provider-side search text for two-way binding.</summary>
    [Parameter] public string SearchText { get; set; } = string.Empty;

    /// <summary>Raised when <see cref="SearchText"/> changes.</summary>
    [Parameter] public EventCallback<string> SearchTextChanged { get; set; }

    /// <summary>Currently selected entry for two-way binding.</summary>
    [Parameter] public FileManagerEntry? SelectedItem { get; set; }

    /// <summary>Raised when <see cref="SelectedItem"/> changes.</summary>
    [Parameter] public EventCallback<FileManagerEntry?> SelectedItemChanged { get; set; }

    /// <summary>Enabled optional operations. Browse is always available.</summary>
    [Parameter] public FileManagerCapabilities Capabilities { get; set; } = FileManagerCapabilities.Browse;

    /// <summary>Current list or grid layout for two-way binding.</summary>
    [Parameter] public FileManagerView View { get; set; } = FileManagerView.List;

    /// <summary>Raised when <see cref="View"/> changes.</summary>
    [Parameter] public EventCallback<FileManagerView> ViewChanged { get; set; }

    /// <summary>Maximum entries requested, retained and rendered per directory.</summary>
    [Parameter] public int MaxItems { get; set; } = 1000;

    /// <summary>Delay before provider-side text searches, in milliseconds.</summary>
    [Parameter] public int SearchDebounce { get; set; } = 250;

    /// <summary>Maximum files accepted in one upload selection.</summary>
    [Parameter] public int MaxUploadFiles { get; set; } = 20;

    /// <summary>Optional custom renderer for each entry.</summary>
    [Parameter] public RenderFragment<FileManagerEntry>? ItemTemplate { get; set; }

    /// <summary>Raised after a directory is opened and its path changes.</summary>
    [Parameter] public EventCallback<FileManagerEntry> DirectoryOpened { get; set; }

    /// <summary>Raised when the user requests download of a file.</summary>
    [Parameter] public EventCallback<FileManagerEntry> DownloadRequested { get; set; }

    /// <summary>Raised after a provider operation changes the directory contents.</summary>
    [Parameter] public EventCallback ItemsChanged { get; set; }

    /// <summary>Raised when an uncancelled provider operation fails.</summary>
    [Parameter] public EventCallback<Exception> OperationFailed { get; set; }

    /// <summary>Accessible label for the component.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Gerenciador de arquivos";

    /// <summary>Accessible label for breadcrumb navigation.</summary>
    [Parameter] public string BreadcrumbLabel { get; set; } = "Localização";

    /// <summary>Label for the create-folder action.</summary>
    [Parameter] public string NewFolderText { get; set; } = "Nova pasta";

    /// <summary>Placeholder for a new folder name.</summary>
    [Parameter] public string NewFolderPlaceholder { get; set; } = "Nome da pasta";

    /// <summary>Label for the rename action.</summary>
    [Parameter] public string RenameText { get; set; } = "Renomear";

    /// <summary>Placeholder for a new entry name.</summary>
    [Parameter] public string RenamePlaceholder { get; set; } = "Novo nome";

    /// <summary>Label for the delete action.</summary>
    [Parameter] public string DeleteText { get; set; } = "Excluir";

    /// <summary>Delete confirmation template. Placeholder zero receives the entry name.</summary>
    [Parameter] public string DeleteConfirmationText { get; set; } = "Excluir “{0}”?";

    /// <summary>Label for the upload action.</summary>
    [Parameter] public string UploadText { get; set; } = "Enviar";

    /// <summary>Label for the download action.</summary>
    [Parameter] public string DownloadText { get; set; } = "Baixar";

    /// <summary>Label for the refresh action.</summary>
    [Parameter] public string RefreshText { get; set; } = "Atualizar";

    /// <summary>Label for list view.</summary>
    [Parameter] public string ListViewText { get; set; } = "Exibição em lista";

    /// <summary>Label for grid view.</summary>
    [Parameter] public string GridViewText { get; set; } = "Exibição em grade";

    /// <summary>Provider-side search placeholder.</summary>
    [Parameter] public string SearchPlaceholder { get; set; } = "Buscar nesta pasta";

    /// <summary>Label for saving an inline edit.</summary>
    [Parameter] public string SaveText { get; set; } = "Salvar";

    /// <summary>Label for cancelling a pending action.</summary>
    [Parameter] public string CancelText { get; set; } = "Cancelar";

    /// <summary>Label for confirming deletion.</summary>
    [Parameter] public string ConfirmText { get; set; } = "Confirmar";

    /// <summary>Message shown while the initial listing loads.</summary>
    [Parameter] public string LoadingText { get; set; } = "Carregando arquivos...";

    /// <summary>Message shown when the directory is empty.</summary>
    [Parameter] public string EmptyText { get; set; } = "Esta pasta está vazia.";

    /// <summary>Message shown after a provider failure.</summary>
    [Parameter] public string ErrorText { get; set; } = "Não foi possível concluir a operação.";

    /// <summary>Footer template for visible and total item counts.</summary>
    [Parameter] public string ItemsCountText { get; set; } = "{0} de {1} itens";

    /// <summary>Footer template shown when the configured item limit was reached.</summary>
    [Parameter] public string LimitText { get; set; } = "Limite de {0} itens";

    /// <summary>Current bounded entry view.</summary>
    public IReadOnlyList<FileManagerEntry> Items => _items;

    /// <summary>Whether a load or mutation is active.</summary>
    public bool IsBusy => _loading || _mutating;

    private IReadOnlyList<BreadcrumbSegment> Breadcrumbs => BuildBreadcrumbs(Path);

    private string RootCss => CssBuilder.Default("omni-file-manager")
        .AddClass("omni-file-manager-busy", IsBusy)
        .AddClass(Class)
        .Build();

    private string ContentCss => CssBuilder.Default("omni-file-manager-content")
        .AddClass(View == FileManagerView.Grid
            ? "omni-file-manager-content-grid"
            : "omni-file-manager-content-list")
        .Build();

    private string UploadCss => CssBuilder.Default("omni-file-manager-tool")
        .AddClass("omni-disabled", IsBusy)
        .Build();

    protected override void OnInitialized()
    {
        _sourceState = RegisterParameter<(IOmniFileManagerProvider?, string, string, int)>("Source")
            .WithParameter(() => (Provider, NormalizePath(Path), SearchText ?? string.Empty, MaxItems))
            .WithChangeHandler(SynchronizeSource)
            .Attach();
    }

    /// <summary>Reloads the current directory immediately.</summary>
    public Task ReloadAsync() => LoadAsync(useSearchDebounce: false, CancellationToken.None);

    /// <summary>Reloads the current directory immediately with cancellation.</summary>
    public Task ReloadAsync(CancellationToken cancellationToken)
        => LoadAsync(useSearchDebounce: false, cancellationToken);

    private void SynchronizeSource()
    {
        var normalizedPath = NormalizePath(Path);
        var normalizedSearch = SearchText ?? string.Empty;
        var changedOnlySearch = _sourceInitialized
            && ReferenceEquals(Provider, _lastProvider)
            && string.Equals(normalizedPath, _lastPath, StringComparison.Ordinal)
            && !string.Equals(normalizedSearch, _lastSearch, StringComparison.Ordinal)
            && MaxItems == _lastMaxItems;

        if (_sourceInitialized
            && ReferenceEquals(Provider, _lastProvider)
            && string.Equals(normalizedPath, _lastPath, StringComparison.Ordinal)
            && string.Equals(normalizedSearch, _lastSearch, StringComparison.Ordinal)
            && MaxItems == _lastMaxItems)
        {
            return;
        }

        _sourceInitialized = true;
        ObserveLoad(LoadAsync(changedOnlySearch, CancellationToken.None));
    }

    private async Task LoadAsync(bool useSearchDebounce, CancellationToken cancellationToken)
    {
        var provider = Provider;
        var normalizedPath = NormalizePath(Path);
        var normalizedSearch = SearchText ?? string.Empty;
        _lastProvider = provider;
        _lastPath = normalizedPath;
        _lastSearch = normalizedSearch;
        _lastMaxItems = MaxItems;

        var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeToken);
        CancellationTokenSource? previous;
        long version;
        lock (_loadSync)
        {
            if (IsDisposed)
            {
                linked.Dispose();
                return;
            }
            version = ++_loadVersion;
            previous = _loadCts;
            _loadCts = linked;
        }
        CancelSafely(previous);

        try
        {
            _loading = true;
            _error = null;
            await InvokeAsync(StateHasChanged);

            if (provider is null)
            {
                _items.Clear();
                _totalCount = 0;
                return;
            }

            if (useSearchDebounce && SearchDebounce > 0)
                await Task.Delay(SearchDebounce, linked.Token);

            var limit = Math.Clamp(MaxItems, 1, 10_000);
            var page = await provider.GetItemsAsync(
                new FileManagerRequest(normalizedPath, NullIfWhiteSpace(normalizedSearch), limit),
                linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (!IsCurrentLoad(version, linked)) return;

            _items.Clear();
            foreach (var item in page.Items)
            {
                if (_items.Count >= limit) break;
                _items.Add(item);
            }
            _totalCount = Math.Max(_items.Count, page.TotalCount);
            if (SelectedItem is not null && !_items.Any(IsSelected))
                await SetSelectedAsync(null);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!IsCurrentLoad(version, linked)) return;
            await PublishErrorAsync(exception);
        }
        finally
        {
            var isCurrent = IsCurrentLoad(version, linked);
            lock (_loadSync)
            {
                if (ReferenceEquals(_loadCts, linked)) _loadCts = null;
            }
            linked.Dispose();

            if (isCurrent && !IsDisposed)
            {
                _loading = false;
                try
                {
                    await InvokeAsync(StateHasChanged);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (InvalidOperationException) when (IsDisposed)
                {
                }
            }
        }
    }

    private async Task NavigateAsync(string path)
    {
        var normalized = NormalizePath(path);
        if (string.Equals(normalized, Path, StringComparison.Ordinal)) return;

        Path = normalized;
        SearchText = string.Empty;
        _confirmDelete = false;
        _editorMode = EditorMode.None;
        await SetSelectedAsync(null);
        if (PathChanged.HasDelegate) await PathChanged.InvokeAsync(normalized);
        if (SearchTextChanged.HasDelegate) await SearchTextChanged.InvokeAsync(string.Empty);
        await LoadAsync(useSearchDebounce: false, CancellationToken.None);
    }

    private async Task OpenAsync(FileManagerEntry item)
    {
        if (!item.IsDirectory || IsBusy) return;
        await NavigateAsync(item.Path);
        if (DirectoryOpened.HasDelegate) await DirectoryOpened.InvokeAsync(item);
    }

    private Task SelectAsync(FileManagerEntry item)
        => SetSelectedAsync(IsSelected(item) ? null : item);

    private async Task SetSelectedAsync(FileManagerEntry? item)
    {
        SelectedItem = item;
        _confirmDelete = false;
        if (SelectedItemChanged.HasDelegate) await SelectedItemChanged.InvokeAsync(item);
    }

    private async Task SearchChangedAsync(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? string.Empty;
        if (SearchTextChanged.HasDelegate) await SearchTextChanged.InvokeAsync(SearchText);
        await LoadAsync(useSearchDebounce: true, CancellationToken.None);
    }

    private void BeginCreateFolder()
    {
        if (IsBusy) return;
        _confirmDelete = false;
        _editorMode = EditorMode.CreateFolder;
        _editorValue = string.Empty;
    }

    private void BeginRename()
    {
        if (IsBusy || SelectedItem is null || SelectedItem.IsReadOnly) return;
        _confirmDelete = false;
        _editorMode = EditorMode.Rename;
        _editorValue = SelectedItem.Name;
    }

    private void CancelEditor()
    {
        _editorMode = EditorMode.None;
        _editorValue = string.Empty;
    }

    private async Task EditorKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter") await CommitEditorAsync();
        else if (args.Key == "Escape") CancelEditor();
    }

    private Task CommitEditorAsync()
    {
        var value = _editorValue.Trim();
        if (value.Length == 0) return Task.CompletedTask;

        return _editorMode switch
        {
            EditorMode.CreateFolder => RunMutationAsync(
                token => Provider!.CreateFolderAsync(NormalizePath(Path), value, token)),
            EditorMode.Rename when SelectedItem is not null => RunMutationAsync(
                token => Provider!.RenameAsync(SelectedItem, value, token)),
            _ => Task.CompletedTask
        };
    }

    private void BeginDelete()
    {
        if (IsBusy || SelectedItem is null || SelectedItem.IsReadOnly) return;
        CancelEditor();
        _confirmDelete = true;
    }

    private void CancelDelete() => _confirmDelete = false;

    private Task DeleteAsync()
    {
        var item = SelectedItem;
        return item is null
            ? Task.CompletedTask
            : RunMutationAsync(token => Provider!.DeleteAsync(item, token));
    }

    private async Task UploadAsync(InputFileChangeEventArgs args)
    {
        if (Provider is null || IsBusy) return;
        try
        {
            var files = args.GetMultipleFiles(Math.Clamp(MaxUploadFiles, 1, 100));
            await RunMutationAsync(token => Provider.UploadAsync(NormalizePath(Path), files, token));
        }
        catch (Exception exception)
        {
            await PublishErrorAsync(exception);
        }
    }

    private async Task RunMutationAsync(Func<CancellationToken, ValueTask> mutation)
    {
        if (Provider is null || Interlocked.CompareExchange(ref _mutationState, 1, 0) != 0) return;

        _mutating = true;
        _error = null;
        try
        {
            await InvokeAsync(StateHasChanged);
            await mutation(_lifetimeToken);
            _lifetimeToken.ThrowIfCancellationRequested();
            _confirmDelete = false;
            CancelEditor();
            await SetSelectedAsync(null);
            await LoadAsync(useSearchDebounce: false, CancellationToken.None);
            if (ItemsChanged.HasDelegate) await ItemsChanged.InvokeAsync();
        }
        catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await PublishErrorAsync(exception);
        }
        finally
        {
            _mutating = false;
            Volatile.Write(ref _mutationState, 0);
            if (!IsDisposed)
            {
                try
                {
                    await InvokeAsync(StateHasChanged);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }

    private async Task DownloadAsync()
    {
        if (SelectedItem is { IsDirectory: false } item && DownloadRequested.HasDelegate)
            await DownloadRequested.InvokeAsync(item);
    }

    private async Task SetViewAsync(FileManagerView view)
    {
        if (View == view) return;
        View = view;
        if (ViewChanged.HasDelegate) await ViewChanged.InvokeAsync(view);
    }

    private bool Can(FileManagerCapabilities capability) => (Capabilities & capability) == capability;
    private bool IsSelected(FileManagerEntry item) =>
        SelectedItem is not null && string.Equals(SelectedItem.Id, item.Id, StringComparison.Ordinal);

    private string ItemCss(FileManagerEntry item) => CssBuilder.Default("omni-file-manager-item")
        .AddClass("omni-selected", IsSelected(item))
        .AddClass("omni-file-manager-directory", item.IsDirectory)
        .Build();

    private string ViewButtonCss(FileManagerView view) => CssBuilder.Default("omni-file-manager-tool")
        .AddClass("omni-file-manager-tool-icon")
        .AddClass("omni-active", View == view)
        .Build();

    private static string IconFor(FileManagerEntry item)
    {
        if (!string.IsNullOrWhiteSpace(item.Icon)) return item.Icon;
        if (item.IsDirectory) return "folder";
        if (item.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true) return "image";
        return "file-text";
    }

    private static string FormatSize(FileManagerEntry item)
    {
        if (item.IsDirectory || item.Size is null) return "—";
        var bytes = item.Size.Value;
        if (bytes < 1024) return string.Create(CultureInfo.CurrentCulture, $"{bytes} B");
        if (bytes < 1024L * 1024) return string.Create(CultureInfo.CurrentCulture, $"{bytes / 1024d:N1} KB");
        if (bytes < 1024L * 1024 * 1024) return string.Create(CultureInfo.CurrentCulture, $"{bytes / (1024d * 1024):N1} MB");
        return string.Create(CultureInfo.CurrentCulture, $"{bytes / (1024d * 1024 * 1024):N2} GB");
    }

    private static string FormatModified(DateTimeOffset? value)
        => value?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "—";

    private static string? NullIfWhiteSpace(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/") return "/";
        var normalized = path.Replace('\\', '/').Trim();
        if (!normalized.StartsWith('/')) normalized = "/" + normalized;
        return normalized.TrimEnd('/');
    }

    private static IReadOnlyList<BreadcrumbSegment> BuildBreadcrumbs(string path)
    {
        var normalized = NormalizePath(path);
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<BreadcrumbSegment>(parts.Length + 1)
        {
            new("/", "/", parts.Length == 0)
        };
        var current = string.Empty;
        for (var index = 0; index < parts.Length; index++)
        {
            current += "/" + parts[index];
            result.Add(new(parts[index], current, index == parts.Length - 1));
        }
        return result;
    }

    private bool IsCurrentLoad(long version, CancellationTokenSource source)
    {
        lock (_loadSync)
        {
            return !IsDisposed
                && version == _loadVersion
                && ReferenceEquals(_loadCts, source)
                && !source.IsCancellationRequested;
        }
    }

    private async Task PublishErrorAsync(Exception exception)
    {
        if (IsDisposed) return;
        _error = exception.Message;
        if (OperationFailed.HasDelegate) await OperationFailed.InvokeAsync(exception);
    }

    private void ObserveLoad(Task task)
        => ObserveTask(ObserveLoadAsync(task), "OmniFileManager.Load");

    private async Task ObserveLoadAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (IsDisposed) return;
            try
            {
                await DispatchExceptionAsync(exception);
            }
            catch when (IsDisposed)
            {
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;

        CancellationTokenSource? load;
        lock (_loadSync)
        {
            ++_loadVersion;
            load = _loadCts;
            _loadCts = null;
        }
        CancelSafely(load);
        CancelSafely(_lifetimeCts);
        _lifetimeCts.Dispose();
        GC.SuppressFinalize(this);
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private static void CancelSafely(CancellationTokenSource? source)
    {
        if (source is null) return;
        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private sealed record BreadcrumbSegment(string Label, string Path, bool IsLast);
    private enum EditorMode { None, CreateFolder, Rename }
}
