namespace Omni.Blazor.Models;

/// <summary>
/// A single password-policy rule for <c>OmniPasswordStrength</c>: a human-readable
/// <see cref="Label"/> plus a <see cref="Test"/> predicate that returns <c>true</c> when the
/// current password satisfies the rule. Build a list of these to mirror an arbitrary policy
/// (e.g. ASP.NET Identity's <c>PasswordOptions</c>).
/// </summary>
public sealed class OmniPasswordRule
{
    public OmniPasswordRule() { }

    public OmniPasswordRule(string label, Func<string, bool> test)
    {
        Label = label;
        Test = test;
    }

    /// <summary>Label shown in the requirement checklist.</summary>
    public string Label { get; set; } = "";

    /// <summary>Predicate evaluated against the current password (returns true when satisfied).</summary>
    public Func<string, bool> Test { get; set; } = _ => false;
}
