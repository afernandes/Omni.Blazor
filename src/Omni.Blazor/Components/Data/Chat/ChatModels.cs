using System.Globalization;

namespace Omni.Blazor.Components;

/// <summary>A single chat message. Alignment (mine vs theirs) is decided by
/// <see cref="UserId"/> against the chat's <c>CurrentUserId</c>.</summary>
public class OmniChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>When true, shows a streaming "…" affordance instead of rendering markdown.</summary>
    public bool IsStreaming { get; set; }
}

/// <summary>Args for <c>OmniChat.TypingChanged</c> — the current user started/stopped typing.</summary>
public sealed class OmniChatTypingEventArgs
{
    public string UserId { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}

/// <summary>A chat participant — provides the avatar, name and accent color shown beside their messages.</summary>
public class OmniChatUser
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    /// <summary>Accent color for this user's avatar/name (any CSS color). Optional.</summary>
    public string? Color { get; set; }

    public bool IsOnline { get; set; } = true;

    /// <summary>Up-to-two-letter initials derived from <see cref="Name"/>.</summary>
    public string GetInitials()
    {
        if (string.IsNullOrWhiteSpace(Name)) return "?";
        var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper(CultureInfo.InvariantCulture);
        return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpper(CultureInfo.InvariantCulture);
    }
}
