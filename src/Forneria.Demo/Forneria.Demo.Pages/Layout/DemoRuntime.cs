namespace Forneria.Demo.Pages.Layout;

internal static class DemoRuntime
{
    /// <summary>
    /// Avoid startup interop that triggers a Mono metadata assertion in browser-wasm Debug.
    /// Release builds and the Server host keep the complete interactive initialization path.
    /// </summary>
    public static bool AvoidWasmDebugStartupInterop
    {
        get
        {
#if DEBUG
            return OperatingSystem.IsBrowser();
#else
            return false;
#endif
        }
    }
}
