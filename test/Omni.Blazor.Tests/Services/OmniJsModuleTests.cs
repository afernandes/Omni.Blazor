using System.Text.RegularExpressions;
using Microsoft.JSInterop;

namespace Omni.Blazor.Tests.Services;

public sealed class OmniJsModuleTests
{
    [Fact]
    public async Task Each_feature_owns_and_lazily_imports_only_its_module()
    {
        FakeJsRuntime runtime = new();
        OmniCoreJsModule core = new(runtime);
        OmniScrollJsModule scroll = new(runtime);

        await core.InvokeVoidAsync("focusElement", "name");
        await core.InvokeAsync<string?>("storageGet", "theme");
        await scroll.InvokeVoidAsync("scrollTo", 0, 120);

        Assert.Equal(
            [OmniCoreJsModule.Path, OmniScrollJsModule.Path],
            runtime.ImportPaths);

        FakeJsModule coreReference = runtime.Modules[OmniCoreJsModule.Path];
        Assert.Equal(2, coreReference.Invocations.Count);
        Assert.Equal("invoke", coreReference.Invocations[0].Identifier);
        Assert.Equal("focusElement", coreReference.Invocations[0].Arguments[0]);
        Assert.Equal("storageGet", coreReference.Invocations[1].Arguments[0]);

        FakeJsModule scrollReference = runtime.Modules[OmniScrollJsModule.Path];
        Assert.Single(scrollReference.Invocations);
        Assert.Equal("scrollTo", scrollReference.Invocations[0].Arguments[0]);

        await core.DisposeAsync();
        await scroll.DisposeAsync();
        Assert.All(runtime.Modules.Values, module => Assert.Equal(1, module.DisposeCount));
    }

    [Fact]
    public async Task Dispose_cancels_and_drains_in_flight_call_and_is_idempotent()
    {
        FakeJsRuntime runtime = new();
        FakeJsModule reference = runtime.GetModule(OmniCoreJsModule.Path);
        reference.BlockInvocations = true;
        OmniCoreJsModule module = new(runtime);

        Task invocation = module.InvokeVoidAsync("focusElement", "name").AsTask();
        await reference.InvocationStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);

        Task firstDispose = module.DisposeAsync().AsTask();
        Task secondDispose = module.DisposeAsync().AsTask();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation);
        await Task.WhenAll(firstDispose, secondDispose);

        Assert.Equal(1, reference.DisposeCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => module.InvokeVoidAsync("focusElement", "other").AsTask());
    }

    [Fact]
    public void Every_interop_call_targets_the_injected_module_that_implements_it()
    {
        string root = FindRepoRoot();
        string library = Path.Combine(root, "src", "Omni.Blazor");
        Dictionary<string, string> modulePaths = new(StringComparer.Ordinal)
        {
            ["Core"] = OmniCoreJsModule.Path,
            ["Scroll"] = OmniScrollJsModule.Path,
            ["Responsive"] = OmniResponsiveJsModule.Path,
            ["Overlay"] = OmniOverlayJsModule.Path,
            ["Inputs"] = OmniInputsJsModule.Path,
            ["Navigation"] = OmniNavigationJsModule.Path,
            ["Speech"] = OmniSpeechJsModule.Path,
            ["Data"] = OmniDataJsModule.Path,
            ["Display"] = OmniDisplayJsModule.Path
        };
        Regex dependencyPattern = new(
            @"IOmni(?<module>Core|Scroll|Responsive|Overlay|Inputs|Navigation|Speech|Data|Display)JsModule\s+(?<receiver>\w+)",
            RegexOptions.CultureInvariant);
        Regex invocationPattern = new(
            @"(?<receiver>\w+)\.Invoke(?:Void)?Async(?:<[^>]+>)?\(\s*""(?<identifier>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)""",
            RegexOptions.CultureInvariant);

        string[] sourceFiles = Directory
            .EnumerateFiles(library, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        int invocationCount = 0;
        foreach (string sourceFile in sourceFiles)
        {
            string source = File.ReadAllText(sourceFile);
            Dictionary<string, string> dependencies = new(StringComparer.Ordinal);
            foreach (Match dependency in dependencyPattern.Matches(source))
            {
                string receiver = dependency.Groups["receiver"].Value;
                string module = dependency.Groups["module"].Value;
                if (dependencies.TryGetValue(receiver, out string? existingModule))
                    Assert.Equal(existingModule, module);
                else
                    dependencies.Add(receiver, module);
            }

            foreach (Match invocation in invocationPattern.Matches(source))
            {
                string receiver = invocation.Groups["receiver"].Value;
                string identifier = invocation.Groups["identifier"].Value;
                if (!dependencies.TryGetValue(receiver, out string? moduleName))
                    continue;

                invocationCount++;
                string modulePath = modulePaths[moduleName];
                string relativePath = modulePath["./_content/Omni.Blazor/".Length..]
                    .Replace('/', Path.DirectorySeparatorChar);
                string moduleSource = File.ReadAllText(Path.Combine(library, "wwwroot", relativePath));
                string rootIdentifier = identifier.Split('.')[0];
                Assert.Contains($"ns.{rootIdentifier}", moduleSource, StringComparison.Ordinal);
            }
        }

        Assert.True(invocationCount > 50);
        Assert.DoesNotContain(sourceFiles, path => File.ReadAllText(path).Contains("ResolveModulePath", StringComparison.Ordinal));
        Assert.DoesNotContain(
            sourceFiles.Where(path => !path.EndsWith("OmniJsModule.cs", StringComparison.Ordinal)),
            path => File.ReadAllText(path).Contains("\"omniBlazor.", StringComparison.Ordinal));
        Assert.False(File.Exists(Path.Combine(library, "wwwroot", "js", "Omni.js")));
        Assert.True(File.Exists(Path.Combine(library, "wwwroot", "js", "omni-diagram.js")));
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public Dictionary<string, FakeJsModule> Modules { get; } = new(StringComparer.Ordinal);

        public List<string> ImportPaths { get; } = [];

        public FakeJsModule GetModule(string path)
        {
            if (!Modules.TryGetValue(path, out FakeJsModule? module))
            {
                module = new FakeJsModule();
                Modules.Add(path, module);
            }
            return module;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("import", identifier);
            string path = Assert.IsType<string>(Assert.Single(args ?? []));
            ImportPaths.Add(path);
            return ValueTask.FromResult((TValue)(object)GetModule(path));
        }
    }

    private sealed class FakeJsModule : IJSObjectReference
    {
        public List<(string Identifier, object?[] Arguments)> Invocations { get; } = [];

        public TaskCompletionSource InvocationStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AllowInvocation { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockInvocations { get; set; }

        public int DisposeCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Invocations.Add((identifier, args ?? []));
            InvocationStarted.TrySetResult();
            return BlockInvocations
                ? new ValueTask<TValue>(CompleteInvocationAsync<TValue>(cancellationToken))
                : ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }

        private async Task<TValue> CompleteInvocationAsync<TValue>(CancellationToken cancellationToken)
        {
            await AllowInvocation.Task.WaitAsync(cancellationToken);
            return default!;
        }
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Omni.Blazor.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find the Omni.Blazor repository root.");
    }
}
