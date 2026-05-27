global using Bunit;
global using Xunit;
// Disambiguate xUnit v3's TestContext vs Bunit.TestContext when tests need
// either symbol. Tests inherit TestContextBase (which derives from Bunit's)
// so they rarely refer to either directly.
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Omni.Blazor;
global using Omni.Blazor.Components;
global using Omni.Blazor.Models;
global using Omni.Blazor.Services;
global using Omni.Blazor.Utilities;
global using Omni.Blazor.State;
