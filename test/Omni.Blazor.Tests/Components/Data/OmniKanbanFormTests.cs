using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniKanbanFormTests
{
    [Fact]
    public void Exposes_programmatic_create_and_edit_entry_points()
    {
        Type component = typeof(OmniKanbanForm<,>);

        Assert.NotNull(component.GetMethod(nameof(OmniKanbanForm<object, int>.BeginCreateAsync)));
        Assert.NotNull(component.GetMethod(nameof(OmniKanbanForm<object, int>.BeginEditAsync)));
    }
}
