using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public class OmniFileManagerTests : TestContextBase
{
    [Fact]
    public void Loads_bounded_items_and_forwards_cross_cutting_attributes()
    {
        var provider = new FakeProvider();
        var cut = Render<OmniFileManager>(parameters => parameters
            .Add(component => component.Provider, provider)
            .Add(component => component.MaxItems, 2)
            .Add(component => component.Class, "custom-manager")
            .Add(component => component.Style, "min-height: 20rem")
            .AddUnmatched("data-testid", "files"));

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find("section.omni-file-manager");
            Assert.Contains("custom-manager", root.ClassName);
            Assert.Equal("min-height: 20rem", root.GetAttribute("style"));
            Assert.Equal("files", root.GetAttribute("data-testid"));
            Assert.Equal(2, cut.FindAll(".omni-file-manager-item").Count);
        });
        Assert.Equal(2, provider.LastRequest?.Take);
    }

    [Fact]
    public void Selects_entry_and_opens_directory_on_double_click()
    {
        FileManagerEntry? selected = null;
        string? path = null;
        var provider = new FakeProvider();
        var cut = Render<OmniFileManager>(parameters => parameters
            .Add(component => component.Provider, provider)
            .Add(component => component.SelectedItemChanged,
                EventCallback.Factory.Create<FileManagerEntry?>(this, item => selected = item))
            .Add(component => component.PathChanged,
                EventCallback.Factory.Create<string>(this, value => path = value)));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".omni-file-manager-item")));
        var directory = cut.FindAll(".omni-file-manager-item")[0];
        directory.Click();
        Assert.Equal("docs", selected?.Id);

        cut.FindAll(".omni-file-manager-item")[0].DoubleClick();
        cut.WaitForAssertion(() => Assert.Equal("/docs", path));
    }

    [Fact]
    public void Create_folder_uses_provider_and_refreshes_listing()
    {
        var provider = new FakeProvider();
        var cut = Render<OmniFileManager>(parameters => parameters
            .Add(component => component.Provider, provider)
            .Add(component => component.Capabilities, FileManagerCapabilities.CreateFolder));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".omni-file-manager-item")));
        cut.FindAll(".omni-file-manager-tool")[0].Click();
        cut.Find(".omni-file-manager-editor-input").Input("Contratos");
        cut.Find(".omni-file-manager-editor button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, provider.CreateCount);
            Assert.Contains(cut.Instance.Items, item => item.Name == "Contratos");
        });
    }

    [Fact]
    public void Delete_requires_explicit_confirmation()
    {
        var provider = new FakeProvider();
        var cut = Render<OmniFileManager>(parameters => parameters
            .Add(component => component.Provider, provider)
            .Add(component => component.Capabilities, FileManagerCapabilities.Delete));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".omni-file-manager-item")));
        cut.FindAll(".omni-file-manager-item")[1].Click();
        cut.Find(".omni-file-manager-tool-danger").Click();

        Assert.Equal(0, provider.DeleteCount);
        Assert.NotNull(cut.Find(".omni-file-manager-confirm"));

        cut.Find(".omni-file-manager-confirm .omni-file-manager-tool-danger").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, provider.DeleteCount));
    }

    [Fact]
    public void Disposal_cancels_in_flight_provider_load()
    {
        var provider = new CancellingProvider();
        var cut = Render<OmniFileManager>(parameters => parameters
            .Add(component => component.Provider, provider));

        cut.WaitForAssertion(() => Assert.True(provider.Started));
        cut.Instance.Dispose();

        Assert.True(provider.Token.IsCancellationRequested);
    }

    [Fact]
    public void Changing_item_limit_reloads_provider()
    {
        var provider = new FakeProvider();
        var cut = Render<OmniFileManager>(parameters => parameters
            .Add(component => component.Provider, provider)
            .Add(component => component.MaxItems, 3));
        cut.WaitForAssertion(() => Assert.Equal(3, provider.LastRequest?.Take));

        cut.Render(parameters => parameters
            .Add(component => component.Provider, provider)
            .Add(component => component.MaxItems, 1));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, provider.LastRequest?.Take);
            Assert.Single(cut.Instance.Items);
        });
    }

    private sealed class FakeProvider : IOmniFileManagerProvider
    {
        private readonly List<FileManagerEntry> _root =
        [
            new("docs", "Documentos", "/docs", true),
            new("readme", "Leia-me.txt", "/Leia-me.txt", false) { Size = 2048 },
            new("logo", "Logo.png", "/Logo.png", false) { Size = 4096, ContentType = "image/png" }
        ];

        public FileManagerRequest? LastRequest { get; private set; }
        public int CreateCount { get; private set; }
        public int DeleteCount { get; private set; }

        public ValueTask<FileManagerPage> GetItemsAsync(
            FileManagerRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            IReadOnlyList<FileManagerEntry> source = request.Path == "/docs"
                ? [new("contract", "Contrato.pdf", "/docs/Contrato.pdf", false) { Size = 8192 }]
                : _root.ToArray();
            return ValueTask.FromResult(new FileManagerPage(source, source.Count));
        }

        public ValueTask CreateFolderAsync(string parentPath, string name, CancellationToken cancellationToken)
        {
            CreateCount++;
            _root.Add(new(Guid.NewGuid().ToString("N"), name, $"/{name}", true));
            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteAsync(FileManagerEntry entry, CancellationToken cancellationToken)
        {
            DeleteCount++;
            _root.RemoveAll(item => item.Id == entry.Id);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CancellingProvider : IOmniFileManagerProvider
    {
        public bool Started { get; private set; }
        public CancellationToken Token { get; private set; }

        public async ValueTask<FileManagerPage> GetItemsAsync(
            FileManagerRequest request,
            CancellationToken cancellationToken)
        {
            Started = true;
            Token = cancellationToken;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new FileManagerPage([], 0);
        }
    }
}
