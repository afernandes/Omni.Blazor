using Microsoft.Extensions.AI;
using Omni.Blazor.Ai;

namespace Omni.Blazor.Tests.Ai;

public class OmniChatClientTests
{
    [Fact]
    public void Constructor_null_client_throws()
        => Assert.Throws<ArgumentNullException>(() => new OmniChatClient(null!));

    [Fact]
    public async Task SendAsync_streams_response_into_an_assistant_turn()
    {
        await using var client = new OmniChatClient(new FakeChatClient("Hel", "lo ", "world"));

        await client.SendAsync("hi");

        Assert.Equal(2, client.Turns.Count);
        Assert.Equal(MessageRole.User, client.Turns[0].Role);
        Assert.Equal("hi", client.Turns[0].Content);
        OmniChatTurn assistant = client.Turns[1];
        Assert.Equal(MessageRole.Assistant, assistant.Role);
        Assert.Equal("Hello world", assistant.Content);
        Assert.False(assistant.IsStreaming);
        Assert.False(assistant.IsError);
        Assert.False(client.IsStreaming);
    }

    [Fact]
    public async Task SendAsync_trims_the_user_text()
    {
        await using var client = new OmniChatClient(new FakeChatClient("ok"));

        await client.SendAsync("  spaced  ");

        Assert.Equal("spaced", client.Turns[0].Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SendAsync_blank_input_is_a_noop(string? input)
    {
        await using var client = new OmniChatClient(new FakeChatClient("ok"));

        await client.SendAsync(input!);

        Assert.Empty(client.Turns);
    }

    [Fact]
    public async Task SendAsync_raises_Changed_as_tokens_arrive()
    {
        await using var client = new OmniChatClient(new FakeChatClient("a", "b", "c"));
        int changes = 0;
        client.Changed += () => changes++;

        await client.SendAsync("hi");

        // user turn + assistant turn + start + 3 tokens + finish ⇒ comfortably > tokens
        Assert.True(changes >= 3, $"expected several Changed raises, got {changes}");
    }

    [Fact]
    public async Task SendAsync_prepends_the_system_prompt()
    {
        var fake = new FakeChatClient("ok");
        await using var client = new OmniChatClient(fake, new OmniChatOptions { SystemPrompt = "You are Omni." });

        await client.SendAsync("hi");

        Assert.NotNull(fake.LastMessages);
        Assert.Equal(ChatRole.System, fake.LastMessages![0].Role);
        Assert.Equal("You are Omni.", fake.LastMessages[0].Text);
        Assert.Equal(ChatRole.User, fake.LastMessages[1].Role);
    }

    [Fact]
    public async Task SendAsync_without_system_prompt_sends_only_turns()
    {
        var fake = new FakeChatClient("ok");
        await using var client = new OmniChatClient(fake);

        await client.SendAsync("hi");

        Assert.Single(fake.LastMessages!);
        Assert.Equal(ChatRole.User, fake.LastMessages![0].Role);
    }

    [Fact]
    public async Task SendAsync_maps_every_role_to_ChatRole()
    {
        var fake = new FakeChatClient("ok");
        await using var client = new OmniChatClient(fake);
        client.AddTurn(new OmniChatTurn(MessageRole.System, "sys"));
        client.AddTurn(new OmniChatTurn(MessageRole.User, "q"));
        client.AddTurn(new OmniChatTurn(MessageRole.Assistant, "a"));

        await client.SendAsync("hi");

        // seeded System, User, Assistant + the new User turn (streaming assistant turn excluded)
        Assert.Equal(4, fake.LastMessages!.Count);
        Assert.Equal(ChatRole.System, fake.LastMessages[0].Role);
        Assert.Equal(ChatRole.User, fake.LastMessages[1].Role);
        Assert.Equal(ChatRole.Assistant, fake.LastMessages[2].Role);
        Assert.Equal(ChatRole.User, fake.LastMessages[3].Role);
    }

    [Fact]
    public async Task SendAsync_excludes_the_in_progress_assistant_turn_from_the_request()
    {
        var fake = new FakeChatClient("ok");
        await using var client = new OmniChatClient(fake);

        await client.SendAsync("hi");

        // only the user turn is sent — never the empty streaming assistant turn
        Assert.Single(fake.LastMessages!);
    }

    [Fact]
    public async Task SendAsync_respects_MaxHistory_window()
    {
        var fake = new FakeChatClient("ok");
        await using var client = new OmniChatClient(fake, new OmniChatOptions { MaxHistory = 2 });
        client.AddTurn(new OmniChatTurn(MessageRole.User, "old-1"));
        client.AddTurn(new OmniChatTurn(MessageRole.Assistant, "old-2"));
        client.AddTurn(new OmniChatTurn(MessageRole.User, "recent"));

        await client.SendAsync("now");

        // window of 2 over completed turns: [recent, now] (old-1/old-2 dropped)
        Assert.Equal(2, fake.LastMessages!.Count);
        Assert.Equal("recent", fake.LastMessages[0].Text);
        Assert.Equal("now", fake.LastMessages[1].Text);
    }

    [Fact]
    public async Task SendAsync_forwards_ChatOptions_to_the_client()
    {
        var options = new ChatOptions { ModelId = "gpt-test", Temperature = 0.3f };
        var fake = new FakeChatClient("ok");
        await using var client = new OmniChatClient(fake, new OmniChatOptions { ChatOptions = options });

        await client.SendAsync("hi");

        Assert.Same(options, fake.LastOptions);
    }

    [Fact]
    public async Task SendAsync_failure_marks_the_turn_as_error_with_default_message()
    {
        var fake = new FakeChatClient("ignored") { ThrowOnStream = new InvalidOperationException("boom") };
        await using var client = new OmniChatClient(fake);

        await client.SendAsync("hi");

        OmniChatTurn assistant = client.Turns[1];
        Assert.True(assistant.IsError);
        Assert.False(assistant.IsStreaming);
        Assert.Contains("boom", assistant.Content);
        Assert.False(client.IsStreaming);
    }

    [Fact]
    public async Task SendAsync_failure_uses_custom_ErrorMessage()
    {
        var fake = new FakeChatClient("ignored") { ThrowOnStream = new InvalidOperationException("boom") };
        await using var client = new OmniChatClient(fake, new OmniChatOptions { ErrorMessage = "Algo deu errado." });

        await client.SendAsync("hi");

        Assert.Equal("Algo deu errado.", client.Turns[1].Content);
        Assert.True(client.Turns[1].IsError);
    }

    [Fact]
    public async Task SendAsync_is_a_noop_while_already_streaming()
    {
        var gated = new GatedChatClient();
        await using var client = new OmniChatClient(gated);

        Task first = client.SendAsync("first");
        await gated.FirstYielded;
        Assert.True(client.IsStreaming);
        int countWhileStreaming = client.Turns.Count;

        await client.SendAsync("second"); // guarded — must not add turns or start a stream

        Assert.Equal(countWhileStreaming, client.Turns.Count);

        gated.Release();
        await first;
        Assert.Equal("partial-rest", client.Turns[1].Content);
    }

    [Fact]
    public async Task SendAsync_cancellation_preserves_partial_content()
    {
        var gated = new GatedChatClient();
        await using var client = new OmniChatClient(gated);
        using var cts = new CancellationTokenSource();

        Task send = client.SendAsync("hi", cts.Token);
        await gated.FirstYielded;
        cts.Cancel();
        await send;

        OmniChatTurn assistant = client.Turns[1];
        Assert.Equal("partial", assistant.Content);
        Assert.False(assistant.IsStreaming);
        Assert.False(assistant.IsError);
        Assert.False(client.IsStreaming);
    }

    [Fact]
    public async Task Clear_empties_the_conversation_and_raises_Changed()
    {
        await using var client = new OmniChatClient(new FakeChatClient("ok"));
        await client.SendAsync("hi");
        Assert.NotEmpty(client.Turns);
        bool raised = false;
        client.Changed += () => raised = true;

        client.Clear();

        Assert.Empty(client.Turns);
        Assert.True(raised);
    }

    [Fact]
    public void AddTurn_null_throws()
    {
        using var client = new OmniChatClient(new FakeChatClient());
        Assert.Throws<ArgumentNullException>(() => client.AddTurn(null!));
    }

    [Fact]
    public void AddTurn_appends_and_raises_Changed()
    {
        using var client = new OmniChatClient(new FakeChatClient());
        bool raised = false;
        client.Changed += () => raised = true;

        client.AddTurn(new OmniChatTurn(MessageRole.Assistant, "Olá! Como posso ajudar?"));

        Assert.Single(client.Turns);
        Assert.Equal("Olá! Como posso ajudar?", client.Turns[0].Content);
        Assert.True(raised);
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_underlying_client()
    {
        var fake = new FakeChatClient("ok");
        var client = new OmniChatClient(fake);

        await client.DisposeAsync();

        Assert.True(fake.Disposed);
    }

    [Fact]
    public void OmniChatTurn_defaults_are_sensible()
    {
        var turn = new OmniChatTurn(MessageRole.User, "hi");
        Assert.Equal("hi", turn.Content);
        Assert.False(turn.IsStreaming);
        Assert.False(turn.IsError);
        Assert.Equal(MessageRole.User, turn.Role);
        Assert.True(turn.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task SendAsync_after_dispose_throws()
    {
        var client = new OmniChatClient(new FakeChatClient("ok"));
        await client.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.SendAsync("hi"));
    }

    [Fact]
    public void Dispose_is_idempotent()
    {
        var client = new OmniChatClient(new FakeChatClient("ok"));
        client.Dispose();
        client.Dispose(); // second call must be a safe no-op
    }

    [Fact]
    public async Task DisposeAsync_is_idempotent()
    {
        var client = new OmniChatClient(new FakeChatClient("ok"));
        await client.DisposeAsync();
        await client.DisposeAsync(); // second call must be a safe no-op
    }

    [Fact]
    public async Task Sequential_sends_each_use_a_fresh_token_source()
    {
        // After each send the linked CTS is disposed + cleared, so the next send
        // creates a fresh one and works — no leak, no reuse of a disposed source.
        using var caller = new CancellationTokenSource();
        await using var client = new OmniChatClient(new FakeChatClient("a"));
        await client.SendAsync("one", caller.Token);
        await client.SendAsync("two", caller.Token);
        Assert.Equal(4, client.Turns.Count); // 2 user + 2 assistant
    }

    [Fact]
    public async Task Streaming_render_coalescing_cuts_raise_count_without_losing_content()
    {
        string[] tokens = ["a", "b", "c", "d", "e", "f", "g", "h"];

        // Clock jumps a full second per token → each token is its own frame → a raise each.
        long fastClock = 0;
        long second = System.Diagnostics.Stopwatch.Frequency;
        var fast = new OmniChatClient(new FakeChatClient(tokens)) { NowTicks = () => { fastClock += second; return fastClock; } };
        int fastRaises = 0;
        fast.Changed += () => fastRaises++;
        await fast.SendAsync("hi");
        await fast.DisposeAsync();

        // Frozen clock → all tokens land within one frame → coalesced to far fewer raises.
        long frozen = System.Diagnostics.Stopwatch.GetTimestamp();
        var slow = new OmniChatClient(new FakeChatClient(tokens)) { NowTicks = () => frozen };
        int coalescedRaises = 0;
        slow.Changed += () => coalescedRaises++;
        await slow.SendAsync("hi");

        Assert.Equal("abcdefgh", slow.Turns[1].Content); // content fully preserved
        Assert.True(coalescedRaises < fastRaises,
            $"coalescing should cut raises: coalesced={coalescedRaises} vs fast={fastRaises}");
        await slow.DisposeAsync();
    }
}
