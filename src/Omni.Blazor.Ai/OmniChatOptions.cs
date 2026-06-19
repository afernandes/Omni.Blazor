using Microsoft.Extensions.AI;

namespace Omni.Blazor.Ai;

/// <summary>
/// Configuration for an <see cref="OmniChatClient"/> conversation.
/// </summary>
public sealed class OmniChatOptions
{
    /// <summary>Optional system / instruction prompt prepended to every request.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Cap on the number of prior turns sent to the model (sliding window). Null = send
    /// the whole conversation. The system prompt is always sent on top of this.
    /// </summary>
    public int? MaxHistory { get; set; }

    /// <summary>
    /// Message shown in the assistant bubble when a request fails before any token
    /// arrives. Null → a generic "⚠️ {error}" is used.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Inference options forwarded to <see cref="IChatClient"/> — model id, temperature,
    /// max output tokens, tools, etc. (the standard Microsoft.Extensions.AI surface).
    /// </summary>
    public ChatOptions? ChatOptions { get; set; }
}
