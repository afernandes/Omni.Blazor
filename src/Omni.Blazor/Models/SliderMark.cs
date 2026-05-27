namespace Omni.Blazor.Models;

/// <summary>
/// Marca customizada num <c>OmniSlider</c> — um ponto na track com um
/// rótulo abaixo. Útil para indicar escalas significativas (0%, 25%, 50%,
/// "Baixo", "Médio", "Alto", etc.).
/// </summary>
public sealed class SliderMark
{
    public SliderMark(double value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>Posição numérica da marca (entre Min e Max do slider).</summary>
    public double Value { get; }

    /// <summary>Rótulo exibido embaixo da marca.</summary>
    public string Label { get; }
}
