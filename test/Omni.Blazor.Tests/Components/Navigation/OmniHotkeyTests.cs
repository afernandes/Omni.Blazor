using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniHotkey"/>: renders no DOM, registers
/// itself against the <see cref="HotkeyService"/>, supports either a string
/// <c>Combo</c> spec or <c>Key</c>+<c>Modifiers</c> composition, and cleans up
/// the registration on dispose. The actual key dispatch happens in JS and is
/// covered by browser tests.
/// </summary>
public class OmniHotkeyTests : TestContextBase
{
    [Fact]
    public void Renders_no_dom()
    {
        var cut = Render<OmniHotkey>(p => p
            .Add(c => c.Combo, "Ctrl+K"));

        Assert.Equal(0, cut.Nodes.Length);
    }

    [Fact]
    public void Combo_string_registers_one_handle()
    {
        var hotkeys = Services.GetRequiredService<HotkeyService>();
        var before = hotkeys.RegistrationCount;

        var cut = Render<OmniHotkey>(p => p
            .Add(c => c.Combo, "Ctrl+K"));

        Assert.Equal(before + 1, hotkeys.RegistrationCount);
    }

    [Fact]
    public void Key_plus_Modifiers_registers_one_handle()
    {
        var hotkeys = Services.GetRequiredService<HotkeyService>();
        var before = hotkeys.RegistrationCount;

        var cut = Render<OmniHotkey>(p => p
            .Add(c => c.Key, "K")
            .Add(c => c.Modifiers, Modifier.Ctrl | Modifier.Shift));

        Assert.Equal(before + 1, hotkeys.RegistrationCount);
    }

    [Fact]
    public void Multiple_combos_pipe_separated_still_a_single_handle()
    {
        var hotkeys = Services.GetRequiredService<HotkeyService>();
        var before = hotkeys.RegistrationCount;

        var cut = Render<OmniHotkey>(p => p
            .Add(c => c.Combo, "Ctrl+K|Meta+K"));

        // The service buckets multiple combos under a single registration.
        Assert.Equal(before + 1, hotkeys.RegistrationCount);
    }

    [Fact]
    public void Empty_Combo_and_Key_registers_nothing()
    {
        var hotkeys = Services.GetRequiredService<HotkeyService>();
        var before = hotkeys.RegistrationCount;

        var cut = Render<OmniHotkey>(); // No Combo, no Key.

        Assert.Equal(before, hotkeys.RegistrationCount);
    }

    [Fact]
    public async Task Disposing_component_unregisters_the_handle()
    {
        var hotkeys = Services.GetRequiredService<HotkeyService>();
        var before = hotkeys.RegistrationCount;

        _ = Render<OmniHotkey>(p => p
            .Add(c => c.Combo, "Ctrl+K"));

        Assert.Equal(before + 1, hotkeys.RegistrationCount);

        // HotkeyService is IAsyncDisposable-only — bunit 2.x requires DisposeAsync.
        await DisposeAsync();
        Assert.Equal(before, hotkeys.RegistrationCount);
    }

    [Fact]
    public void Changing_Combo_re_registers_under_a_single_handle()
    {
        var hotkeys = Services.GetRequiredService<HotkeyService>();
        var before = hotkeys.RegistrationCount;

        var cut = Render<OmniHotkey>(p => p
            .Add(c => c.Combo, "Ctrl+K"));
        Assert.Equal(before + 1, hotkeys.RegistrationCount);

        cut.Render(p => p.Add(c => c.Combo, "Ctrl+P"));
        // The old handle is disposed and a new one is created -> still +1.
        Assert.Equal(before + 1, hotkeys.RegistrationCount);
    }

    [Fact]
    public void Captures_OnPressed_PreventDefault_StopPropagation_defaults()
    {
        var cut = Render<OmniHotkey>(p => p
            .Add(c => c.Combo, "Ctrl+K"));

        Assert.True(cut.Instance.PreventDefault);
        Assert.False(cut.Instance.StopPropagation);
        Assert.False(cut.Instance.Disabled);
    }
}
