using System.Globalization;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Localization;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;
using Omni.Localization;

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

    /// <summary>
    /// Stable DOM id used by ARIA relationships and JS targeting. A consumer
    /// supplied <c>id</c> in <see cref="Attributes"/> wins; otherwise an id is
    /// generated once for this component instance.
    /// </summary>
    public string Id
    {
        get
        {
            if (Attributes is not null)
            {
                foreach (KeyValuePair<string, object> attribute in Attributes)
                {
                    if (!string.Equals(attribute.Key, "id", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string? explicitId = Convert.ToString(attribute.Value, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(explicitId)) return explicitId;
                    break;
                }
            }

            return _id ??= "omni-" + Guid.NewGuid().ToString("N")[..8];
        }
    }

    [Inject] private IServiceProvider? ServiceProvider { get; set; }

    [CascadingParameter(Name = OmniCultureScope.CultureCascadeName)]
    private CultureInfo? CascadedCulture { get; set; }

    [CascadingParameter(Name = OmniCultureScope.UICultureCascadeName)]
    private CultureInfo? CascadedUICulture { get; set; }

    private OmniTexts? _texts;

    /// <summary>
    /// User-facing strings a component falls back to when the consumer did not pass an
    /// explicit parameter. Resolved from DI when registered (see
    /// <c>AddOmniComponents(o =&gt; o.Texts = ...)</c>), otherwise the built-in pt-BR set.
    /// Resolution stays optional on purpose: a component still renders correctly in a host
    /// that never called <c>AddOmniComponents</c>.
    /// </summary>
    protected OmniTexts Texts => _texts ??= ResolveTexts();

    /// <summary>Culture used for numbers, dates and other formatted values.</summary>
    protected CultureInfo FormattingCulture => CascadedCulture ?? CultureInfo.CurrentCulture;

    /// <summary>Culture used for component UI strings.</summary>
    protected CultureInfo TextCulture => CascadedUICulture ?? CultureInfo.CurrentUICulture;

    private OmniTexts ResolveTexts()
    {
        OmniTexts? registered = ServiceProvider?.GetService(typeof(OmniTexts)) as OmniTexts;
        if (registered is not null && !registered.IsLocalizedFacade)
            return registered;

        IOmniLocalizer<OmniBlazorResource>? localizer =
            ServiceProvider?.GetService(typeof(IOmniLocalizer<OmniBlazorResource>))
                as IOmniLocalizer<OmniBlazorResource>;
        return localizer is null
            ? registered ?? OmniTexts.Default
            : OmniTexts.FromLocalizer(localizer, () => TextCulture, () => FormattingCulture);
    }

    /// <summary>
    /// Scope for declarative parameter change detection. Use
    /// <c>RegisterParameter&lt;T&gt;</c> in your constructor (or
    /// <c>OnInitialized</c>) to track when consumers change a parameter.
    /// </summary>
    protected ParameterRegisterScope ParameterScope { get; } = new();

    /// <summary>Convenience: <c>ParameterScope.RegisterParameter&lt;T&gt;(...)</c>.</summary>
    protected ParameterStateBuilder<T> RegisterParameter<T>(string name)
        => ParameterScope.RegisterParameter<T>(name);

    /// <summary>
    /// Observes deliberately detached component work and routes failures through
    /// Blazor's normal exception boundary.
    /// </summary>
    protected void ObserveTask(Task task, string? operation = null)
        => TaskObserver.Observe(task, DispatchExceptionAsync, operation);

    /// <summary>ValueTask overload of <see cref="ObserveTask(Task,string?)"/>.</summary>
    protected void ObserveTask(ValueTask task, string? operation = null)
        => TaskObserver.Observe(task, DispatchExceptionAsync, operation);

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
