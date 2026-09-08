using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniDataGridLoadingTests : TestContextBase
{
    private static RenderFragment Columns => b =>
    {
        b.OpenComponent<OmniDataGridColumn<string>>(0);
        b.AddAttribute(1, "Title", "Name");
        b.AddAttribute(2, "PropertyName", "Name");
        b.AddAttribute(3, "TextSelector", (Func<string, string>)(item => item));
        b.CloseComponent();
    };

    [Fact]
    public void Loading_ExternalData_PreservesHeadersAndDoesNotEnumeratePendingData()
    {
        var cut = Render<OmniDataGrid<string>>(p => p
            .Add(c => c.Loading, true).Add(c => c.LoadingSkeleton, true)
            .Add(c => c.Columns, Columns).Add(c => c.Data, PendingData()));
        Assert.Equal("Name", cut.Find("th").TextContent.Trim());
        Assert.NotEmpty(cut.FindAll("td .omni-grid-skeleton-bar"));
        Assert.Equal("true", cut.Find(".omni-grid").GetAttribute("aria-busy"));
        cut.Render(p => p.Add(c => c.Loading, false).Add(c => c.Data, new[] { "Ready" }));
        Assert.Empty(cut.FindAll(".omni-grid-skeleton-bar"));
        Assert.Contains("Ready", cut.Find("tbody").TextContent);
        Assert.Equal("false", cut.Find(".omni-grid").GetAttribute("aria-busy"));
    }

    [Fact]
    public void Loading_FalseWhileProviderPending_RemainsBusyUntilProviderCompletes()
    {
        var completion = new TaskCompletionSource<GridLoadResult<string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = Render<OmniDataGrid<string>>(p => p
            .Add(c => c.Loading, true).Add(c => c.LoadingSkeleton, true)
            .Add(c => c.Columns, Columns)
            .Add(c => c.DataProvider, async (_, ct) => await completion.Task.WaitAsync(ct)));
        cut.Render(p => p.Add(c => c.Loading, false));
        Assert.NotEmpty(cut.FindAll(".omni-grid-skeleton-bar"));
        completion.SetResult(new(new[] { "Loaded" }, 1));
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".omni-grid-skeleton-bar"));
            Assert.Contains("Loaded", cut.Find("tbody").TextContent);
        });
    }

    [Fact]
    public void Loading_CustomTemplate_TakesPrecedenceOverSkeleton()
    {
        var cut = Render<OmniDataGrid<string>>(p => p
            .Add(c => c.Loading, true).Add(c => c.LoadingSkeleton, true)
            .Add(c => c.Columns, Columns)
            .Add(c => c.LoadingTemplate, b => b.AddContent(0, "Pending")));
        Assert.Contains("Pending", cut.Markup);
        Assert.Empty(cut.FindAll(".omni-grid-skeleton-bar"));
        Assert.Empty(cut.FindAll(".omni-grid-empty"));
    }

    [Fact]
    public void Loading_ExternalPending_DisposesWithoutWaitingForApplicationData()
    {
        var cut = Render<OmniDataGrid<string>>(p => p
            .Add(c => c.Loading, true).Add(c => c.LoadingSkeleton, true)
            .Add(c => c.Columns, Columns));
        cut.Instance.Dispose();
        cut.Instance.Dispose();
    }

    private static IEnumerable<string> PendingData()
    {
        yield return "Pending";
        throw new InvalidOperationException("Pending data must not be enumerated.");
    }
}
