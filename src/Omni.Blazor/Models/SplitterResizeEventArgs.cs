namespace Omni.Blazor.Models;

/// <summary>
/// Event args for <c>OmniSplitter.OnResize</c> and <c>OnResizeEnd</c>.
/// </summary>
public sealed class SplitterResizeEventArgs
{
    public SplitterResizeEventArgs(int barIndex, double startPanePercent, double endPanePercent)
    {
        BarIndex = barIndex;
        StartPanePercent = startPanePercent;
        EndPanePercent = endPanePercent;
    }

    /// <summary>Index of the bar that was dragged (0-based, between pane[BarIndex] and pane[BarIndex+1]).</summary>
    public int BarIndex { get; }

    /// <summary>New size (% of container) of the pane on the START side of the bar.</summary>
    public double StartPanePercent { get; }

    /// <summary>New size (% of container) of the pane on the END side of the bar.</summary>
    public double EndPanePercent { get; }
}
