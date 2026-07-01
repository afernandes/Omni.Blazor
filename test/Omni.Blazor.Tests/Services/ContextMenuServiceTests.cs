namespace Omni.Blazor.Tests.Services;

/// <summary>
/// In-memory behaviour of <see cref="ContextMenuService"/>. This service is pure
/// state (no <c>IJSRuntime</c>), so the tests assert observable state
/// (<c>IsOpen</c>/<c>X</c>/<c>Y</c>/<c>Items</c>), the Open/Close transitions,
/// the Close-while-closed guard, and <c>OnChange</c> event firing.
/// </summary>
public class ContextMenuServiceTests : TestContextBase
{
    private static ContextMenuItem Item(string text) => new() { Text = text };

    [Fact]
    public void Default_state_is_closed_and_empty()
    {
        var svc = new ContextMenuService();
        Assert.False(svc.IsOpen);
        Assert.Equal(0d, svc.X);
        Assert.Equal(0d, svc.Y);
        Assert.Empty(svc.Items);
    }

    [Fact]
    public void Open_with_coordinates_sets_position_items_and_flag()
    {
        var svc = new ContextMenuService();
        var items = new[] { Item("Copy"), Item("Paste") };

        svc.Open(12.5, 34.5, items);

        Assert.True(svc.IsOpen);
        Assert.Equal(12.5, svc.X);
        Assert.Equal(34.5, svc.Y);
        Assert.Equal(2, svc.Items.Count);
        Assert.Equal("Copy", svc.Items[0].Text);
        Assert.Equal("Paste", svc.Items[1].Text);
    }

    [Fact]
    public void Open_with_mouse_event_uses_client_coordinates()
    {
        var svc = new ContextMenuService();
        var args = new MouseEventArgs { ClientX = 80, ClientY = 120 };

        svc.Open(args, new[] { Item("Rename") });

        Assert.True(svc.IsOpen);
        Assert.Equal(80d, svc.X);
        Assert.Equal(120d, svc.Y);
        Assert.Single(svc.Items);
    }

    [Fact]
    public void Open_materializes_items_into_an_independent_list()
    {
        var svc = new ContextMenuService();
        var source = new List<ContextMenuItem> { Item("A") };

        svc.Open(0, 0, source);
        source.Add(Item("B")); // mutate the caller's collection afterwards

        Assert.Single(svc.Items); // service kept its own snapshot
    }

    [Fact]
    public void Open_raises_OnChange()
    {
        var svc = new ContextMenuService();
        var count = 0;
        svc.OnChange += () => count++;

        svc.Open(1, 1, new[] { Item("X") });

        Assert.Equal(1, count);
    }

    [Fact]
    public void Close_after_open_resets_flag_and_clears_items_and_raises_OnChange()
    {
        var svc = new ContextMenuService();
        svc.Open(5, 5, new[] { Item("X") });

        var count = 0;
        svc.OnChange += () => count++;

        svc.Close();

        Assert.False(svc.IsOpen);
        Assert.Empty(svc.Items);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Close_while_already_closed_is_a_noop_and_does_not_raise_OnChange()
    {
        var svc = new ContextMenuService();
        var count = 0;
        svc.OnChange += () => count++;

        svc.Close();

        Assert.False(svc.IsOpen);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Reopening_replaces_position_and_items()
    {
        var svc = new ContextMenuService();
        svc.Open(1, 2, new[] { Item("First") });
        svc.Open(9, 8, new[] { Item("Second"), Item("Third") });

        Assert.True(svc.IsOpen);
        Assert.Equal(9d, svc.X);
        Assert.Equal(8d, svc.Y);
        Assert.Equal(2, svc.Items.Count);
        Assert.Equal("Second", svc.Items[0].Text);
    }

    [Fact]
    public void Separator_factory_produces_a_separator_item()
    {
        var sep = ContextMenuItem.Separator();
        Assert.True(sep.IsSeparator);
        Assert.Null(sep.Text);
    }
}
