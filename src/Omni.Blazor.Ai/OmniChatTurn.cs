using Omni.Blazor.Models;

namespace Omni.Blazor.Ai;

/// <summary>
/// One turn in an AI conversation managed by <see cref="OmniChatClient"/>. Carries a
/// <see cref="MessageRole"/> (not a user id — this is the AI shape), the accumulating
/// content, and streaming/error flags the UI can bind to.
/// </summary>
public sealed class OmniChatTurn
{
    private string _content;
    private int _isStreaming;
    private int _isError;

    public OmniChatTurn(MessageRole role, string content = "")
    {
        Role = role;
        _content = content;
    }

    /// <summary>Who produced this turn: User / Assistant / System.</summary>
    public MessageRole Role { get; }

    /// <summary>The turn's text. For a streaming assistant turn it grows as tokens arrive.</summary>
    public string Content
    {
        get => Volatile.Read(ref _content);
        set => Volatile.Write(ref _content, value ?? string.Empty);
    }

    /// <summary>True while the assistant is still streaming this turn's tokens.</summary>
    public bool IsStreaming
    {
        get => Volatile.Read(ref _isStreaming) != 0;
        set => Volatile.Write(ref _isStreaming, value ? 1 : 0);
    }

    /// <summary>True when this turn holds an error message (the request failed).</summary>
    public bool IsError
    {
        get => Volatile.Read(ref _isError) != 0;
        set => Volatile.Write(ref _isError, value ? 1 : 0);
    }

    /// <summary>When the turn was created (UTC).</summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}
