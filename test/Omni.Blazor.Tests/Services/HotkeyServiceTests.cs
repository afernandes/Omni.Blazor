namespace Omni.Blazor.Tests.Services;

/// <summary>
/// Behavioural coverage for <see cref="HotkeyService"/>. Under bUnit Loose mode
/// the JS calls (omniBlazor.registerHotkey / unregisterHotkey / setHotkeyDisabled)
/// are no-ops returning default, so these tests assert observable in-memory state
/// (RegistrationCount), the JSInvokable dispatch to the registered handler,
/// disposal semantics, guard clauses, and — where the behaviour IS the JS call —
/// the invocation itself via JSInterop.VerifyInvoke.
/// </summary>
public class HotkeyServiceTests : TestContextBase
{
    private HotkeyService NewService() => new(JSInterop.JSRuntime);

    private static Func<KeyboardEventArgs, Task> NoOpHandler => _ => Task.CompletedTask;

    [Fact]
    public void New_service_has_no_registrations()
    {
        var svc = NewService();
        Assert.Equal(0, svc.RegistrationCount);
    }

    [Fact]
    public async Task RegisterAsync_string_increments_registration_count()
    {
        var svc = NewService();
        await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        Assert.Equal(1, svc.RegistrationCount);
    }

    [Fact]
    public async Task RegisterAsync_invokes_registerHotkey_js()
    {
        var svc = NewService();
        await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        JSInterop.VerifyInvoke("omniBlazor.registerHotkey");
    }

    [Fact]
    public async Task RegisterAsync_combo_overload_registers()
    {
        var svc = NewService();
        await svc.RegisterAsync(new HotkeyCombo("K", Modifier.Ctrl), NoOpHandler);
        Assert.Equal(1, svc.RegistrationCount);
    }

    [Fact]
    public async Task RegisterAsync_unparseable_spec_throws_and_registers_nothing()
    {
        var svc = NewService();
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.RegisterAsync("+", NoOpHandler));
        Assert.Equal(0, svc.RegistrationCount);
    }

    [Fact]
    public async Task RegisterAsync_empty_combo_collection_throws()
    {
        var svc = NewService();
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.RegisterAsync(Array.Empty<HotkeyCombo>(), NoOpHandler));
        Assert.Equal(0, svc.RegistrationCount);
    }

    [Fact]
    public async Task RegisterAsync_null_handler_throws()
    {
        var svc = NewService();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => svc.RegisterAsync("Ctrl+K", null!));
    }

    [Fact]
    public async Task OnHotkeyAsync_dispatches_to_registered_handler()
    {
        var svc = NewService();
        var fired = 0;
        // The handle Id is opaque; register then drive dispatch via the same id.
        // We capture the id by registering and reading through a second handler
        // that records the received args, using the known register/dispatch path.
        await svc.RegisterAsync("Ctrl+K", _ => { fired++; return Task.CompletedTask; });

        // The registration id is generated internally; use VerifyInvoke to read it
        // from the registerHotkey call arguments (first arg is the id).
        var inv = JSInterop.VerifyInvoke("omniBlazor.registerHotkey");
        var id = (string)inv.Arguments[0]!;

        await svc.OnHotkeyAsync(id, new KeyboardEventArgs { Key = "k" });
        Assert.Equal(1, fired);
    }

    [Fact]
    public async Task OnHotkeyAsync_unknown_id_is_a_noop()
    {
        var svc = NewService();
        var fired = 0;
        await svc.RegisterAsync("Ctrl+K", _ => { fired++; return Task.CompletedTask; });
        await svc.OnHotkeyAsync("hk-does-not-exist", new KeyboardEventArgs { Key = "k" });
        Assert.Equal(0, fired);
    }

    [Fact]
    public async Task OnHotkeyAsync_swallows_handler_exceptions()
    {
        var svc = NewService();
        await svc.RegisterAsync("Ctrl+K", _ => throw new InvalidOperationException("boom"));
        var inv = JSInterop.VerifyInvoke("omniBlazor.registerHotkey");
        var id = (string)inv.Arguments[0]!;

        // Dispatch must not surface the handler's exception.
        await svc.OnHotkeyAsync(id, new KeyboardEventArgs { Key = "k" });
    }

    [Fact]
    public async Task Disposing_handle_removes_the_registration()
    {
        var svc = NewService();
        var handle = await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        Assert.Equal(1, svc.RegistrationCount);

        await handle.DisposeAsync();
        Assert.Equal(0, svc.RegistrationCount);
    }

    [Fact]
    public async Task Disposing_handle_twice_is_idempotent()
    {
        var svc = NewService();
        var handle = await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        await handle.DisposeAsync();
        await handle.DisposeAsync(); // must not throw
        Assert.Equal(0, svc.RegistrationCount);
    }

    [Fact]
    public async Task DisposeAsync_clears_all_registrations()
    {
        var svc = NewService();
        await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        await svc.RegisterAsync("Ctrl+P", NoOpHandler);
        Assert.Equal(2, svc.RegistrationCount);

        await svc.DisposeAsync();
        Assert.Equal(0, svc.RegistrationCount);
    }

    [Fact]
    public async Task RegisterAsync_after_dispose_throws_ObjectDisposedException()
    {
        var svc = NewService();
        await svc.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => svc.RegisterAsync("Ctrl+K", NoOpHandler));
    }

    [Fact]
    public async Task SetDisabledAsync_invokes_setHotkeyDisabled_js()
    {
        var svc = NewService();
        var handle = await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        await svc.SetDisabledAsync(handle, true);

        var inv = JSInterop.VerifyInvoke("omniBlazor.setHotkeyDisabled");
        Assert.Equal(true, inv.Arguments[1]);
    }

    [Fact]
    public async Task SetDisabledAsync_on_disposed_handle_is_a_noop()
    {
        var svc = NewService();
        var handle = await svc.RegisterAsync("Ctrl+K", NoOpHandler);
        await handle.DisposeAsync();

        // Handle is gone from the dictionary; must not throw and must not call JS.
        await svc.SetDisabledAsync(handle, true);
        Assert.Equal(0, JSInterop.Invocations.Count(i => i.Identifier == "omniBlazor.setHotkeyDisabled"));
    }
}
