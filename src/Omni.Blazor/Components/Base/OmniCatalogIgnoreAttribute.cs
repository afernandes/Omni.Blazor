namespace Omni.Blazor.Components;

/// <summary>
/// Excludes an implementation-only <c>ComponentBase</c> type from the public
/// Omni component catalog. Public user-facing components are included by
/// default and should not use this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OmniCatalogIgnoreAttribute : Attribute;
