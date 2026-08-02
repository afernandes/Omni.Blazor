using System.Globalization;
using Omni.Blazor.Localization;
using Omni.Blazor.Models;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Trunca e rotula datas para o agrupamento hierárquico do <c>OmniDataGrid</c>.
///
/// <para>Separado do componente porque é lógica pura de calendário: é aqui que se decide
/// o que "mesmo mês" significa e como um grupo se chama. O grid só chama
/// <see cref="Truncate"/> para montar a chave e <see cref="Label"/> para exibi-la.</para>
/// </summary>
public static class DateGrouping
{
    /// <summary>
    /// Chave do grupo: a data truncada até <paramref name="interval"/>. Devolve
    /// <c>null</c> quando o valor não é uma data — o grid trata esses itens como um grupo
    /// vazio, em vez de fingir uma data que o dado não tem.
    ///
    /// <para>O truncamento preserva o instante como <see cref="DateTime"/> para que os
    /// grupos continuem comparáveis entre si (ordenação cronológica, e não alfabética de
    /// "abril" antes de "janeiro").</para>
    /// </summary>
    public static DateTime? Truncate(object? value, DateGroupInterval interval, CultureInfo? culture = null)
    {
        if (!TryGetDate(value, out var date)) return null;

        return interval switch
        {
            DateGroupInterval.Year => new DateTime(date.Year, 1, 1, 0, 0, 0, date.Kind),
            DateGroupInterval.Quarter => new DateTime(date.Year, ((date.Month - 1) / 3 * 3) + 1, 1, 0, 0, 0, date.Kind),
            DateGroupInterval.Month => new DateTime(date.Year, date.Month, 1, 0, 0, 0, date.Kind),
            DateGroupInterval.Week => StartOfWeek(date, culture ?? CultureInfo.CurrentCulture),
            DateGroupInterval.Day => date.Date,
            DateGroupInterval.Hour => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, date.Kind),
            _ => date,
        };
    }

    /// <summary>
    /// Rótulo do grupo.
    ///
    /// <para><paramref name="withinLargerInterval"/> diz se um nível acima já mostra a
    /// unidade maior: sob "2026", o mês se chama "julho", e não "julho de 2026" — repetir o
    /// ano em cada linha da hierarquia é ruído. É o que o Excel faz numa tabela dinâmica.</para>
    /// </summary>
    public static string Label(
        DateTime key,
        DateGroupInterval interval,
        bool withinLargerInterval = false,
        CultureInfo? culture = null,
        OmniTexts? texts = null)
    {
        var c = culture ?? CultureInfo.CurrentCulture;
        var t = texts ?? OmniTexts.Default;

        return interval switch
        {
            DateGroupInterval.Year => key.ToString("yyyy", c),

            DateGroupInterval.Quarter => Quarter(key, c, t, withinLargerInterval),

            DateGroupInterval.Month => withinLargerInterval
                ? c.DateTimeFormat.GetMonthName(key.Month)
                : $"{c.DateTimeFormat.GetMonthName(key.Month)} {key:yyyy}",

            // A semana é identificada pelo dia em que começa: "semana de 27/07" diz mais do
            // que um número ISO, que quase ninguém sabe traduzir em datas de cabeça.
            DateGroupInterval.Week => string.Format(c, t.WeekOf, key.ToString("d", c)),

            // Sob um mês, o dia sozinho basta; solto, precisa da data inteira.
            DateGroupInterval.Day => withinLargerInterval
                ? key.ToString("dd", c)
                : key.ToString("d", c),

            DateGroupInterval.Hour => key.ToString("HH:00", c),

            _ => key.ToString(c),
        };
    }

    /// <summary>
    /// Diz se <paramref name="inner"/> está contido em <paramref name="outer"/> na mesma
    /// hierarquia — usado para decidir o rótulo curto. Semana não entra: uma semana cruza
    /// a virada de mês, então "semana de 30/06" sob "julho" seria mentira.
    /// </summary>
    public static bool IsNestedIn(DateGroupInterval inner, DateGroupInterval outer) =>
        (outer, inner) switch
        {
            (DateGroupInterval.Year, DateGroupInterval.Quarter) => true,
            (DateGroupInterval.Year, DateGroupInterval.Month) => true,
            (DateGroupInterval.Quarter, DateGroupInterval.Month) => true,
            (DateGroupInterval.Month, DateGroupInterval.Day) => true,
            (DateGroupInterval.Week, DateGroupInterval.Day) => true,
            (DateGroupInterval.Day, DateGroupInterval.Hour) => true,
            _ => false,
        };

    /// <summary>Nome da unidade, para compor o rótulo do chip de agrupamento.</summary>
    public static string IntervalName(DateGroupInterval interval, OmniTexts? texts = null)
    {
        var t = texts ?? OmniTexts.Default;
        return interval switch
        {
            DateGroupInterval.Year => t.Year,
            DateGroupInterval.Quarter => t.Quarter,
            DateGroupInterval.Month => t.Month,
            DateGroupInterval.Week => t.Week,
            DateGroupInterval.Day => t.Day,
            DateGroupInterval.Hour => t.Hour,
            _ => interval.ToString(),
        };
    }

    /// <summary>
    /// Aceita os tipos de data que uma coluna pode devolver. <c>string</c> fica de fora de
    /// propósito: adivinhar o formato de um texto acerta em pt-BR e erra em en-US no mesmo
    /// dataset — quem guarda data como texto declara um <c>Property</c> que a converte.
    /// </summary>
    private static bool TryGetDate(object? value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt:
                date = dt;
                return true;
            case DateTimeOffset dto:
                // O horário como foi gravado, sem converter fuso: é o que a célula mostra.
                date = dto.DateTime;
                return true;
            case DateOnly d:
                date = d.ToDateTime(TimeOnly.MinValue);
                return true;
            default:
                date = default;
                return false;
        }
    }

    // O primeiro dia da semana é cultural: domingo em pt-BR e en-US, segunda em de-DE.
    private static DateTime StartOfWeek(DateTime date, CultureInfo culture)
    {
        var first = culture.DateTimeFormat.FirstDayOfWeek;
        var diff = (7 + (int)date.DayOfWeek - (int)first) % 7;
        return date.Date.AddDays(-diff);
    }

    private static string Quarter(DateTime key, CultureInfo c, OmniTexts t, bool withinYear)
    {
        var quarter = ((key.Month - 1) / 3) + 1;
        var rotulo = string.Format(c, t.QuarterAbbreviation, quarter);
        return withinYear ? rotulo : $"{rotulo} {key:yyyy}";
    }
}
