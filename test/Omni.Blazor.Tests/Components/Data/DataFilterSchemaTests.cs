using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public class DataFilterSchemaTests : TestContextBase
{
    public sealed record Person(string Name, int Age, bool Active);
    public sealed record ExternalCode(string Value);
    public sealed record CodedItem(ExternalCode Code);
    public sealed record Address(string City);
    public sealed record Customer(string Name, Address? Address);

    private static readonly Person[] People =
    {
        new("Ana", 28, true),
        new("Bruno", 41, false),
        new("Carla", 35, true),
        new("Diego", 23, true)
    };

    [Fact]
    public void Schema_infers_types_and_rejects_duplicate_stable_ids()
    {
        DataFilterSchema<Person> schema = CreateSchema();

        Assert.Equal(3, schema.Fields.Count);
        Assert.Equal(ColumnFilterType.Text, schema.Fields[0].Type);
        Assert.Equal(ColumnFilterType.Number, schema.Fields[1].Type);
        Assert.Equal(ColumnFilterType.Boolean, schema.Fields[2].Type);
        Assert.Equal("name", schema.Fields[0].Id);

        DataFilterSchemaBuilder<Person> duplicate = DataFilterSchema<Person>.Builder()
            .Field(person => person.Name, field => field.Id("same"))
            .Field(person => person.Age, field => field.Id("same"));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(duplicate.Build);
        Assert.Contains("same", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Typed_nested_query_filters_locally_without_reflection()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        DataFilterQuery<Person> query = schema.Query(root => root
            .Condition(person => person.Active, FilterOperator.Equals, true)
            .Group(FilterLogic.Or, group => group
                .Condition(person => person.Name, FilterOperator.Contains, "a")
                .Condition(person => person.Age, FilterOperator.GreaterThan, 30)));

        Assert.Equal(
            new[] { "Ana", "Carla" },
            query.Apply(People, schema).Select(person => person.Name).OrderBy(static name => name));
    }

    [Fact]
    public void Query_round_trips_with_stable_ids_and_primitive_value_kinds()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        DataFilterQuery<Person> original = schema.Query(root => root
            .Condition(person => person.Age, FilterOperator.GreaterThan, 30));

        string json = original.Serialize();
        DataFilterQuery<Person> restored = DataFilterQuery<Person>.Deserialize(json, schema);

        Assert.Contains("\"version\":1", json, StringComparison.Ordinal);
        Assert.Contains("\"field\":\"age\"", json, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"SignedInteger\"", json, StringComparison.Ordinal);
        Assert.Equal(
            new[] { "Bruno", "Carla" },
            restored.Apply(People, schema).Select(person => person.Name).OrderBy(static name => name));
    }

    [Fact]
    public void Deserialization_fails_closed_for_unknown_fields_and_disallowed_operators()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        string json = schema.Query(root => root
                .Condition(person => person.Age, FilterOperator.GreaterThan, 30))
            .Serialize();

        Assert.False(DataFilterQuery<Person>.TryDeserialize(
            json.Replace("\"age\"", "\"missing\"", StringComparison.Ordinal),
            schema,
            out _,
            out string? unknownError));
        Assert.Contains("missing", unknownError, StringComparison.Ordinal);

        Assert.False(DataFilterQuery<Person>.TryDeserialize(
            json.Replace("\"GreaterThan\"", "\"Contains\"", StringComparison.Ordinal),
            schema,
            out _,
            out string? operatorError));
        Assert.Contains("Contains", operatorError, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\"version\":1,\"root\":{\"logic\":\"And\",\"rules\":[null]}}")]
    [InlineData("{\"version\":1,\"root\":{\"logic\":\"And\",\"rules\":[{\"field\":\"age\",\"operator\":\"Equals\",\"value\":{\"kind\":\"SignedInteger\",\"text\":null},\"upperValue\":null,\"group\":null}]}}")]
    public void Deserialization_fails_closed_for_malformed_nodes(string json)
    {
        Assert.False(DataFilterQuery<Person>.TryDeserialize(
            json,
            CreateSchema(),
            out DataFilterQuery<Person>? query,
            out string? error));
        Assert.Null(query);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void IQueryable_expression_preserves_nested_boolean_structure()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        DataFilterQuery<Person> query = schema.Query(root => root
            .Condition(person => person.Age, FilterOperator.GreaterOrEqual, 28)
            .Condition(person => person.Name, FilterOperator.NotContains, "z"));

        Person[] result = query.Apply(People.AsQueryable(), schema).ToArray();

        Assert.Equal(new[] { "Ana", "Bruno", "Carla" }, result.Select(person => person.Name));
    }

    [Fact]
    public void Between_has_typed_lower_and_upper_values()
    {
        DataFilterSchema<Person> schema = CreateSchema(includeRanges: true);
        DataFilterQuery<Person> query = schema.Query(root => root
            .Between(person => person.Age, 28, 35));

        string json = query.Serialize();
        DataFilterQuery<Person> restored = DataFilterQuery<Person>.Deserialize(json, schema);

        Assert.Contains("upperValue", json, StringComparison.Ordinal);
        Assert.Equal(
            new[] { "Ana", "Carla" },
            restored.Apply(People, schema).Select(person => person.Name));
        Assert.Equal(2, restored.Apply(People.AsQueryable(), schema).Count());
    }

    [Fact]
    public void Custom_value_codec_round_trips_without_reflection_serialization()
    {
        DataFilterSchema<CodedItem> schema = DataFilterSchema<CodedItem>.Create(builder => builder
            .Field(item => item.Code, field => field
                .Id("code")
                .Operators(FilterOperator.Equals)
                .ValueCodec(code => code.Value, text => new ExternalCode(text))));
        DataFilterQuery<CodedItem> query = schema.Query(root => root
            .Condition(item => item.Code, FilterOperator.Equals, new ExternalCode("A-1")));

        string json = query.Serialize();
        DataFilterQuery<CodedItem> restored = DataFilterQuery<CodedItem>.Deserialize(json, schema);

        Assert.Contains("\"kind\":\"Custom\"", json, StringComparison.Ordinal);
        Assert.True(restored.Compile(schema)(new CodedItem(new ExternalCode("A-1"))));
        Assert.False(restored.Compile(schema)(new CodedItem(new ExternalCode("B-2"))));
    }

    [Fact]
    public void Compiled_local_accessors_treat_null_intermediate_members_as_null_values()
    {
        DataFilterSchema<Customer> schema = DataFilterSchema<Customer>.Create(builder => builder
            .Field(customer => customer.Address!.City));
        DataFilterQuery<Customer> query = schema.Query(root => root
            .Condition(customer => customer.Address!.City, FilterOperator.Equals, "São Paulo"));
        Customer[] customers =
        {
            new("Sem endereço", null),
            new("Com endereço", new Address("São Paulo"))
        };

        Assert.Equal("Com endereço", Assert.Single(query.Apply(customers, schema)).Name);
    }

    [Fact]
    public void Schema_limits_bound_programmatic_and_deserialized_trees()
    {
        DataFilterSchema<Person> schema = DataFilterSchema<Person>.Create(builder => builder
            .Limits(maximumDepth: 2, maximumRules: 2)
            .Field(person => person.Name));

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => schema.Query(root => root
            .Condition(person => person.Name, FilterOperator.Contains, "a")
            .Condition(person => person.Name, FilterOperator.Contains, "b")
            .Condition(person => person.Name, FilterOperator.Contains, "c")));

        Assert.Contains("maximum rule count", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Component_uses_schema_query_and_publishes_immutable_snapshots()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        DataFilterQuery<Person> initial = schema.Query(root => root
            .Condition(person => person.Name, FilterOperator.Contains, "a"));
        DataFilterQuery<Person>? changed = null;

        IRenderedComponent<OmniDataFilter<Person>> cut = Render<OmniDataFilter<Person>>(parameters => parameters
            .Add(component => component.Data, People)
            .Add(component => component.Schema, schema)
            .Add(component => component.Query, initial)
            .Add(component => component.QueryChanged,
                EventCallback.Factory.Create<DataFilterQuery<Person>>(this, query => changed = query)));

        Assert.Single(cut.FindAll(".omni-datafilter-condition"));
        cut.Find(".omni-datafilter-input").Input("bru");

        Assert.NotNull(changed);
        Assert.Equal(new[] { "Bruno" }, changed!.Apply(People, schema).Select(person => person.Name));
        Assert.NotSame(initial, changed);
    }

    [Fact]
    public void Incomplete_visual_condition_is_not_persisted_as_an_invalid_query()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        DataFilterQuery<Person>? changed = null;
        IRenderedComponent<OmniDataFilter<Person>> cut = Render<OmniDataFilter<Person>>(parameters => parameters
            .Add(component => component.Data, People)
            .Add(component => component.Schema, schema)
            .Add(component => component.QueryChanged,
                EventCallback.Factory.Create<DataFilterQuery<Person>>(this, query => changed = query)));

        cut.Find(".omni-datafilter-add").Click();

        Assert.NotNull(changed);
        Assert.Empty(changed!.Root.Rules);
    }

    [Fact]
    public void Component_edits_between_as_two_typed_values()
    {
        DataFilterSchema<Person> schema = CreateSchema(includeRanges: true);
        DataFilterQuery<Person> initial = schema.Query(root => root
            .Between(person => person.Age, 28, 41));
        DataFilterQuery<Person>? changed = null;
        IRenderedComponent<OmniDataFilter<Person>> cut = Render<OmniDataFilter<Person>>(parameters => parameters
            .Add(component => component.Data, People)
            .Add(component => component.Schema, schema)
            .Add(component => component.Query, initial)
            .Add(component => component.QueryChanged,
                EventCallback.Factory.Create<DataFilterQuery<Person>>(this, query => changed = query)));

        Assert.Single(cut.FindAll(".omni-datafilter-range"));
        Assert.Equal(2, cut.FindAll(".omni-datafilter-range .omni-numeric-input").Count);
        cut.FindAll(".omni-datafilter-range .omni-numeric-input")[0].Change("35");

        Assert.NotNull(changed);
        Assert.Equal(
            new[] { "Bruno", "Carla" },
            changed!.Apply(People, schema).Select(person => person.Name).OrderBy(static name => name));
    }

    [Fact]
    public void Controlled_query_replacement_recomputes_the_view()
    {
        DataFilterSchema<Person> schema = CreateSchema();
        DataFilterQuery<Person> initial = schema.Query(root => root
            .Condition(person => person.Name, FilterOperator.Equals, "Ana"));
        DataFilterQuery<Person> replacement = schema.Query(root => root
            .Condition(person => person.Age, FilterOperator.GreaterThan, 40));
        IRenderedComponent<OmniDataFilter<Person>> cut = Render<OmniDataFilter<Person>>(parameters => parameters
            .Add(component => component.Data, People)
            .Add(component => component.Schema, schema)
            .Add(component => component.Query, initial));

        cut.Render(parameters => parameters
            .Add(component => component.Data, People)
            .Add(component => component.Schema, schema)
            .Add(component => component.Query, replacement));

        cut.WaitForAssertion(() =>
            Assert.Equal("Bruno", Assert.Single(cut.Instance.View).Name));
    }

    private static DataFilterSchema<Person> CreateSchema(bool includeRanges = false)
        => DataFilterSchema<Person>.Create(builder =>
        {
            builder.Field(person => person.Name, field => field.Id("name").Title("Nome"));
            builder.Field(person => person.Age, field =>
            {
                field.Id("age").Title("Idade");
                if (includeRanges)
                {
                    field.Operators(
                        FilterOperator.Equals,
                        FilterOperator.GreaterThan,
                        FilterOperator.GreaterOrEqual,
                        FilterOperator.LessThan,
                        FilterOperator.LessOrEqual,
                        FilterOperator.Between,
                        FilterOperator.NotBetween);
                }
            });
            builder.Field(person => person.Active, field => field.Id("active").Title("Ativo"));
        });
}
