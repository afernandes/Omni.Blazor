using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniChat"/>: structure, message rendering
/// with author alignment (mine vs theirs), the roster, send/clear callbacks, the
/// typing indicator, and the cross-cutting splat.
/// </summary>
public class OmniChatTests : TestContextBase
{
    private static OmniChatUser[] Users() => new[]
    {
        new OmniChatUser { Id = "me",  Name = "Eu",        Color = "#1976d2" },
        new OmniChatUser { Id = "bob", Name = "Bob Silva", Color = "#388e3c" },
    };

    private static List<OmniChatMessage> Msgs() => new()
    {
        new() { Content = "Olá pessoal", UserId = "bob", Timestamp = DateTime.Today.AddHours(9) },
        new() { Content = "Oi **Bob**!", UserId = "me",  Timestamp = DateTime.Today.AddHours(9).AddMinutes(1) },
    };

    private IRenderedComponent<OmniChat> RenderChat(
        Action<ComponentParameterCollectionBuilder<OmniChat>>? extra = null,
        List<OmniChatMessage>? messages = null)
        => Render<OmniChat>(p =>
        {
            p.Add(c => c.CurrentUserId, "me");
            p.Add(c => c.Users, Users());
            p.Add(c => c.Messages, messages ?? Msgs());
            extra?.Invoke(p);
        });

    // ─── Structure ────────────────────────────────────────────────────────

    [Fact]
    public void Renders_messages_and_input()
    {
        var cut = RenderChat();
        Assert.NotNull(cut.Find("div.omni-chat"));
        Assert.NotNull(cut.Find(".omni-chat-messages"));
        Assert.NotNull(cut.Find(".omni-chat-textarea"));
        Assert.Equal(2, cut.FindAll(".omni-chat-message").Count);
    }

    [Fact]
    public void Shows_empty_state_when_no_messages()
    {
        var cut = RenderChat(messages: new());
        Assert.NotNull(cut.Find(".omni-chat-empty"));
        Assert.Empty(cut.FindAll(".omni-chat-message"));
    }

    [Fact]
    public void Appends_Class_and_splats_attributes()
    {
        var cut = RenderChat(p => p.Add(c => c.Class, "team").AddUnmatched("data-testid", "ch1"));
        var root = cut.Find("div.omni-chat");
        Assert.Contains("team", root.ClassName);
        Assert.Equal("ch1", root.GetAttribute("data-testid"));
    }

    // ─── Alignment & author ───────────────────────────────────────────────

    [Fact]
    public void Aligns_own_vs_others_messages()
    {
        var cut = RenderChat();
        var msgs = cut.FindAll(".omni-chat-message");
        // Bob's message (other) first, mine second.
        Assert.Contains("omni-chat-message-other", msgs[0].ClassName);
        Assert.Contains("omni-chat-message-mine", msgs[1].ClassName);
    }

    [Fact]
    public void Shows_author_name_only_for_others()
    {
        var cut = RenderChat();
        var msgs = cut.FindAll(".omni-chat-message");
        Assert.Contains("Bob Silva", msgs[0].QuerySelector(".omni-chat-name")?.TextContent ?? "");
        Assert.Null(msgs[1].QuerySelector(".omni-chat-name")); // own message: no name
    }

    [Fact]
    public void Renders_bubble_as_markdown()
    {
        var cut = RenderChat();
        // "Oi **Bob**!" → <strong>Bob</strong>
        Assert.Contains(cut.FindAll(".omni-chat-bubble strong"), s => s.TextContent == "Bob");
    }

    [Fact]
    public void Renders_roster_with_overflow()
    {
        var many = Enumerable.Range(0, 8).Select(i => new OmniChatUser { Id = $"u{i}", Name = $"User {i}" }).ToArray();
        var cut = Render<OmniChat>(p => p
            .Add(c => c.CurrentUserId, "u0")
            .Add(c => c.Users, many)
            .Add(c => c.MaxVisibleUsers, 3));
        Assert.Equal(3, cut.FindAll(".omni-chat-roster-avatar").Count);
        Assert.Equal("+5", cut.Find(".omni-chat-roster-more").TextContent);
    }

