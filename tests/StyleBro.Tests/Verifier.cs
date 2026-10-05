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

        return RunAsync(test, editorConfig);
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

        return RunAsync(test, editorConfig);
    }

    /// <summary>The diagnostics in <paramref name="sources"/> are reported, but the fix deliberately leaves them.</summary>
    public static Task VerifyNotFixedAsync(string[] sources, string? editorConfig = null)
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

        if (editorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        }

        return RunAsync(test, editorConfig);
    }

    /// <summary>Like <see cref="VerifyNotFixedAsync(string[], string?)"/>, for files with their names ('/0/Page.g.cs').</summary>
    public static Task VerifyNotFixedAsync(params (string Name, string Text)[] sources)
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

        return RunAsync(test, null);
    }

    /// <summary>No diagnostics in files with their names ('/0/Page.g.cs').</summary>
    public static Task VerifyNoDiagnosticsAsync(params (string Name, string Text)[] sources)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>();
        foreach (var source in sources)
        {
            test.TestState.Sources.Add(source);
        }

        return RunAsync(test, null);
    }

    /// <summary>
    /// The framework turns on every supported diagnostic; rules that are off by default (BRO1310) stay off here unless the
    /// test's .editorconfig turns them on, like in a build.
    /// </summary>
    private static Task RunAsync(CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> test, string? editorConfig)
    {
        var off = new TAnalyzer().SupportedDiagnostics.Where(d => !d.IsEnabledByDefault).Select(d => d.Id)
            .Where(id => editorConfig?.Contains($"dotnet_diagnostic.{id}.severity", StringComparison.Ordinal) != true).ToList();
        if (off.Count > 0)
        {
            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var options = solution.GetProject(projectId)!.CompilationOptions!;
                return solution.WithProjectCompilationOptions(projectId, options.WithSpecificDiagnosticOptions(
                    options.SpecificDiagnosticOptions.SetItems(off.Select(id => KeyValuePair.Create(id, Microsoft.CodeAnalysis.ReportDiagnostic.Suppress)))));
            });
        }

        return test.RunAsync();
    }
}
