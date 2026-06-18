using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.ManifestGen;
using Omni.Blazor.Models;
using Xunit;

namespace Omni.Blazor.ManifestGen.Tests;

public class TypeNamesTests
{
    [Theory]
    [InlineData("OmniGrid`1", "OmniGrid")]
    [InlineData("Plain", "Plain")]
    [InlineData("Dict`2", "Dict")]
    public void StripArity(string input, string expected) => Assert.Equal(expected, TypeNames.StripArity(input));

    [Theory]
    [InlineData("String", "string")]
    [InlineData("Boolean", "bool")]
    [InlineData("Int32", "int")]
    [InlineData("Decimal", "decimal")]
    [InlineData("MouseEventArgs", "MouseEventArgs")] // non-primitive passes through
    public void Keyword(string input, string expected) => Assert.Equal(expected, TypeNames.Keyword(input));

    [Theory]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(int?), "int")]            // Nullable unwrapped
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(ButtonVariant), "ButtonVariant")]
    [InlineData(typeof(string[]), "string[]")]
    [InlineData(typeof(List<int>), "List<int>")]
    [InlineData(typeof(EventCallback<MouseEventArgs>), "EventCallback<MouseEventArgs>")]
    [InlineData(typeof(RenderFragment<string>), "RenderFragment<string>")]
    public void Friendly(Type t, string expected) => Assert.Equal(expected, TypeNames.Friendly(t));

    [Fact]
    public void Classify_event_slot_parameter()
    {
        Assert.Equal(("event", (string?)null), TypeNames.Classify(typeof(EventCallback)));
        Assert.Equal(("event", "MouseEventArgs"), TypeNames.Classify(typeof(EventCallback<MouseEventArgs>)));
        Assert.Equal(("slot", (string?)null), TypeNames.Classify(typeof(RenderFragment)));
        Assert.Equal(("slot", "string"), TypeNames.Classify(typeof(RenderFragment<string>)));
        Assert.Equal(("parameter", (string?)null), TypeNames.Classify(typeof(string)));
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(ButtonVariant), true)]   // enum
    [InlineData(typeof(object), false)]
    [InlineData(typeof(List<int>), false)]
    public void IsSimple(Type t, bool expected) => Assert.Equal(expected, TypeNames.IsSimple(t));

    [Fact]
    public void DefaultToString_handles_kinds()
    {
        Assert.Null(TypeNames.DefaultToString(null));
        Assert.Equal("true", TypeNames.DefaultToString(true));
        Assert.Equal("false", TypeNames.DefaultToString(false));
        Assert.Equal("hello", TypeNames.DefaultToString("hello"));
        Assert.Equal("5", TypeNames.DefaultToString(5));
        Assert.Equal("Primary", TypeNames.DefaultToString(ButtonVariant.Primary));
    }

    [Fact]
    public void XmlId_uses_full_name() => Assert.Equal("Omni.Blazor.Models.ButtonVariant", TypeNames.XmlId(typeof(ButtonVariant)));

    [Fact]
    public void IsFormInput_true_for_input_false_otherwise()
    {
        Assert.True(TypeNames.IsFormInput(typeof(OmniTextBox)));
        Assert.False(TypeNames.IsFormInput(typeof(OmniButton)));
    }

    [Fact]
    public void TryInstantiate_creates_concrete_and_skips_open_generic()
    {
        Assert.NotNull(TypeNames.TryInstantiate(typeof(OmniButton)));
        Assert.Null(TypeNames.TryInstantiate(typeof(List<>))); // open generic → null
    }

    [Fact]
    public void SafeGetTypes_returns_library_types()
    {
        var types = TypeNames.SafeGetTypes(typeof(OmniComponent).Assembly).ToList();
        Assert.Contains(typeof(OmniButton), types);
        Assert.True(types.Count > 100);
    }
}
