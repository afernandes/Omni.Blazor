using System;
using System.Collections.Generic;
using System.Linq;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Engine contract for <see cref="FilterSqlConverter"/>: rule tree → SQL,
/// SQL → rule tree, coercion, errors, and round-trip stability.
/// </summary>
public class FilterSqlConverterTests
{
    private static IReadOnlyList<OmniFilterPropertyInfo> Props() => new[]
    {
        new OmniFilterPropertyInfo { Property = "Nome",     Title = "Nome",     Type = ColumnFilterType.Text },
        new OmniFilterPropertyInfo { Property = "Idade",    Title = "Idade",    Type = ColumnFilterType.Number },
        new OmniFilterPropertyInfo { Property = "Cadastro", Title = "Cadastro", Type = ColumnFilterType.Date },
        new OmniFilterPropertyInfo { Property = "Ativo",    Title = "Ativo",    Type = ColumnFilterType.Boolean },
        new OmniFilterPropertyInfo { Property = "Status",   Title = "Status",   Type = ColumnFilterType.Select },
    };

    private static OmniFilterRule Cond(string prop, FilterOperator op, object? val) =>
        new() { Property = prop, Operator = op, Value = val };

    private static List<OmniFilterRule> R(params OmniFilterRule[] r) => r.ToList();

    // ─── ToSql ───────────────────────────────────────────────────────────────

