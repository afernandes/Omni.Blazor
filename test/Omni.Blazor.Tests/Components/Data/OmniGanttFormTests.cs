using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniGanttFormTests
{
    [Fact]
    public void Exposes_programmatic_create_and_edit_entry_points()
    {
        Type component = typeof(OmniGanttForm<,>);

        Assert.NotNull(component.GetMethod(nameof(OmniGanttForm<object, int>.BeginCreateAsync)));
        Assert.NotNull(component.GetMethod(nameof(OmniGanttForm<object, int>.BeginEditAsync)));
    }
}