    // ─── Send / clear ─────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_adds_message_and_fires_callbacks()
    {
        IEnumerable<OmniChatMessage>? changed = null;
        OmniChatMessage? sent = null;
        var cut = RenderChat(p => p
            .Add(c => c.MessagesChanged, EventCallback.Factory.Create<IEnumerable<OmniChatMessage>>(this, m => changed = m))
            .Add(c => c.MessageSent, EventCallback.Factory.Create<OmniChatMessage>(this, m => sent = m)));

        await cut.InvokeAsync(() => cut.Instance.SendAsync("Nova mensagem"));

        Assert.NotNull(changed);
        Assert.Equal(3, changed!.Count());
        Assert.NotNull(sent);
        Assert.Equal("Nova mensagem", sent!.Content);
        Assert.Equal("me", sent.UserId);
    }

    [Fact]
    public async Task SendAsync_ignores_blank_and_without_sender()
    {
        IEnumerable<OmniChatMessage>? changed = null;
        var cut = Render<OmniChat>(p => p
            .Add(c => c.CurrentUserId, "")   // no sender
            .Add(c => c.Users, Users())
            .Add(c => c.Messages, Msgs())
            .Add(c => c.MessagesChanged, EventCallback.Factory.Create<IEnumerable<OmniChatMessage>>(this, m => changed = m)));

        await cut.InvokeAsync(() => cut.Instance.SendAsync("   "));   // blank
        await cut.InvokeAsync(() => cut.Instance.SendAsync("hi"));    // no sender
        Assert.Null(changed);
    }

    [Fact]
    public async Task Enter_sends_via_OnEnterPressed()
    {
        OmniChatMessage? sent = null;
        var cut = RenderChat(p => p.Add(c => c.MessageSent,
            EventCallback.Factory.Create<OmniChatMessage>(this, m => sent = m)));

        await cut.InvokeAsync(() => cut.Instance.OnEnterPressed("via enter"));
        Assert.Equal("via enter", sent?.Content);
    }

    [Fact]
    public async Task ClearChatAsync_empties_and_fires_ChatCleared()
    {
        IEnumerable<OmniChatMessage>? changed = null;
        var cleared = false;
        var cut = RenderChat(p => p
            .Add(c => c.MessagesChanged, EventCallback.Factory.Create<IEnumerable<OmniChatMessage>>(this, m => changed = m))
            .Add(c => c.ChatCleared, EventCallback.Factory.Create(this, () => cleared = true)));

        await cut.InvokeAsync(() => cut.Instance.ClearChatAsync());
        Assert.Empty(changed!);
        Assert.True(cleared);
    }

    [Fact]
    public void Disabled_hides_clear_and_disables_input()
    {
        var cut = RenderChat(p => p.Add(c => c.Disabled, true));
        Assert.Empty(cut.FindAll(".omni-chat-clear"));
        Assert.NotNull(cut.Find(".omni-chat-textarea").GetAttribute("disabled"));
    }

    // ─── Typing ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Typing_indicator_shows_for_remote_user()
    {
        var cut = RenderChat(p => p.Add(c => c.ShowTypingIndicator, true));
        Assert.Empty(cut.FindAll(".omni-chat-typing"));

        await cut.InvokeAsync(() => cut.Instance.SetUserTypingAsync("bob", true));
        Assert.NotNull(cut.Find(".omni-chat-typing"));
        Assert.Contains("Bob", cut.Find(".omni-chat-typing-text").TextContent);

        await cut.InvokeAsync(() => cut.Instance.SetUserTypingAsync("bob", false));
        Assert.Empty(cut.FindAll(".omni-chat-typing"));
    }

    [Fact]
    public void Typing_timeout_after_dispose_is_a_safe_noop()
    {
        var cut = RenderChat();
        OmniChat chat = cut.Instance;
        chat.Dispose(); // sets _disposed = true (as navigation would)

        // The threadpool typing-timer can fire ~1.5s later, after the component is gone
        // (navigation). It must be a guarded no-op — never an async-void exception that
        // tears down the Blazor Server circuit.
        Assert.Null(Record.Exception(() => chat.OnTypingTimeout(null)));
    }
}
