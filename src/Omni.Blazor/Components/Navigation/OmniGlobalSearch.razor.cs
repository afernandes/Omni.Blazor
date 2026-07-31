using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Models;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Embedded global search surface with local/remote providers and keyboard navigation.</summary>
public partial class OmniGlobalSearch
{
    private readonly object _searchSync = new();
    private readonly List<GlobalSearchResult> _results = [];
    private CancellationTokenSource? _searchCts;
    private ParameterState<string?> _queryState = null!;
    private ParameterState<(IEnumerable<GlobalSearchResult>? Items,
        GlobalSearchProvider? Provider,
        int MinQueryLength,
        int MaxResults,
        bool ShowAllWhenEmpty)> _sourceState = null!;
    private string _query = string.Empty;
    private string? _error;
    private long _searchVersion;
    private int _activeIndex;
    private int _disposeState;
    private bool _loading;
    private bool _queryInitialized;
    private bool _sourceInitialized;

    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Optional in-memory result source, combined with provider results.</summary>
    [Parameter] public IEnumerable<GlobalSearchResult>? Items { get; set; }

    /// <summary>Optional asynchronous, cancellable server-side result source.</summary>
    [Parameter] public GlobalSearchProvider? SearchProvider { get; set; }

    /// <summary>Current query for two-way binding.</summary>
    [Parameter] public string? Query { get; set; }

    /// <summary>Raised when <see cref="Query"/> changes.</summary>
    [Parameter] public EventCallback<string?> QueryChanged { get; set; }

    /// <summary>Raised when the user chooses a result.</summary>
    [Parameter] public EventCallback<GlobalSearchResult> ResultSelected { get; set; }

    /// <summary>Raised when a provider throws an uncancelled exception.</summary>
    [Parameter] public EventCallback<Exception> SearchFailed { get; set; }

    /// <summary>Optional custom result renderer.</summary>
    [Parameter] public RenderFragment<GlobalSearchResult>? ResultTemplate { get; set; }

    /// <summary>Optional content displayed before the minimum query length is reached.</summary>
    [Parameter] public RenderFragment? InitialContent { get; set; }

    /// <summary>Delay before invoking <see cref="SearchProvider"/>, in milliseconds.</summary>
    [Parameter] public int Debounce { get; set; } = 250;

    /// <summary>Minimum trimmed query length required to search.</summary>
    [Parameter] public int MinQueryLength { get; set; } = 2;

    /// <summary>Maximum number of combined results retained and rendered.</summary>
    [Parameter] public int MaxResults { get; set; } = 50;

    /// <summary>Shows all local items when the query is empty.</summary>
    [Parameter] public bool ShowAllWhenEmpty { get; set; }

    /// <summary>Navigates to a selected result's URL after raising <see cref="ResultSelected"/>.</summary>
    [Parameter] public bool NavigateOnSelect { get; set; } = true;

    /// <summary>Accessible label for the search landmark.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Busca global";

    /// <summary>Search input placeholder.</summary>
    [Parameter] public string Placeholder { get; set; } = "Buscar em todo o sistema...";

    /// <summary>Hint shown before the minimum query length is reached.</summary>
    [Parameter] public string HintText { get; set; } = "Digite para buscar";

    /// <summary>Message shown while the provider is running.</summary>
    [Parameter] public string LoadingText { get; set; } = "Buscando...";

    /// <summary>Message shown when no result matches.</summary>
    [Parameter] public string EmptyText { get; set; } = "Nenhum resultado encontrado.";

    /// <summary>Message shown when the provider fails.</summary>
    [Parameter] public string ErrorText { get; set; } = "Não foi possível concluir a busca.";

    /// <summary>Accessible label for the clear-query action.</summary>
    [Parameter] public string ClearText { get; set; } = "Limpar busca";

    /// <summary>Whether an asynchronous provider request is currently active.</summary>
    public bool IsLoading => _loading;

    /// <summary>Current immutable result view.</summary>
    public IReadOnlyList<GlobalSearchResult> Results => _results;

    private string ResultsId => $"{Id}-results";
    private string? ActiveDescendant =>
        _results.Count > 0 && _activeIndex >= 0 && _activeIndex < _results.Count
            ? ResultId(_activeIndex)
            : null;

    private string RootCss => CssBuilder.Default("omni-global-search")
        .AddClass("omni-global-search-loading", _loading)
        .AddClass(Class)
        .Build();

    protected override void OnInitialized()
    {
        _queryState = RegisterParameter<string?>(nameof(Query))
            .WithParameter(() => Query)
            .WithEventCallback(() => QueryChanged)
            .WithChangeHandler(SynchronizeQueryParameter)
            .Attach();

        _sourceState = RegisterParameter<(IEnumerable<GlobalSearchResult>?,
            GlobalSearchProvider?,
            int,
            int,
            bool)>("SearchSource")
            .WithParameter(() => (Items, SearchProvider, MinQueryLength, MaxResults, ShowAllWhenEmpty))
            .WithChangeHandler(SynchronizeSource)
            .Attach();
    }

    /// <summary>Runs a search immediately, without the configured debounce.</summary>
    public Task SearchAsync(string? query, CancellationToken cancellationToken = default)
        => SetQueryAndSearchAsync(query ?? string.Empty, useDebounce: false, cancellationToken);

    /// <summary>Repeats the current search immediately.</summary>
    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => RunSearchAsync(_query, useDebounce: false, cancellationToken);

    private void SynchronizeQueryParameter()
    {
        var next = Query ?? string.Empty;
        if (_queryInitialized && string.Equals(next, _query, StringComparison.Ordinal)) return;
        _queryInitialized = true;
        _query = next;
        ObserveSearch(RunSearchAsync(next, useDebounce: true, CancellationToken.None));
    }

