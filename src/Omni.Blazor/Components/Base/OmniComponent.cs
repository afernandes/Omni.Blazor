using Microsoft.AspNetCore.Components;
using Omni.Blazor.State;

namespace Omni.Blazor.Components;

/// <summary>
/// Foundation base class for every Omni.Blazor component.
/// Provides extra CSS class, inline style, attribute splatting, an
/// ElementReference, and a <see cref="ParameterRegisterScope"/> for declarative
/// parameter change detection.
/// </summary>
public abstract class OmniComponent : ComponentBase
{
    private string? _id;

    /// <summary>Extra CSS classes appended to the component root.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style appended to the component root.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Extra HTML attributes splatted on the component root.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    /// <summary>Reference to the root DOM element.</summary>
    public ElementReference Element { get; protected set; }

    /// <summary>Auto-generated stable id, available for ARIA labels and JS targeting.</summary>
    public string Id => _id ??= "omni-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Scope for declarative parameter change detection. Use
    /// <c>RegisterParameter&lt;T&gt;</c> in your constructor (or
    /// <c>OnInitialized</c>) to track when consumers change a parameter.
    /// </summary>
    protected ParameterRegisterScope ParameterScope { get; } = new();

    /// <summary>Convenience: <c>ParameterScope.RegisterParameter&lt;T&gt;(...)</c>.</summary>
    protected ParameterStateBuilder<T> RegisterParameter<T>(string name)
        => ParameterScope.RegisterParameter<T>(name);

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        // Run detection AFTER base sets the parameters, so getters see the new
        // values. Handlers fire only on real changes (or first detect).
        await ParameterScope.DetectAllAsync();
    }
}

/// <summary>
/// Component base that accepts a <see cref="ChildContent"/> render fragment.
/// </summary>
public abstract class OmniComponentWithChildren : OmniComponent
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
