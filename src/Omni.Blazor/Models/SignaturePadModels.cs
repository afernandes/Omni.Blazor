namespace Omni.Blazor.Models;

/// <summary>Output format produced by <c>OmniSignaturePad</c>.</summary>
public enum SignaturePadFormat
{
    /// <summary>Portable Network Graphics with lossless compression.</summary>
    Png,

    /// <summary>JPEG raster image using the configured quality.</summary>
    Jpeg,

    /// <summary>Scalable Vector Graphics generated from the captured strokes.</summary>
    Svg
}

