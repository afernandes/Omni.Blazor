using Bunit;
using Omni.Blazor.Ai;
using Omni.Blazor.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Tests.Ai;

namespace Omni.Blazor.Tests.Components.Ai;

public class OmniAiConversationTests : TestContextBase
{
    private static OmniChatClient Client(params string[] chunks) => new(new FakeChatClient(chunks));

    private IRenderedComponent<OmniAiConversation> Render(OmniChatClient client,
        Action<Bunit.ComponentParameterCollectionBuilder<OmniAiConversation>>? extra = null)
        => Render<OmniAiConversation>(p => { p.Add(c => c.Client, client); extra?.Invoke(p); });

    [Fact]
    public void Renders_root_log_and_composer()
    {
        var cut = Render(Client("ok"));
        Assert.NotNull(cut.Find("div.omni-ai-conversation"));
        Assert.NotNull(cut.Find(".omni-ai-conversation-log[role='log']"));
        Assert.NotNull(cut.Find(".omni-ai-conversation-composer .omni-prompt-input"));
    }

    [Fact]
    public void Appends_Class_Style_and_Attributes()
    {
        var cut = Render(Client(), p => p
            .Add(c => c.Class, "cc").Add(c => c.Style, "height: 500px")
            .AddUnmatched("data-testid", "chat"));
        var root = cut.Find("div.omni-ai-conversation");
        Assert.Contains("cc", root.ClassName);
        Assert.Equal("height: 500px", root.GetAttribute("style"));
        Assert.Equal("chat", root.GetAttribute("data-testid"));
    }

    [Fact]
    public void Shows_empty_content_when_no_turns()
    {
        var cut = Render(Client(), p => p.Add(c => c.EmptyContent,
            b => b.AddMarkupContent(0, "<span class=\"hello\">Comece a conversa</span>")));
        Assert.NotNull(cut.Find(".omni-ai-conversation-empty .hello"));
    }

    [Fact]
    public void Renders_seeded_turns_as_messages()
    {
        var client = Client();
        client.AddTurn(new OmniChatTurn(MessageRole.User, "Olá"));
        client.AddTurn(new OmniChatTurn(MessageRole.Assistant, "Oi! Tudo bem?"));

        var cut = Render(client);

        Assert.Equal(2, cut.FindAll(".omni-message").Count);
        Assert.NotNull(cut.Find(".omni-message-user"));
        Assert.Contains("Oi! Tudo bem?", cut.Find(".omni-message-assistant").TextContent);
    }

    [Fact]
    public void Hides_empty_content_once_a_turn_exists()
    {
        var client = Client();
        client.AddTurn(new OmniChatTurn(MessageRole.User, "Olá"));
        var cut = Render(client, p => p.Add(c => c.EmptyContent,
            b => b.AddMarkupContent(0, "<span class=\"hello\">vazio</span>")));
        Assert.Empty(cut.FindAll(".omni-ai-conversation-empty"));
    }

    [Fact]
    public async Task Reacts_to_client_changes()
    {
        var client = Client("Olá", " mundo");
        var cut = Render(client);
        Assert.Empty(cut.FindAll(".omni-message"));

        await cut.InvokeAsync(() => client.SendAsync("oi"));

        Assert.Equal(2, cut.FindAll(".omni-message").Count);
        Assert.Contains("Olá mundo", cut.Find(".omni-message-assistant").TextContent);
        Assert.Contains("oi", cut.Find(".omni-message-user").TextContent);
    }

    [Fact]
    public void Marks_error_turns()
    {
        var client = Client();
        client.AddTurn(new OmniChatTurn(MessageRole.Assistant, "deu ruim") { IsError = true });
        var cut = Render(client);
        Assert.NotNull(cut.Find(".omni-message.omni-message-error"));
    }

    [Fact]
    public void Renders_author_names_when_enabled()
    {
        var client = Client();
        client.AddTurn(new OmniChatTurn(MessageRole.User, "q"));
        client.AddTurn(new OmniChatTurn(MessageRole.Assistant, "a"));
        var cut = Render(client, p => p
            .Add(c => c.ShowAuthor, true)
            .Add(c => c.UserName, "Anderson")
            .Add(c => c.AssistantName, "Omni AI"));

        var authors = cut.FindAll(".omni-message-author").Select(a => a.TextContent).ToList();
        Assert.Contains("Anderson", authors);
        Assert.Contains("Omni AI", authors);
    }

    [Fact]
    public void Renders_header_content()
    {
        var cut = Render(Client(), p => p.Add(c => c.HeaderContent,
            b => b.AddMarkupContent(0, "<h2 class=\"title\">Assistente</h2>")));
        Assert.Equal("Assistente", cut.Find(".omni-ai-conversation-header .title").TextContent);
    }

    [Fact]
    public async Task Send_button_drives_the_client()
    {
        var client = Client("resposta");
        var cut = Render(client);

        cut.Find("textarea.omni-prompt-input-field").Input("pergunta");
        cut.Find("button[aria-label='Send']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("pergunta", cut.Find(".omni-message-user").TextContent);
            Assert.Contains("resposta", cut.Find(".omni-message-assistant").TextContent);
        });
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Streaming_disables_composer_and_flags_root()
    {
        var gated = new GatedChatClient();
        await using var client = new OmniChatClient(gated);
        var cut = Render(client);

        Task send = cut.InvokeAsync(() => client.SendAsync("oi"));
        await gated.FirstYielded;

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("omni-ai-conversation-streaming", cut.Find(".omni-ai-conversation").ClassName);
            Assert.True(cut.Find("textarea.omni-prompt-input-field").HasAttribute("disabled"));
        });

        gated.Release();
        await send;
    }

    [Fact]
    public void Rendering_without_a_client_fails()
        => Assert.ThrowsAny<Exception>(() => Render<OmniAiConversation>(p => { }));

    [Fact]
    public async Task Swapping_the_client_resubscribes_to_the_new_one()
    {
        var first = Client();
        var second = Client("nova");
        var cut = Render(first);

        // Parent swaps the Client instance.
        cut.Render(p => p.Add(c => c.Client, second));

        // Reacts to the NEW client.
        await cut.InvokeAsync(() => second.SendAsync("oi"));
        Assert.Equal(2, cut.FindAll(".omni-message").Count);
        Assert.Contains("nova", cut.Find(".omni-message-assistant").TextContent);

        // No longer reacts to the OLD client (unsubscribed — no leak, no ghost render).
        first.AddTurn(new OmniChatTurn(MessageRole.User, "fantasma"));
        Assert.DoesNotContain("fantasma", cut.Markup);
    }
}