    private void SynchronizeSource()
    {
        if (!_sourceInitialized)
        {
            _sourceInitialized = true;
            return;
        }
        ObserveSearch(RunSearchAsync(_query, useDebounce: false, CancellationToken.None));
    }

    private Task OnInputAsync(ChangeEventArgs args)
        => SetQueryAndSearchAsync(args.Value?.ToString() ?? string.Empty, useDebounce: true);

    private async Task SetQueryAndSearchAsync(
        string query,
        bool useDebounce,
        CancellationToken cancellationToken = default)
    {
        _query = query;
        Query = query;
        _activeIndex = 0;
        _error = null;
        if (QueryChanged.HasDelegate) await QueryChanged.InvokeAsync(query);
        await RunSearchAsync(query, useDebounce, cancellationToken);
    }

    private async Task RunSearchAsync(
        string query,
        bool useDebounce,
        CancellationToken cancellationToken)
    {
        var trimmed = query.Trim();
        var canSearch = trimmed.Length >= Math.Max(0, MinQueryLength)
            || (ShowAllWhenEmpty && trimmed.Length == 0);

        var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationTokenSource? previous;
        long version;
        lock (_searchSync)
        {
            if (IsDisposed)
            {
                linked.Dispose();
                return;
            }

            version = ++_searchVersion;
            previous = _searchCts;
            _searchCts = linked;
        }
        CancelSafely(previous);

        try
        {
            _error = null;
            _activeIndex = 0;
            if (!canSearch)
            {
                _results.Clear();
                _loading = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            ReplaceResults(FilterLocal(trimmed));
            _loading = SearchProvider is not null;
            await InvokeAsync(StateHasChanged);

            if (SearchProvider is null) return;
            if (useDebounce && Debounce > 0)
                await Task.Delay(Debounce, linked.Token);

            var request = new GlobalSearchRequest(trimmed, Math.Max(1, MaxResults));
            var remote = await SearchProvider(request, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (!IsCurrent(version, linked)) return;

            MergeRemote(remote);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            // Superseded/disposed searches never publish stale state.
        }
        catch (Exception exception)
        {
            if (!IsCurrent(version, linked)) return;
            _error = exception.Message;
            _results.Clear();
            if (SearchFailed.HasDelegate) await SearchFailed.InvokeAsync(exception);
        }
        finally
        {
            var isCurrent = IsCurrent(version, linked);
            lock (_searchSync)
            {
                if (ReferenceEquals(_searchCts, linked)) _searchCts = null;
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

    private IEnumerable<GlobalSearchResult> FilterLocal(string query)
    {
        if (Items is null) return [];
        var limit = Math.Max(1, MaxResults);
        if (query.Length == 0) return Items.Take(limit);

        return Items
            .Where(item => Matches(item, query))
            .Take(limit);
    }

    private void MergeRemote(IReadOnlyList<GlobalSearchResult>? remote)
    {
        if (remote is null || remote.Count == 0) return;
        var limit = Math.Max(1, MaxResults);
        var ids = new HashSet<string>(_results.Select(result => result.Id), StringComparer.Ordinal);
        foreach (var result in remote)
        {
            if (_results.Count >= limit) break;
            if (result is not null && ids.Add(result.Id)) _results.Add(result);
        }
    }

    private void ReplaceResults(IEnumerable<GlobalSearchResult> source)
    {
        _results.Clear();
        _results.AddRange(source);
    }

    private static bool Matches(GlobalSearchResult result, string query)
    {
        if (result.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || (result.Description?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (result.Category?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false))
        {
            return true;
        }

        foreach (var keyword in result.Keywords)
        {
            if (keyword.Contains(query, StringComparison.CurrentCultureIgnoreCase)) return true;
        }
        return false;
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowDown" when _results.Count > 0:
                _activeIndex = (_activeIndex + 1) % _results.Count;
                break;
            case "ArrowUp" when _results.Count > 0:
                _activeIndex = (_activeIndex - 1 + _results.Count) % _results.Count;
                break;
            case "Enter" when _activeIndex >= 0 && _activeIndex < _results.Count:
                await SelectAsync(_results[_activeIndex]);
                break;
            case "Escape":
                await ClearAsync();
                break;
        }
    }

    private async Task SelectAsync(GlobalSearchResult result)
    {
        if (ResultSelected.HasDelegate) await ResultSelected.InvokeAsync(result);
        if (NavigateOnSelect && !string.IsNullOrWhiteSpace(result.Url))
            Navigation.NavigateTo(result.Url);
    }

    private Task ClearAsync() => SetQueryAndSearchAsync(string.Empty, useDebounce: false);

    private string ResultId(int index) => $"{Id}-result-{index}";

    private static string ResultCss(bool active) => CssBuilder.Default("omni-global-search-result")
        .AddClass("omni-active", active)
        .Build();

    private bool IsCurrent(long version, CancellationTokenSource source)
    {
        lock (_searchSync)
        {
            return !IsDisposed
                && version == _searchVersion
                && ReferenceEquals(_searchCts, source)
                && !source.IsCancellationRequested;
        }
    }

    private void ObserveSearch(Task task)
    {
        ObserveTask(ObserveSearchAsync(task), "OmniGlobalSearch.Search");
    }

    private async Task ObserveSearchAsync(Task task)
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

        CancellationTokenSource? active;
        lock (_searchSync)
        {
            ++_searchVersion;
            active = _searchCts;
            _searchCts = null;
        }
        CancelSafely(active);
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
}
