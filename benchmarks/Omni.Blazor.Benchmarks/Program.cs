using BenchmarkDotNet.Running;

namespace Omni.Blazor.Benchmarks;

/// <summary>
/// dotnet run -c Release --project benchmarks/Omni.Blazor.Benchmarks
///   (no args)          → pick a suite interactively
///   -- --filter "*Css*" → run one suite
///   -- --filter "*"     → run everything
///
/// Quote the pattern: an unquoted <c>*</c> is glob-expanded by the shell before
/// BenchmarkDotNet ever sees it.
///
/// Namespaced deliberately: the project references Omni.Blazor.ManifestGen, whose
/// own top-level Program would otherwise collide with this one (CS0436).
/// </summary>
internal static class BenchmarkProgram
{
    private static void Main(string[] args)
        => BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly).Run(args);
}
