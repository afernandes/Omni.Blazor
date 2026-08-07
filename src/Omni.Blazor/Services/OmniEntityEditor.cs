using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

/// <summary>
/// Headless, UI-independent coordinator for cancellable create, edit and delete
/// operations against either an owned list or a persistence provider.
/// </summary>
public sealed class OmniEntityEditor<TItem, TKey> : IDisposable
    where TItem : class
    where TKey : notnull
{
    private readonly object _sync = new();
    private readonly Func<TItem, TKey> _keySelector;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<TKey, CancellationTokenSource> _rowOperations = [];
    private CancellationTokenSource? _createOperation;
    private int _activeOperations;
    private bool _disposed;
    private bool _cancellationCompleted;
    private bool _lifetimeDisposed;

    /// <summary>Creates a coordinator with a stable key selector.</summary>
    public OmniEntityEditor(Func<TItem, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        _keySelector = keySelector;
    }

    /// <summary>Whether one or more mutations are currently active.</summary>
    public bool IsBusy
    {
        get
        {
            lock (_sync) return _activeOperations != 0;
        }
    }

    /// <summary>Whether a mutation is active for the supplied stable key.</summary>
    public bool IsBusyFor(TKey key)
    {
        lock (_sync) return _rowOperations.ContainsKey(key);
    }

    /// <summary>Creates an entity through the provider or the owned local list.</summary>
    public async ValueTask<EntityMutationResult<TItem>> CreateAsync(
        TItem item,
        IList<TItem>? items,
        IOmniEntityMutationProvider<TItem, TKey>? provider = null,
        int maximumItems = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (maximumItems < 1) throw new ArgumentOutOfRangeException(nameof(maximumItems));
        if (!TryBeginCreate(cancellationToken, out CancellationTokenSource operation))
            return EntityMutationResult<TItem>.Busy("An entity create operation is already running.");

        try
        {
            if (provider is not null)
            {
                EntityMutationResult<TItem> result = await provider.CreateAsync(item, operation.Token);
                if (result.Succeeded && result.Item is null)
                    return EntityMutationResult<TItem>.Failure(
                        "A successful entity create result must contain the authoritative item.");
                return result;
            }

            IList<TItem> source = RequireMutableItems(items);
            if (source.Count >= maximumItems)
                return EntityMutationResult<TItem>.Failure("The configured maximum entity count was reached.");
            TKey key = _keySelector(item);
            if (FindUniqueIndex(source, key) >= 0)
                return EntityMutationResult<TItem>.Conflict(message: $"An entity with key '{key}' already exists.");
            source.Add(item);
            return EntityMutationResult<TItem>.Success(item, localCollectionChanged: true);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return EntityMutationResult<TItem>.Failure(exception.Message, exception);
        }
        finally
        {
            EndCreate(operation);
        }
    }

    /// <summary>Updates an entity through the provider or replaces it in the owned local list.</summary>
    public async ValueTask<EntityMutationResult<TItem>> UpdateAsync(
        TItem sourceItem,
        TItem editedItem,
        IList<TItem>? items,
        IOmniEntityMutationProvider<TItem, TKey>? provider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceItem);
        ArgumentNullException.ThrowIfNull(editedItem);
        TKey key;
        try
        {
            key = _keySelector(sourceItem);
            TKey editedKey = _keySelector(editedItem);
            if (!EqualityComparer<TKey>.Default.Equals(key, editedKey))
                return EntityMutationResult<TItem>.ValidationFailed(
                    ["A stable entity key cannot change during edit."]);
        }
        catch (Exception exception)
        {
            return EntityMutationResult<TItem>.Failure(exception.Message, exception);
        }

        if (!TryBeginRow(key, cancellationToken, out CancellationTokenSource operation))
            return EntityMutationResult<TItem>.Busy($"An entity operation is already running for key '{key}'.");

        try
        {
            if (provider is not null)
            {
                EntityMutationResult<TItem> result = await provider.UpdateAsync(key, editedItem, operation.Token);
                if (result.Succeeded && result.Item is null)
                    return EntityMutationResult<TItem>.Failure(
                        "A successful entity update result must contain the authoritative item.");
                return result;
            }

            IList<TItem> source = RequireMutableItems(items);
            int index = FindUniqueIndex(source, key);
            if (index < 0) return EntityMutationResult<TItem>.NotFound();
            source[index] = editedItem;
            return EntityMutationResult<TItem>.Success(editedItem, localCollectionChanged: true);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return EntityMutationResult<TItem>.Failure(exception.Message, exception);
        }
        finally
        {
            EndRow(key, operation);
        }
    }

    /// <summary>Deletes an entity through the provider or removes it from the owned local list.</summary>
    public async ValueTask<EntityMutationResult<TItem>> DeleteAsync(
        TItem item,
        IList<TItem>? items,
        IOmniEntityMutationProvider<TItem, TKey>? provider = null,
        int minimumItems = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (minimumItems < 0) throw new ArgumentOutOfRangeException(nameof(minimumItems));
        TKey key;
        try
        {
            key = _keySelector(item);
        }
        catch (Exception exception)
        {
            return EntityMutationResult<TItem>.Failure(exception.Message, exception);
        }

        if (!TryBeginRow(key, cancellationToken, out CancellationTokenSource operation))
            return EntityMutationResult<TItem>.Busy($"An entity operation is already running for key '{key}'.");

        try
        {
            if (provider is not null)
                return await provider.DeleteAsync(key, operation.Token);

            IList<TItem> source = RequireMutableItems(items);
            if (source.Count <= minimumItems)
                return EntityMutationResult<TItem>.ValidationFailed(
                    ["The configured minimum entity count was reached."]);
            int index = FindUniqueIndex(source, key);
            if (index < 0) return EntityMutationResult<TItem>.NotFound();
            source.RemoveAt(index);
            return EntityMutationResult<TItem>.Deleted(localCollectionChanged: true);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return EntityMutationResult<TItem>.Failure(exception.Message, exception);
        }
        finally
        {
            EndRow(key, operation);
        }
    }

    private bool TryBeginCreate(
        CancellationToken cancellationToken,
        out CancellationTokenSource operation)
    {
        lock (_sync)
        {
            if (_disposed || _createOperation is not null)
            {
                operation = null!;
                return false;
            }
            operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
            _createOperation = operation;
            _activeOperations++;
            return true;
        }
    }

    private bool TryBeginRow(
        TKey key,
        CancellationToken cancellationToken,
        out CancellationTokenSource operation)
    {
        lock (_sync)
        {
            if (_disposed || _rowOperations.ContainsKey(key))
            {
                operation = null!;
                return false;
            }
            operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);
            _rowOperations.Add(key, operation);
            _activeOperations++;
            return true;
        }
    }

    private void EndCreate(CancellationTokenSource operation)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_createOperation, operation)) _createOperation = null;
            CompleteOperationLocked();
        }
        operation.Dispose();
    }

    private void EndRow(TKey key, CancellationTokenSource operation)
    {
        lock (_sync)
        {
            if (_rowOperations.TryGetValue(key, out CancellationTokenSource? current)
                && ReferenceEquals(current, operation))
                _rowOperations.Remove(key);
            CompleteOperationLocked();
        }
        operation.Dispose();
    }

    private void CompleteOperationLocked()
    {
        _activeOperations--;
        if (_disposed && _cancellationCompleted && _activeOperations == 0 && !_lifetimeDisposed)
        {
            _lifetimeDisposed = true;
            _lifetime.Dispose();
        }
    }

    private int FindUniqueIndex(IList<TItem> items, TKey key)
    {
        int found = -1;
        for (int index = 0; index < items.Count; index++)
        {
            if (!EqualityComparer<TKey>.Default.Equals(_keySelector(items[index]), key)) continue;
            if (found >= 0)
                throw new InvalidOperationException($"The entity source contains duplicate key '{key}'.");
            found = index;
        }
        return found;
    }

    private static IList<TItem> RequireMutableItems(IList<TItem>? items)
    {
        if (items is null)
            throw new InvalidOperationException("A local entity operation requires an Items source.");
        if (items.IsReadOnly)
            throw new InvalidOperationException("The local entity source is read-only.");
        return items;
    }

    /// <summary>Cancels active operations. Their owners observe cancellation and release token sources.</summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _lifetime.Cancel();

        lock (_sync)
        {
            _cancellationCompleted = true;
            if (_activeOperations == 0 && !_lifetimeDisposed)
            {
                _lifetimeDisposed = true;
                _lifetime.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }
}
