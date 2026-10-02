using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace StyleBro.Tests;

internal static class Verifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static Task VerifyNoDiagnosticsAsync(string source, string? editorConfig = null) =>
        VerifyFixAsync(source, source, editorConfig);

    /// <summary>
    /// Verifies diagnostics in <paramref name="source"/> (markup: {|BROxxxx:text|}), the single-fix result,
    /// and the Fix All result, which is the path 'dotnet format' takes. The framework also re-runs the
    /// analyzer on the fixed code, so a fix that is not idempotent fails here. <paramref name="batchFixedSource"/>
    /// is the Fix All result when it differs from applying the fixes one at a time.
    /// </summary>
    public static Task VerifyFixAsync(string source, string fixedSource, string? editorConfig = null, string? batchFixedSource = null)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        if (batchFixedSource is not null)
        {
            test.BatchFixedCode = batchFixedSource;
        }

        if (editorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        }

        return test.RunAsync();
    }

    /// <summary>Like <see cref="VerifyFixAsync(string, string, string?, string?)"/>, for several files.</summary>
    public static Task VerifyFixAsync(string[] sources, string[] fixedSources, string? editorConfig = null)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>();
        if (editorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        }

        foreach (var source in sources)
        {
            test.TestState.Sources.Add(source);
        }

        foreach (var source in fixedSources)
        {
            test.FixedState.Sources.Add(source);
        }

        return test.RunAsync();
    }

    /// <summary>The diagnostics in <paramref name="sources"/> are reported, but the fix deliberately leaves them.</summary>
    public static Task VerifyNotFixedAsync(string[] sources)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            NumberOfIncrementalIterations = 1,
            NumberOfFixAllIterations = 1,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck,
        };
        foreach (var source in sources)
        {
            test.TestState.Sources.Add(source);
            test.FixedState.Sources.Add(source);
        }

        return test.RunAsync();
    }
}
