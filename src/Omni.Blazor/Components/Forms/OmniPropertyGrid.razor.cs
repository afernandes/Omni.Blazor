using System.Diagnostics.CodeAnalysis;

namespace Omni.Blazor.Components;

public partial class OmniPropertyGrid<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel>
    where TModel : class
{
}
