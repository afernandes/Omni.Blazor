using System.Globalization;
using System.Text.RegularExpressions;
using Omni.Blazor.Utilities;
using Xunit;

namespace Omni.Blazor.Tests.Utilities;

public class MonotoneSplineTests
{
    private static int Segments(string d) => Regex.Matches(d, " C ").Count;

    [Fact]
    public void Empty_returns_empty() => Assert.Equal(string.Empty, MonotoneSpline.Path([]));

    [Fact]
    public void Null_returns_empty() => Assert.Equal(string.Empty, MonotoneSpline.Path(null!));

    [Fact]
    public void Single_point_is_moveTo_only()
    {
        (double X, double Y)[] pts = [(1, 2)];
        Assert.Equal("M 1 2", MonotoneSpline.Path(pts));
    }

    [Fact]
    public void Two_points_make_one_cubic_segment()
    {
        (double X, double Y)[] pts = [(0, 0), (10, 10)];
        string d = MonotoneSpline.Path(pts);
        Assert.StartsWith("M 0 0", d);
        Assert.Equal(1, Segments(d));
        Assert.EndsWith("10 10", d);   // curve passes through the last point
    }

    [Fact]
    public void Passes_through_every_point_with_n_minus_1_segments()
    {
        (double X, double Y)[] pts = [(0, 0), (1, 5), (2, 3), (3, 8)];
        string d = MonotoneSpline.Path(pts);
        Assert.Equal(3, Segments(d));
        Assert.StartsWith("M 0 0", d);
        Assert.Contains("1 5", d);
        Assert.Contains("2 3", d);
        Assert.EndsWith("3 8", d);
    }

    [Fact]
    public void Peak_changes_slope_sign_and_stays_valid()
    {
        // up then down: opposite slope signs at the peak → tangent zeroed (no overshoot)
        (double X, double Y)[] pts = [(0, 0), (1, 10), (2, 0)];
        string d = MonotoneSpline.Path(pts);
        Assert.Equal(2, Segments(d));
        Assert.EndsWith("2 0", d);
    }

    [Fact]
    public void Flat_run_has_zero_slope()
    {
        // equal Y values exercise the zero-slope adjustment
        (double X, double Y)[] pts = [(0, 5), (1, 5), (2, 5)];
        string d = MonotoneSpline.Path(pts);
        Assert.StartsWith("M 0 5", d);
        Assert.EndsWith("2 5", d);
    }

    [Fact]
    public void Steep_then_gentle_triggers_fritsch_carlson_clamp()
    {
        // a big rise then a tiny one pushes a^2 + b^2 > 9 → tangent clamping branch
        (double X, double Y)[] pts = [(0, 0), (1, 100), (2, 101)];
        string d = MonotoneSpline.Path(pts);
        Assert.Equal(2, Segments(d));
        Assert.EndsWith("2 101", d);
    }

    [Fact]
    public void Vertical_segment_has_zero_dx()
    {
        // dx == 0 between the first two points exercises the divide-by-zero guard
        (double X, double Y)[] pts = [(0, 0), (0, 10), (1, 10)];
        string d = MonotoneSpline.Path(pts);
        Assert.StartsWith("M 0 0", d);
        Assert.Equal(2, Segments(d));
    }

    [Fact]
    public void Numbers_are_invariant_and_trimmed()
    {
        // F = "0.###" with InvariantCulture: even under a ',' decimal-separator culture
        // the output must use '.' (and trim trailing zeros).
        CultureInfo prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            (double X, double Y)[] pts = [(1.5, 2.25), (3.0, 4.0)];
            string d = MonotoneSpline.Path(pts);
            Assert.StartsWith("M 1.5 2.25", d);
            Assert.EndsWith("3 4", d);
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }
}
