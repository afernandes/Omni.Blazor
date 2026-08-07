using Omni.Blazor.Models;
using Omni.Blazor.Services;

namespace Omni.Blazor.Tests.Services;

public sealed class OmniEntityEditorTests
{
    private sealed record Item(int Id, string Name);

    [Fact]
    public async Task Local_mutations_are_keyed_and_report_collection_changes()
    {
        List<Item> items = [new(1, "Before")];
        using OmniEntityEditor<Item, int> editor = new(static item => item.Id);

        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        EntityMutationResult<Item> created = await editor.CreateAsync(new Item(2, "Created"), items, cancellationToken: cancellationToken);
        EntityMutationResult<Item> updated = await editor.UpdateAsync(items[0], new Item(1, "After"), items, cancellationToken: cancellationToken);
        EntityMutationResult<Item> deleted = await editor.DeleteAsync(items[1], items, cancellationToken: cancellationToken);

        Assert.All([created, updated, deleted], result => Assert.True(result.Succeeded));
        Assert.All([created, updated, deleted], result => Assert.True(result.LocalCollectionChanged));
        Assert.Equal([new Item(1, "After")], items);
    }

    [Fact]
    public async Task Duplicate_create_and_key_change_are_structured_conflicts()
    {
        List<Item> items = [new(1, "Existing")];
        using OmniEntityEditor<Item, int> editor = new(static item => item.Id);

        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        EntityMutationResult<Item> duplicate = await editor.CreateAsync(new Item(1, "Duplicate"), items, cancellationToken: cancellationToken);
        EntityMutationResult<Item> keyChange = await editor.UpdateAsync(items[0], new Item(2, "Changed"), items, cancellationToken: cancellationToken);

        Assert.Equal(EntityMutationStatus.Conflict, duplicate.Status);
        Assert.Equal(EntityMutationStatus.ValidationFailed, keyChange.Status);
        Assert.Single(items);
    }

    [Fact]
    public async Task Concurrent_operation_for_the_same_key_returns_busy()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DelegateEntityMutationProvider<Item, int> provider = new(
            static (item, _) => ValueTask.FromResult(EntityMutationResult<Item>.Success(item)),
            async (key, item, cancellationToken) =>
            {
                await release.Task.WaitAsync(cancellationToken);
                return EntityMutationResult<Item>.Success(item);
            },
            static (_, _) => ValueTask.FromResult(EntityMutationResult<Item>.Deleted()));
        using OmniEntityEditor<Item, int> editor = new(static item => item.Id);

        CancellationToken cancellationToken = Xunit.TestContext.Current.CancellationToken;
        ValueTask<EntityMutationResult<Item>> first = editor.UpdateAsync(
            new Item(1, "A"), new Item(1, "B"), null, provider, cancellationToken);
        EntityMutationResult<Item> second = await editor.DeleteAsync(
            new Item(1, "A"), null, provider, cancellationToken: cancellationToken);
        release.SetResult();
        EntityMutationResult<Item> completed = await first;

        Assert.Equal(EntityMutationStatus.Busy, second.Status);
        Assert.True(completed.Succeeded);
        Assert.False(editor.IsBusy);
    }

    [Fact]
    public async Task Dispose_cancels_provider_work_and_releases_busy_state()
    {
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DelegateEntityMutationProvider<Item, int> provider = new(
            async (item, cancellationToken) =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return EntityMutationResult<Item>.Success(item);
            },
            static (_, item, _) => ValueTask.FromResult(EntityMutationResult<Item>.Success(item)),
            static (_, _) => ValueTask.FromResult(EntityMutationResult<Item>.Deleted()));
        OmniEntityEditor<Item, int> editor = new(static item => item.Id);

        ValueTask<EntityMutationResult<Item>> pending = editor.CreateAsync(
            new Item(1, "A"), null, provider, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        await started.Task;
        editor.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
        Assert.False(editor.IsBusy);
    }
}
