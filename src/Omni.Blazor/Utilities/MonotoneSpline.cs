using System.Globalization;
using System.Text;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Gera SVG path com curvas monotônicas cúbicas Bezier passando exatamente
/// por cada ponto de entrada. Algoritmo "monotone cubic Hermite" — mesmo que
/// d3.js usa em <c>curveMonotoneX</c> e que o Radzen usa no SplineGenerator.
///
/// Por que monotônico: spline cúbico tradicional pode oscilar (overshoot) em
/// dados não-monotônicos, criando "vales" e "picos" artificiais. O monotone
/// preserva monotonicidade do dataset original — a curva não vai abaixo de um
/// mínimo local nem acima de um máximo local. Crítico pra dados de séries
/// temporais onde valores intermediários artificiais seriam enganosos.
///
/// Uso: pass pares (x, y); recebe string pronta pra SVG <c>&lt;path d="..."&gt;</c>.
/// </summary>
public static class MonotoneSpline
{
    /// <summary>
    /// Gera path SVG passando suavemente por todos os pontos. Retorna string
    /// vazia se menos de 2 pontos.
    /// </summary>
    public static string Path((double X, double Y)[] points)
    {
        if (points is null || points.Length == 0) return string.Empty;
        if (points.Length == 1)
        {
            // Único ponto: só o move-to (não dá pra desenhar curva sem 2º ponto).
            return $"M {F(points[0].X)} {F(points[0].Y)}";
        }

        var n = points.Length;
        // dx[i] = points[i+1].x - points[i].x; dy[i] = same pra y
        // slope[i] = dy[i] / dx[i] (slope do segmento entre i e i+1)
        var slopes = new double[n - 1];
        var dxs = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            var dx = points[i + 1].X - points[i].X;
            var dy = points[i + 1].Y - points[i].Y;
            dxs[i] = dx;
            slopes[i] = dx != 0 ? dy / dx : 0;
        }

        // tangents[i] = tangente em points[i]
        // Endpoints: usa o slope do segmento adjacente
        // Interior: média dos slopes vizinhos, ZERO se sinais opostos (preserva monotonicidade)
        var tangents = new double[n];
        tangents[0] = slopes[0];
        tangents[n - 1] = slopes[n - 2];
        for (int i = 1; i < n - 1; i++)
        {
            var prev = slopes[i - 1];
            var next = slopes[i];
            if (Math.Sign(prev) != Math.Sign(next) || prev == 0 || next == 0)
            {
                tangents[i] = 0; // mudança de direção: zera tangente pra evitar overshoot
            }
            else
            {
                tangents[i] = (prev + next) / 2;
            }
        }

        // Ajuste de Fritsch-Carlson: limita tangentes pra garantir monotonicidade
        for (int i = 0; i < n - 1; i++)
        {
            if (slopes[i] == 0) { tangents[i] = 0; tangents[i + 1] = 0; continue; }
            var a = tangents[i] / slopes[i];
            var b = tangents[i + 1] / slopes[i];
            var sq = a * a + b * b;
            if (sq <= 9) continue;
            var tau = 3 / Math.Sqrt(sq);
            tangents[i] = a * tau * slopes[i];
            tangents[i + 1] = b * tau * slopes[i];
        }

        var sb = new StringBuilder(n * 32);
        sb.Append('M').Append(' ').Append(F(points[0].X)).Append(' ').Append(F(points[0].Y));
        for (int i = 0; i < n - 1; i++)
        {
            // Cubic Bezier de points[i] a points[i+1] com control points definidos pela tangente
            var dx = dxs[i] / 3;
            var c1x = points[i].X + dx;
            var c1y = points[i].Y + dx * tangents[i];
            var c2x = points[i + 1].X - dx;
            var c2y = points[i + 1].Y - dx * tangents[i + 1];
            sb.Append(" C ")
              .Append(F(c1x)).Append(' ').Append(F(c1y)).Append(", ")
              .Append(F(c2x)).Append(' ').Append(F(c2y)).Append(", ")
              .Append(F(points[i + 1].X)).Append(' ').Append(F(points[i + 1].Y));
        }
        return sb.ToString();
    }

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