    [Fact]
    public void ToSql_contains_emits_like()
        => Assert.Equal("Nome LIKE '%silva%'", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.Contains, "silva")), FilterLogic.And, Props()));

    [Fact]
    public void ToSql_startswith_endswith()
    {
        Assert.Equal("Nome LIKE 'a%'", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.StartsWith, "a")), FilterLogic.And, Props()));
        Assert.Equal("Nome LIKE '%a'", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.EndsWith, "a")), FilterLogic.And, Props()));
    }

    [Fact]
    public void ToSql_number_is_unquoted_string_is_quoted()
    {
        Assert.Equal("Idade >= 18", FilterSqlConverter.ToSql(R(Cond("Idade", FilterOperator.GreaterOrEqual, 18m)), FilterLogic.And, Props()));
        Assert.Equal("Nome = 'João'", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.Equals, "João")), FilterLogic.And, Props()));
    }

    [Fact]
    public void ToSql_bool_and_date()
    {
        Assert.Equal("Ativo = TRUE", FilterSqlConverter.ToSql(R(Cond("Ativo", FilterOperator.Equals, true)), FilterLogic.And, Props()));
        Assert.Equal("Cadastro > '2020-01-01'", FilterSqlConverter.ToSql(R(Cond("Cadastro", FilterOperator.GreaterThan, new DateTime(2020, 1, 1))), FilterLogic.And, Props()));
    }

    [Fact]
    public void ToSql_is_empty_maps_to_is_null()
    {
        Assert.Equal("Nome IS NULL", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.IsEmpty, null)), FilterLogic.And, Props()));
        Assert.Equal("Nome IS NOT NULL", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.IsNotEmpty, null)), FilterLogic.And, Props()));
    }

    [Fact]
    public void ToSql_escapes_single_quotes()
        => Assert.Equal("Nome = 'd''Or'", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.Equals, "d'Or")), FilterLogic.And, Props()));

    [Fact]
    public void ToSql_nested_groups_get_parens_and_logic()
    {
        var group = new OmniFilterRule { Logic = FilterLogic.Or, Rules = R(Cond("Idade", FilterOperator.GreaterThan, 5m), Cond("Idade", FilterOperator.LessThan, 1m)) };
        var sql = FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.Contains, "a"), group), FilterLogic.And, Props());
        Assert.Equal("Nome LIKE '%a%' AND (Idade > 5 OR Idade < 1)", sql);
    }

    [Fact]
    public void ToSql_skips_incomplete_conditions_and_empty()
    {
        Assert.Equal("", FilterSqlConverter.ToSql(R(Cond("Nome", FilterOperator.Equals, null)), FilterLogic.And, Props()));
        Assert.Equal("", FilterSqlConverter.ToSql(R(), FilterLogic.And, Props()));
    }

    // ─── TryParse ──────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_comparison_coerces_number()
    {
        Assert.True(FilterSqlConverter.TryParse("Idade >= 18", Props(), out var rules, out var logic, out var err));
        Assert.Null(err);
        Assert.Equal(FilterLogic.And, logic);
        var r = Assert.Single(rules);
        Assert.Equal("Idade", r.Property);
        Assert.Equal(FilterOperator.GreaterOrEqual, r.Operator);
        Assert.Equal(18m, r.Value);
    }

    [Theory]
    [InlineData("Nome LIKE '%x%'", FilterOperator.Contains)]
    [InlineData("Nome LIKE 'x%'", FilterOperator.StartsWith)]
    [InlineData("Nome LIKE '%x'", FilterOperator.EndsWith)]
    [InlineData("Nome NOT LIKE '%x%'", FilterOperator.NotContains)]
    public void Parse_like_infers_operator(string sql, FilterOperator expected)
    {
        Assert.True(FilterSqlConverter.TryParse(sql, Props(), out var rules, out _, out _));
        Assert.Equal(expected, Assert.Single(rules).Operator);
        Assert.Equal("x", rules[0].Value);
    }

    [Fact]
    public void Parse_is_null_maps_to_is_empty()
    {
        Assert.True(FilterSqlConverter.TryParse("Nome IS NULL", Props(), out var r1, out _, out _));
        Assert.Equal(FilterOperator.IsEmpty, r1[0].Operator);
        Assert.True(FilterSqlConverter.TryParse("Nome IS NOT NULL", Props(), out var r2, out _, out _));
        Assert.Equal(FilterOperator.IsNotEmpty, r2[0].Operator);
    }

    [Fact]
    public void Parse_bool_and_date_coercion()
    {
        Assert.True(FilterSqlConverter.TryParse("Ativo = TRUE", Props(), out var rb, out _, out _));
        Assert.Equal(true, rb[0].Value);
        Assert.True(FilterSqlConverter.TryParse("Cadastro > '2020-01-01'", Props(), out var rd, out _, out _));
        Assert.Equal(new DateTime(2020, 1, 1), rd[0].Value);
    }

    [Fact]
    public void Parse_precedence_and_binds_tighter_than_or()
    {
        Assert.True(FilterSqlConverter.TryParse("Idade > 5 AND Idade < 10 OR Nome LIKE '%x%'", Props(), out var rules, out var logic, out _));
        Assert.Equal(FilterLogic.Or, logic);
        Assert.Equal(2, rules.Count);
        Assert.True(rules[0].IsGroup);
        Assert.Equal(FilterLogic.And, rules[0].Logic);
        Assert.Equal(2, rules[0].Rules!.Count);
        Assert.False(rules[1].IsGroup);
    }

    [Fact]
    public void Parse_parentheses_group()
    {
        Assert.True(FilterSqlConverter.TryParse("(Idade > 5 OR Idade < 1) AND Nome = 'a'", Props(), out var rules, out var logic, out _));
        Assert.Equal(FilterLogic.And, logic);
        Assert.True(rules[0].IsGroup);
        Assert.Equal(FilterLogic.Or, rules[0].Logic);
    }

    [Fact]
    public void Parse_empty_is_valid_and_clears()
    {
        Assert.True(FilterSqlConverter.TryParse("   ", Props(), out var rules, out _, out var err));
        Assert.Empty(rules);
        Assert.Null(err);
    }

    [Theory]
    [InlineData("Foo = 1", "desconhecida")]
    [InlineData("Idade = 'abc'", "numérico")]
    [InlineData("Nome = 'abc", "não terminada")]
    [InlineData("Idade 5", "operador")]
    [InlineData("Nome BETWEEN 1 AND 2", "BETWEEN")]
    public void Parse_errors_are_reported(string sql, string fragment)
    {
        Assert.False(FilterSqlConverter.TryParse(sql, Props(), out var rules, out _, out var err));
        Assert.Empty(rules);
        Assert.NotNull(err);
        Assert.Contains(fragment, err);
    }

    // ─── Round-trip ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Nome LIKE '%silva%' AND Idade >= 18")]
    [InlineData("(Nome LIKE '%a%' AND Idade > 5) OR Status = 'Ativo'")]
    [InlineData("Cadastro <= '2024-12-31' AND Ativo = TRUE")]
    [InlineData("Nome IS NULL OR Nome = 'x'")]
    public void Round_trip_is_stable(string sql)
    {
        Assert.True(FilterSqlConverter.TryParse(sql, Props(), out var rules, out var logic, out var err), err);
        var rendered = FilterSqlConverter.ToSql(rules, logic, Props());
        Assert.Equal(sql, rendered);
    }

    // ─── Hardening (from adversarial review) ─────────────────────────────────────

    [Fact]
    public void ToSql_number_string_value_is_canonicalized()
    {
        Assert.Equal("Idade = 5", FilterSqlConverter.ToSql(R(Cond("Idade", FilterOperator.Equals, (object)"5")), FilterLogic.And, Props()));
        Assert.Equal("Idade = -5", FilterSqlConverter.ToSql(R(Cond("Idade", FilterOperator.Equals, (object)"-5")), FilterLogic.And, Props()));
    }

    [Fact]
    public void ToSql_boolean_string_one_zero_become_literals()
    {
        Assert.Equal("Ativo = TRUE", FilterSqlConverter.ToSql(R(Cond("Ativo", FilterOperator.Equals, (object)"1")), FilterLogic.And, Props()));
        Assert.Equal("Ativo = FALSE", FilterSqlConverter.ToSql(R(Cond("Ativo", FilterOperator.Equals, (object)"0")), FilterLogic.And, Props()));
    }

    [Fact]
    public void ToSql_non_finite_double_does_not_emit_bare_identifier()
    {
        var sql = FilterSqlConverter.ToSql(R(Cond("Idade", FilterOperator.GreaterThan, double.NaN)), FilterLogic.And, Props());
        Assert.DoesNotContain("NaN", sql);
        Assert.True(FilterSqlConverter.TryParse(sql, Props(), out _, out _, out _)); // emitted SQL stays parseable
    }

    [Theory]
    [InlineData(1e21)]
    [InlineData(1e-7)]
    [InlineData(9.5)]
    public void ToSql_finite_double_emits_parseable_non_scientific(double val)
    {
        var sql = FilterSqlConverter.ToSql(R(Cond("Idade", FilterOperator.GreaterThan, val)), FilterLogic.And, Props());
        Assert.DoesNotContain("E+", sql);
        Assert.DoesNotContain("E-", sql);
        Assert.True(FilterSqlConverter.TryParse(sql, Props(), out _, out _, out var err), err);  // round-trips
    }

    [Fact]
    public void Round_trip_bracketed_identifier_with_bracket_char()
    {
        var props = new[] { new OmniFilterPropertyInfo { Property = "a]b", Title = "x", Type = ColumnFilterType.Text } };
        var sql = FilterSqlConverter.ToSql(R(new OmniFilterRule { Property = "a]b", Operator = FilterOperator.Equals, Value = "x" }), FilterLogic.And, props);
        Assert.Equal("[a]]b] = 'x'", sql);
        Assert.True(FilterSqlConverter.TryParse(sql, props, out var rules, out _, out _));
        Assert.Equal("a]b", Assert.Single(rules).Property);
    }

    [Theory]
    [InlineData("Cadastro = '12/13/2024'")]   // US slashes — rejected (only ISO accepted)
    [InlineData("Cadastro = '13/12/2024'")]
    public void Parse_date_accepts_only_iso(string sql)
        => Assert.False(FilterSqlConverter.TryParse(sql, Props(), out _, out _, out _));

    [Theory]
    [InlineData("Idade = '(5)'")]
    [InlineData("Idade = '1,000'")]
    [InlineData("Idade = '1e2'")]
    public void Parse_number_rejects_non_canonical_literals(string sql)
        => Assert.False(FilterSqlConverter.TryParse(sql, Props(), out _, out _, out _));

    [Fact]
    public void Parse_boolean_accepts_only_zero_one_not_arbitrary_numeric()
    {
        Assert.True(FilterSqlConverter.TryParse("Ativo = 1", Props(), out var r1, out _, out _));
        Assert.Equal(true, r1[0].Value);
        Assert.True(FilterSqlConverter.TryParse("Ativo = 0", Props(), out var r0, out _, out _));
        Assert.Equal(false, r0[0].Value);
        Assert.False(FilterSqlConverter.TryParse("Ativo = 2", Props(), out _, out _, out _));
    }

    [Fact]
    public void Parse_prefix_not_gives_a_clear_error()
    {
        Assert.False(FilterSqlConverter.TryParse("NOT Nome LIKE '%x%'", Props(), out _, out _, out var err));
        Assert.Contains("NOT", err);
    }
}
