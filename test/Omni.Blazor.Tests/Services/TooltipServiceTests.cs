namespace Omni.Blazor.Tests.Services;

/// <summary>
/// State + event behaviour of <see cref="TooltipService"/>. It is pure in-memory
/// state (no IJSRuntime), so every assertion targets observable state
/// (IsOpen/X/Y/Text/Content), the Open/Close transitions, the Close guard, and
/// the <see cref="TooltipService.OnChange"/> event fan-out.
/// </summary>
public class TooltipServiceTests : TestContextBase
{
    [Fact]
    public void Starts_closed_and_empty()
    {
        var svc = new TooltipService();

        Assert.False(svc.IsOpen);
        Assert.Null(svc.Text);
        Assert.Null(svc.Content);
        Assert.Equal(0d, svc.X);
        Assert.Equal(0d, svc.Y);
    }

    [Fact]
    public void Open_with_text_sets_position_text_and_opens()
    {
        var svc = new TooltipService();

        svc.Open(12.5, 34.5, "hello");

        Assert.True(svc.IsOpen);
        Assert.Equal(12.5, svc.X);
        Assert.Equal(34.5, svc.Y);
        Assert.Equal("hello", svc.Text);
        Assert.Null(svc.Content);
    }

    [Fact]
    public void Open_with_content_sets_content_and_clears_text()
    {
        var svc = new TooltipService();
        RenderFragment fragment = builder => builder.AddContent(0, "rich");

        svc.Open(1, 2, fragment);

        Assert.True(svc.IsOpen);
        Assert.Equal(1d, svc.X);
        Assert.Equal(2d, svc.Y);
        Assert.Same(fragment, svc.Content);
        Assert.Null(svc.Text);
    }

    [Fact]
    public void Open_content_after_text_swaps_mode_and_nulls_text()
    {
        var svc = new TooltipService();
        RenderFragment fragment = builder => builder.AddContent(0, "rich");

        svc.Open(0, 0, "plain");
        svc.Open(5, 6, fragment);

        Assert.Same(fragment, svc.Content);
        Assert.Null(svc.Text);
        Assert.Equal(5d, svc.X);
        Assert.Equal(6d, svc.Y);
    }

    [Fact]
    public void Open_text_after_content_swaps_mode_and_nulls_content()
    {
        var svc = new TooltipService();
        RenderFragment fragment = builder => builder.AddContent(0, "rich");

        svc.Open(0, 0, fragment);
        svc.Open(7, 8, "plain");

        Assert.Equal("plain", svc.Text);
        Assert.Null(svc.Content);
    }

    [Fact]
    public void Close_when_open_clears_state()
    {
        var svc = new TooltipService();
        svc.Open(10, 20, "hello");

        svc.Close();

        Assert.False(svc.IsOpen);
        Assert.Null(svc.Text);
        Assert.Null(svc.Content);
    }

    [Fact]
    public void Open_raises_OnChange()
    {
        var svc = new TooltipService();
        var fired = 0;
        svc.OnChange += () => fired++;

        svc.Open(1, 1, "a");

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Each_Open_raises_OnChange()
    {
        var svc = new TooltipService();
        var fired = 0;
        svc.OnChange += () => fired++;

        svc.Open(1, 1, "a");
        svc.Open(2, 2, "b");

        Assert.Equal(2, fired);
    }

    [Fact]
    public void Close_when_open_raises_OnChange()
    {
        var svc = new TooltipService();
        svc.Open(1, 1, "a");
        var fired = 0;
        svc.OnChange += () => fired++;

        svc.Close();

        Assert.Equal(1, fired);
    }

    [Fact]
    public void Close_when_already_closed_is_a_no_op_and_does_not_raise_OnChange()
    {
        var svc = new TooltipService();
        var fired = 0;
        svc.OnChange += () => fired++;

        svc.Close();

        Assert.False(svc.IsOpen);
        Assert.Equal(0, fired);
    }
}
