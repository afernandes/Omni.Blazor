using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Forms.Validators;

/// <summary>
/// Stub <see cref="IOmniFormComponent"/> used to drive the validators in
/// isolation. Validators only consume <c>GetValue</c> / <c>HasValue</c> /
/// <c>FieldIdentifier</c> — the rest of the input surface (rendering,
/// EditContext wiring) is irrelevant to unit tests of the validation rule.
/// </summary>
internal sealed class StubFormComponent : IOmniFormComponent
{
    private readonly object? _value;

    public StubFormComponent(object? value, string name = "field", object? model = null)
    {
        _value = value;
        ResolvedName = name;
        // FieldIdentifier requires a non-null model — use a tiny placeholder when none supplied.
        FieldIdentifier = new FieldIdentifier(model ?? new HolderModel { Value = value }, "Value");
    }

    public string ResolvedName { get; }
    public FieldIdentifier FieldIdentifier { get; }

    public object? GetValue() => _value;

    public bool HasValue
    {
        get
        {
            if (_value is null) return false;
            if (_value is string s) return !string.IsNullOrEmpty(s);
            return true;
        }
    }

    private sealed class HolderModel
    {
        public object? Value { get; set; }
    }
}

/// <summary>
/// Stub <see cref="IOmniFormRegistry"/> for cross-field validators
/// (<c>OmniCompareValidator</c> with <c>OtherComponent</c>).
/// </summary>
internal sealed class StubFormRegistry : IOmniFormRegistry
{
    private readonly Dictionary<string, IOmniFormComponent> _by = new(StringComparer.OrdinalIgnoreCase);

    public StubFormRegistry Register(IOmniFormComponent c)
    {
        _by[c.ResolvedName] = c;
        return this;
    }

    public IOmniFormComponent? FindComponent(string name)
        => _by.TryGetValue(name, out var c) ? c : null;

    public void RegisterComponent(IOmniFormComponent component) => Register(component);
    public void UnregisterComponent(IOmniFormComponent component) => _by.Remove(component.ResolvedName);
}
