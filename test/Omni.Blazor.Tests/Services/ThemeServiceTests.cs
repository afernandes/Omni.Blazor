namespace Omni.Blazor.Tests.Services;

/// <summary>
/// Behaviour of <see cref="ThemeService"/> under bUnit Loose mode. JS calls are
/// no-ops that return default(T), so the assertions target observable C# state
/// (Accent/Dark/Density/IsInitialized/IsUserPicked), the OnChange event, the
/// UseSystem revert path, and the exact JS invocations for the DOM attribute
/// writes and localStorage persistence (verified by name via VerifyInvoke).
/// </summary>
public class ThemeServiceTests : TestContextBase
{
    private ThemeService NewService() => new(JSInterop.JSRuntime);

    [Fact]
    public void Defaults_are_amber_light_comfortable_and_not_initialized()
    {
        var svc = NewService();
        Assert.Equal("amber", svc.Accent);
        Assert.False(svc.Dark);
        Assert.Equal(LayoutDensity.Comfortable, svc.Density);
        Assert.False(svc.IsInitialized);
        Assert.False(svc.IsUserPicked);
    }

    [Fact]
    public async Task InitializeAsync_with_empty_storage_marks_initialized_and_not_user_picked()
    {
        // Loose mode: storageGet returns null -> the "empty storage" branch runs,
        // adopting whatever the DOM has (also null) and subscribing to the OS.
        var svc = NewService();
        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.InitializeAsync();

        Assert.True(svc.IsInitialized);
        Assert.False(svc.IsUserPicked);
        Assert.Equal("amber", svc.Accent); // unchanged (DOM getAttr was null)
        Assert.False(svc.Dark);
        Assert.Equal(1, changes); // OnChange fires once from the finally block
    }

    [Fact]
    public async Task InitializeAsync_is_idempotent()
    {
        var svc = NewService();
        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.InitializeAsync();
        await svc.InitializeAsync(); // second call is a no-op (IsInitialized guard)

        Assert.True(svc.IsInitialized);
        Assert.Equal(1, changes); // only the first call raised OnChange
    }

    [Fact]
    public async Task InitializeAsync_restores_saved_accent_dark_and_density()
    {
        // Seed the saved value so the "restore" branch parses accent|dark|density.
        JSInterop.Setup<string?>("omniBlazor.storageGet", "omni.theme")
                 .SetResult("emerald|1|spacious");

        var svc = NewService();
        await svc.InitializeAsync();

        Assert.True(svc.IsInitialized);
        Assert.True(svc.IsUserPicked); // a non-empty saved value means the user picked
        Assert.Equal("emerald", svc.Accent);
        Assert.True(svc.Dark);
        Assert.Equal(LayoutDensity.Spacious, svc.Density);
    }

    [Fact]
    public async Task InitializeAsync_legacy_two_part_value_defaults_density_to_comfortable()
    {
        // Migration-safe: an old 2-part value has no density -> Comfortable default.
        JSInterop.Setup<string?>("omniBlazor.storageGet", "omni.theme")
                 .SetResult("crimson|0");

        var svc = NewService();
        await svc.InitializeAsync();

        Assert.Equal("crimson", svc.Accent);
        Assert.False(svc.Dark);
        Assert.Equal(LayoutDensity.Comfortable, svc.Density);
    }

    [Fact]
    public async Task SetAccentAsync_updates_state_marks_user_picked_and_fires_change()
    {
        var svc = NewService();
        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.SetAccentAsync("violet");

        Assert.Equal("violet", svc.Accent);
        Assert.True(svc.IsUserPicked);
        Assert.Equal(1, changes);
        // The DOM write goes through setAttr; verify the invocation happened.
        JSInterop.VerifyInvoke("omniBlazor.setAttr");
    }

    [Fact]
    public async Task SetDarkAsync_updates_state_and_persists()
    {
        var svc = NewService();
        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.SetDarkAsync(true);

        Assert.True(svc.Dark);
        Assert.True(svc.IsUserPicked);
        Assert.Equal(1, changes);
        JSInterop.VerifyInvoke("omniBlazor.storageSet"); // PersistAsync ran
    }

    [Fact]
    public async Task ToggleDarkAsync_flips_dark_each_call()
    {
        var svc = NewService();

        await svc.ToggleDarkAsync();
        Assert.True(svc.Dark);

        await svc.ToggleDarkAsync();
        Assert.False(svc.Dark);
    }

    [Fact]
    public async Task SetDensityAsync_updates_density_marks_user_picked_and_fires_change()
    {
        var svc = NewService();
        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.SetDensityAsync(LayoutDensity.Compact);

        Assert.Equal(LayoutDensity.Compact, svc.Density);
        Assert.True(svc.IsUserPicked);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task UseSystemColorSchemeAsync_clears_user_picked_and_fires_change()
    {
        var svc = NewService();
        await svc.SetDarkAsync(true); // becomes user-picked first
        Assert.True(svc.IsUserPicked);

        var changesAfter = 0;
        svc.OnChange += () => changesAfter++;

        await svc.UseSystemColorSchemeAsync();

        Assert.False(svc.IsUserPicked); // reverted to following the OS
        // prefersColorSchemeDark is a loose no-op returning false.
        Assert.False(svc.Dark);
        Assert.Equal(1, changesAfter);
        JSInterop.VerifyInvoke("omniBlazor.storageRemove"); // cleared the key
    }

    [Fact]
    public async Task OnColorSchemeChanged_is_ignored_after_user_picked()
    {
        var svc = NewService();
        await svc.SetDarkAsync(false); // user picked light
        Assert.True(svc.IsUserPicked);

        var changes = 0;
        svc.OnChange += () => changes++;

        // OS reports dark, but the user's explicit pick wins -> no-op.
        await svc.OnColorSchemeChanged(true);

        Assert.False(svc.Dark);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task OnColorSchemeChanged_applies_os_value_when_following_system()
    {
        var svc = NewService();
        await svc.InitializeAsync(); // empty storage -> following the OS (not user-picked)
        Assert.False(svc.IsUserPicked);

        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.OnColorSchemeChanged(true); // OS switched to dark

        Assert.True(svc.Dark);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task OnColorSchemeChanged_no_change_when_value_matches_current()
    {
        var svc = NewService(); // Dark is already false, not user-picked
        var changes = 0;
        svc.OnChange += () => changes++;

        await svc.OnColorSchemeChanged(false); // same as current -> guarded no-op

        Assert.False(svc.Dark);
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task DisposeAsync_is_idempotent_and_stops_further_change_from_os()
    {
        var svc = NewService();
        await svc.InitializeAsync(); // following the OS

        await svc.DisposeAsync();
        await svc.DisposeAsync(); // second dispose is a no-op guard

        var changes = 0;
        svc.OnChange += () => changes++;

        // After dispose, the OS handler bails out (_disposed guard).
        await svc.OnColorSchemeChanged(true);

        Assert.False(svc.Dark);
        Assert.Equal(0, changes);
    }
}
