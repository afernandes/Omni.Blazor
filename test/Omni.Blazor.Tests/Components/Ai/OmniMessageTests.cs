using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Ai;

public class OmniMessageTests : TestContextBase
{
    [Theory]
    [InlineData(MessageRole.Assistant, "omni-message-assistant")]
    [InlineData(MessageRole.User, "omni-message-user")]
    [InlineData(MessageRole.System, "omni-message-system")]
    public void Applies_role_modifier(MessageRole role, string expectedClass)
    {
        var cut = RenderComponent<OmniMessage>(p => p.Add(c => c.Role, role).Add(c => c.Content, "hi"));
        Assert.Contains(expectedClass, cut.Find("div.omni-message").ClassName);
    }

    [Fact]
    public void Renders_author_and_markdown_content()
    {
        var cut = RenderComponent<OmniMessage>(p => p.Add(c => c.Author, "Assistant").Add(c => c.Content, "**hi**"));
        Assert.Equal("Assistant", cut.Find(".omni-message-author").TextContent);
        Assert.Contains("<strong>hi</strong>", cut.Find(".omni-message-content .omni-markdown").InnerHtml);
    }

    [Fact]
    public void Streaming_shows_caret_in_content()
    {
        var cut = RenderComponent<OmniMessage>(p => p.Add(c => c.Content, "typing").Add(c => c.Streaming, true));
        Assert.NotNull(cut.Find(".omni-message-content .omni-streaming-caret"));
    }

    [Fact]
    public void ChildContent_overrides_Content()
    {
        var cut = RenderComponent<OmniMessage>(p => p
            .Add(c => c.Content, "ignored")
            .AddChildContent("<p class=\"custom\">custom</p>"));
        Assert.NotNull(cut.Find(".omni-message-content p.custom"));
        Assert.Empty(cut.FindAll(".omni-message-content .omni-streaming-text"));
    }

    [Fact]
    public void Renders_default_avatar_from_initials()
    {
        var cut = RenderComponent<OmniMessage>(p => p.Add(c => c.AvatarInitials, "AI").Add(c => c.Content, "x"));
        var avatar = cut.Find(".omni-message-avatar .omni-avatar");
        Assert.Contains("AI", avatar.TextContent);
    }

    [Fact]
    public void AvatarContent_overrides_default_avatar()
    {
        var cut = RenderComponent<OmniMessage>(p => p
            .Add(c => c.AvatarContent, b => b.AddMarkupContent(0, "<img class=\"bot\" />"))
            .Add(c => c.Content, "x"));
        Assert.NotNull(cut.Find(".omni-message-avatar img.bot"));
        Assert.Empty(cut.FindAll(".omni-message-avatar .omni-avatar"));
    }

    [Fact]
    public void Renders_Footer_slot()
    {
        var cut = RenderComponent<OmniMessage>(p => p
            .Add(c => c.Content, "x")
            .Add(c => c.Footer, b => b.AddMarkupContent(0, "<span class=\"src\">[1]</span>")));
        Assert.NotNull(cut.Find(".omni-message-footer span.src"));
    }

    [Fact]
    public void Appends_Class_Style_and_Attributes()
    {
        var cut = RenderComponent<OmniMessage>(p => p
            .Add(c => c.Content, "x")
            .Add(c => c.Class, "cc").Add(c => c.Style, "gap: 4px")
            .AddUnmatched("data-testid", "msg"));
        var root = cut.Find("div.omni-message");
        Assert.Contains("cc", root.ClassName);
        Assert.Equal("gap: 4px", root.GetAttribute("style"));
        Assert.Equal("msg", root.GetAttribute("data-testid"));
    }
}
