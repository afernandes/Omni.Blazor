using Microsoft.AspNetCore.Components.Forms;

namespace Omni.Blazor.Components;

/// <summary>
/// Internal validation seam for generated subforms that must participate in
/// the parent OmniForm asynchronous validation pass.
/// </summary>
internal interface IOmniFormValidationParticipant
{
    ValueTask ValidateAsync(
        EditContext context,
        ValidationMessageStore store,
        CancellationToken cancellationToken);
}

/// <summary>Owns the lifetime of asynchronous validation participants.</summary>
internal interface IOmniFormValidationParticipantRegistry
{
    void RegisterValidationParticipant(IOmniFormValidationParticipant participant);
    void UnregisterValidationParticipant(IOmniFormValidationParticipant participant);
}
