using System.Globalization;
using Omni.Blazor.Localization;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Tests.Utilities;

/// <summary>
/// Truncamento e rotulagem por trás do agrupamento hierárquico de datas do
/// <c>OmniDataGrid</c>. Cultura sempre explícita: o resultado depende dela, e um teste
/// que herda a cultura da máquina passa no dev e falha no CI.
/// </summary>
public class DateGroupingTests
{
    private static readonly CultureInfo PtBr = new("pt-BR");
    private static readonly CultureInfo EnUs = new("en-US");

    // 2026-07-31 22:44:16 — uma sexta-feira.
    private static readonly DateTime Instante = new(2026, 7, 31, 22, 44, 16);

    // --- Truncamento -------------------------------------------------------------

    [Fact]
    public void Year_collapses_to_january_first()
    {
        Assert.Equal(new DateTime(2026, 1, 1), DateGrouping.Truncate(Instante, DateGroupInterval.Year));
    }

    [Fact]
    public void Quarter_collapses_to_the_first_month_of_the_quarter()
    {
        // Julho abre o terceiro trimestre.
        Assert.Equal(new DateTime(2026, 7, 1), DateGrouping.Truncate(Instante, DateGroupInterval.Quarter));
        Assert.Equal(new DateTime(2026, 4, 1), DateGrouping.Truncate(new DateTime(2026, 6, 30), DateGroupInterval.Quarter));
    }

    [Fact]
    public void Month_day_and_hour_drop_everything_below_them()
    {
        Assert.Equal(new DateTime(2026, 7, 1), DateGrouping.Truncate(Instante, DateGroupInterval.Month));
        Assert.Equal(new DateTime(2026, 7, 31), DateGrouping.Truncate(Instante, DateGroupInterval.Day));
        Assert.Equal(new DateTime(2026, 7, 31, 22, 0, 0), DateGrouping.Truncate(Instante, DateGroupInterval.Hour));
    }

    [Fact]
    public void Week_starts_on_the_cultures_first_day()
    {
        // 31/07/2026 é sexta. Domingo em pt-BR (26/07); segunda em de-DE (27/07).
        Assert.Equal(new DateTime(2026, 7, 26), DateGrouping.Truncate(Instante, DateGroupInterval.Week, PtBr));
        Assert.Equal(new DateTime(2026, 7, 27), DateGrouping.Truncate(Instante, DateGroupInterval.Week, new CultureInfo("de-DE")));
    }

    [Fact]
    public void Two_instants_in_the_same_month_share_a_key()
    {
        // É a razão de existir do truncamento: sem ele, cada instante vira um grupo.
        var a = DateGrouping.Truncate(new DateTime(2026, 7, 1, 0, 0, 1), DateGroupInterval.Month);
        var b = DateGrouping.Truncate(new DateTime(2026, 7, 31, 23, 59, 59), DateGroupInterval.Month);

        Assert.Equal(a, b);
    }

    [Fact]
    public void DateTimeOffset_uses_the_time_as_written()
    {
        // A célula mostra o horário com o offset gravado; converter para UTC aqui jogaria
        // um evento das 22h de 31/07 (-03:00) para o dia 1º de agosto.
        var value = new DateTimeOffset(2026, 7, 31, 22, 44, 16, TimeSpan.FromHours(-3));

        Assert.Equal(new DateTime(2026, 7, 31), DateGrouping.Truncate(value, DateGroupInterval.Day));
    }

    [Fact]
    public void DateOnly_is_accepted()
    {
        Assert.Equal(new DateTime(2026, 7, 1), DateGrouping.Truncate(new DateOnly(2026, 7, 31), DateGroupInterval.Month));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026-07-31")]  // texto não é data: adivinhar o formato acerta numa cultura e erra na outra
    [InlineData(42)]
    public void A_value_that_is_not_a_date_has_no_key(object? value)
    {
        Assert.Null(DateGrouping.Truncate(value, DateGroupInterval.Day));
    }

    // --- Rótulos -----------------------------------------------------------------

    [Fact]
    public void The_year_label_is_the_year()
    {
        Assert.Equal("2026", DateGrouping.Label(new DateTime(2026, 1, 1), DateGroupInterval.Year, culture: PtBr));
    }

    [Fact]
    public void A_month_under_its_year_drops_the_year()
    {
        var key = new DateTime(2026, 7, 1);

        Assert.Equal("julho", DateGrouping.Label(key, DateGroupInterval.Month, withinLargerInterval: true, PtBr));
        Assert.Equal("julho 2026", DateGrouping.Label(key, DateGroupInterval.Month, withinLargerInterval: false, PtBr));
    }

    [Fact]
    public void A_day_under_its_month_is_just_the_day_number()
    {
        var key = new DateTime(2026, 7, 31);

        Assert.Equal("31", DateGrouping.Label(key, DateGroupInterval.Day, withinLargerInterval: true, PtBr));
        Assert.Equal("31/07/2026", DateGrouping.Label(key, DateGroupInterval.Day, withinLargerInterval: false, PtBr));
    }

    [Fact]
    public void Labels_follow_the_culture()
    {
        var key = new DateTime(2026, 7, 1);

        Assert.Equal("July", DateGrouping.Label(key, DateGroupInterval.Month, withinLargerInterval: true, EnUs));
    }

    [Fact]
    public void The_quarter_label_comes_from_the_texts()
    {
        var key = new DateTime(2026, 7, 1);

        Assert.Equal("T3", DateGrouping.Label(key, DateGroupInterval.Quarter, withinLargerInterval: true, PtBr));
        Assert.Equal("Q3", DateGrouping.Label(key, DateGroupInterval.Quarter, withinLargerInterval: true, EnUs, OmniTexts.English()));
    }

    [Fact]
    public void The_week_label_names_the_day_it_starts_on()
    {
        var key = new DateTime(2026, 7, 26);

        Assert.Equal("Semana de 26/07/2026", DateGrouping.Label(key, DateGroupInterval.Week, culture: PtBr));
    }

    [Fact]
    public void The_hour_label_is_the_whole_hour()
    {
        Assert.Equal("22:00", DateGrouping.Label(new DateTime(2026, 7, 31, 22, 0, 0), DateGroupInterval.Hour, culture: PtBr));
    }

    // --- Aninhamento --------------------------------------------------------------

    [Theory]
    [InlineData(DateGroupInterval.Month, DateGroupInterval.Year, true)]
    [InlineData(DateGroupInterval.Quarter, DateGroupInterval.Year, true)]
    [InlineData(DateGroupInterval.Month, DateGroupInterval.Quarter, true)]
    [InlineData(DateGroupInterval.Day, DateGroupInterval.Month, true)]
    [InlineData(DateGroupInterval.Hour, DateGroupInterval.Day, true)]
    // Uma semana cruza a virada de mês, então "semana de 30/06" sob "julho" seria mentira.
    [InlineData(DateGroupInterval.Week, DateGroupInterval.Month, false)]
    [InlineData(DateGroupInterval.Day, DateGroupInterval.Year, false)]
    public void Nesting_is_only_claimed_when_the_outer_interval_really_contains_the_inner(
        DateGroupInterval inner, DateGroupInterval outer, bool expected)
    {
        Assert.Equal(expected, DateGrouping.IsNestedIn(inner, outer));
    }

    [Fact]
    public void Interval_names_come_from_the_texts()
    {
        Assert.Equal("Mês", DateGrouping.IntervalName(DateGroupInterval.Month));
        Assert.Equal("Month", DateGrouping.IntervalName(DateGroupInterval.Month, OmniTexts.English()));
    }
}
