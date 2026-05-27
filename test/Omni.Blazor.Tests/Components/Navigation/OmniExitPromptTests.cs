using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniExitPrompt"/>: renders no DOM,
/// registers a <c>LocationChangingHandler</c> on the bUnit-provided fake
/// <see cref="NavigationManager"/>, and gates the prompt with the
/// <c>When</c> predicate. The native <c>beforeunload</c> path is JS interop
/// only and exercised in browser integration tests.
/// </summary>
public class OmniExitPromptTests : TestContextBase
{
    [Fact]
    public void Renders_no_dom()
    {
        var cut = RenderComponent<OmniExitPrompt>();

        // The component is render-fragment-empty; bUnit reports zero nodes.
        Assert.Equal(0, cut.Nodes.Length);
    }

    [Fact]
    public void When_returning_false_allows_navigation()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<OmniExitPrompt>(p => p
            .Add(c => c.When, () => false));

        // Navigate — handler should not block.
        nav.NavigateTo("/somewhere-else");
        Assert.EndsWith("/somewhere-else", nav.Uri);
    }

    [Fact]
    public void Default_When_null_is_treated_as_guard_off_for_native_path()
    {
        // Renders cleanly even with no `When` set — no exceptions during
        // OnInitialized / OnParametersSetAsync. The native-prompt JS path
        // is silently swallowed by the bUnit loose-mode JS runtime.
        var cut = RenderComponent<OmniExitPrompt>();

        Assert.Equal(0, cut.Nodes.Length);
    }

    [Fact]
    public void Parameters_have_default_portuguese_text()
    {
        var cut = RenderComponent<OmniExitPrompt>();

        Assert.Equal("Sair sem salvar?", cut.Instance.Title);
        Assert.Equal("Você tem alterações não salvas. Deseja sair mesmo assim?", cut.Instance.Text);
        Assert.Equal("Sair sem salvar", cut.Instance.ConfirmText);
        Assert.Equal("Continuar editando", cut.Instance.CancelText);
        Assert.False(cut.Instance.UseNativePrompt);
    }

    [Fact]
    public void Custom_text_parameters_are_captured()
    {
        var cut = RenderComponent<OmniExitPrompt>(p => p
            .Add(c => c.Title, "Discard?")
            .Add(c => c.Text, "You will lose changes.")
            .Add(c => c.ConfirmText, "Discard")
            .Add(c => c.CancelText, "Keep editing")
            .Add(c => c.UseNativePrompt, true));

        Assert.Equal("Discard?", cut.Instance.Title);
        Assert.Equal("You will lose changes.", cut.Instance.Text);
        Assert.Equal("Discard", cut.Instance.ConfirmText);
        Assert.Equal("Keep editing", cut.Instance.CancelText);
        Assert.True(cut.Instance.UseNativePrompt);
    }

    [Fact]
    public void Dispose_cleans_up_without_throwing()
    {
        _ = RenderComponent<OmniExitPrompt>(p => p.Add(c => c.When, () => true));

        // Dispose path includes a JS interop call that must not throw under
        // the loose-mode bUnit runtime (catch-all in the source guards it).
        Dispose();
    }
}
