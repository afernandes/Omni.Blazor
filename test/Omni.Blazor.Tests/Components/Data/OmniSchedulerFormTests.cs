using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniSchedulerFormTests
{
    [Fact]
    public void Exposes_programmatic_create_and_edit_entry_points()
    {
        Type component = typeof(OmniSchedulerForm<,>);

        Assert.NotNull(component.GetMethod(nameof(OmniSchedulerForm<object, int>.BeginCreateAsync)));
        Assert.NotNull(component.GetMethod(nameof(OmniSchedulerForm<object, int>.BeginEditAsync)));
    }
}
