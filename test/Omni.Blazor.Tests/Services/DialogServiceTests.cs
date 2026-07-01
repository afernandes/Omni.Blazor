namespace Omni.Blazor.Tests.Services;

/// <summary>
/// In-memory behaviour of <see cref="DialogService"/>. The service holds no
/// JS dependency (pure state + a <see cref="TaskCompletionSource{T}"/> per
/// open dialog), so these tests exercise the observable state: the open-dialog
/// lists, the OnChange event, task resolution on close, side-dialog exclusivity
/// and the topmost open-order resolution.
/// </summary>
public class DialogServiceTests : TestContextBase
{
    // A minimal component to open — never actually rendered here; the service
    // only stores its Type on the DialogReference.
    private sealed class DummyDialog : ComponentBase { }

    [Fact]
    public void OpenAsync_adds_a_dialog_and_fires_OnChange()
    {
        var svc = new DialogService();
        var changes = 0;
        svc.OnChange += () => changes++;

        var task = svc.OpenAsync<DummyDialog>("Hello");

        Assert.Single(svc.OpenDialogs);
        Assert.Equal("Hello", svc.OpenDialogs[0].Title);
        Assert.False(svc.OpenDialogs[0].IsSide);
        Assert.False(task.IsCompleted); // stays pending until closed
        Assert.Equal(1, changes);
    }

    [Fact]
    public void OpenAsync_uses_a_default_options_when_none_supplied()
    {
        var svc = new DialogService();

        svc.OpenAsync<DummyDialog>("Untitled");

        Assert.NotNull(svc.OpenDialogs[0].Options);
        Assert.IsType<DialogOptions>(svc.OpenDialogs[0].Options);
    }

    [Fact]
    public void OpenAsync_stacks_multiple_main_dialogs()
    {
        var svc = new DialogService();

        svc.OpenAsync<DummyDialog>("First");
        svc.OpenAsync<DummyDialog>("Second");

        Assert.Equal(2, svc.OpenDialogs.Count);
        Assert.Equal("Second", svc.OpenDialogs[^1].Title);
    }

    [Fact]
    public async Task Close_resolves_the_topmost_task_and_removes_it()
    {
        var svc = new DialogService();
        var task = svc.OpenAsync<DummyDialog>("Closable");

        svc.Close("done");

        Assert.Empty(svc.OpenDialogs);
        var result = await task;
        Assert.Equal("done", result);
    }

    [Fact]
    public void Close_with_no_open_dialog_is_a_no_op()
    {
        var svc = new DialogService();
        var changes = 0;
        svc.OnChange += () => changes++;

        svc.Close("ignored");

        Assert.Empty(svc.OpenDialogs);
        Assert.Equal(0, changes); // guard: no event when nothing to close
    }

    [Fact]
    public void OpenSideAsync_sets_the_side_dialog_and_fires_OnChange()
    {
        var svc = new DialogService();
        var changes = 0;
        svc.OnChange += () => changes++;

        svc.OpenSideAsync<DummyDialog>("Drawer");

        Assert.NotNull(svc.OpenSideDialog);
        Assert.Equal("Drawer", svc.OpenSideDialog!.Title);
        Assert.True(svc.OpenSideDialog.IsSide);
        Assert.IsType<SideDialogOptions>(svc.OpenSideDialog.Options);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task OpenSideAsync_replaces_and_cancels_a_previous_side_dialog()
    {
        var svc = new DialogService();
        var first = svc.OpenSideAsync<DummyDialog>("Old");

        var second = svc.OpenSideAsync<DummyDialog>("New");

        // only one side dialog can be open at a time
        Assert.Equal("New", svc.OpenSideDialog!.Title);
        // the replaced side dialog's task is resolved (with null)
        var firstResult = await first;
        Assert.Null(firstResult);
        Assert.False(second.IsCompleted);
    }

    [Fact]
    public async Task CloseSide_resolves_the_side_task_and_clears_it()
    {
        var svc = new DialogService();
        var task = svc.OpenSideAsync<DummyDialog>("Drawer");

        svc.CloseSide("result");

        Assert.Null(svc.OpenSideDialog);
        var result = await task;
        Assert.Equal("result", result);
    }

    [Fact]
    public void CloseSide_with_no_side_open_is_a_no_op()
    {
        var svc = new DialogService();
        var changes = 0;
        svc.OnChange += () => changes++;

        svc.CloseSide();

        Assert.Null(svc.OpenSideDialog);
        Assert.Equal(0, changes); // guard: no event when there's nothing to close
    }

    [Fact]
    public async Task Close_targets_the_side_dialog_when_it_was_opened_last()
    {
        var svc = new DialogService();
        var mainTask = svc.OpenAsync<DummyDialog>("Main");
        var sideTask = svc.OpenSideAsync<DummyDialog>("Side"); // opened later => topmost

        svc.Close("via-close");

        // topmost is the side dialog, so Close closes it (not the main)
        Assert.Null(svc.OpenSideDialog);
        Assert.Single(svc.OpenDialogs);
        Assert.Equal("Main", svc.OpenDialogs[0].Title);
        Assert.False(mainTask.IsCompleted);
        var sideResult = await sideTask;
        Assert.Equal("via-close", sideResult);
    }

    [Fact]
    public async Task Close_targets_the_last_main_dialog_when_it_was_opened_after_the_side()
    {
        var svc = new DialogService();
        var sideTask = svc.OpenSideAsync<DummyDialog>("Side");
        var mainTask = svc.OpenAsync<DummyDialog>("Main"); // opened later => topmost

        svc.Close("main-result");

        // topmost is the main dialog, so the side stays open
        Assert.NotNull(svc.OpenSideDialog);
        Assert.Empty(svc.OpenDialogs);
        Assert.False(sideTask.IsCompleted);
        var mainResult = await mainTask;
        Assert.Equal("main-result", mainResult);
    }

    [Fact]
    public void Each_dialog_reference_gets_a_stable_unique_id()
    {
        var svc = new DialogService();

        svc.OpenAsync<DummyDialog>("A");
        svc.OpenAsync<DummyDialog>("B");

        var id0 = svc.OpenDialogs[0].Id;
        var id1 = svc.OpenDialogs[1].Id;
        Assert.StartsWith("omni-dlg-", id0);
        Assert.NotEqual(id0, id1);
    }
}
