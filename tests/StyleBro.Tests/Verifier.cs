using StyleBro.Analyzers.Ordering;
using StyleBro.CodeFixes.Ordering;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace StyleBro.Tests;

internal static class MemberOrderingVerifier
{
    public static Task VerifyNoDiagnosticsAsync(string source, string? editorConfig = null) =>
        VerifyFixAsync(source, source, editorConfig);

    /// <summary>
    /// Verifies diagnostics in <paramref name="source"/> (markup: {|BRO1001:name|}), the single-fix result,
    /// and the Fix All result, which is the path 'dotnet format' takes. The framework also re-runs the
    /// analyzer on the fixed code, so a fix that is not idempotent fails here.
    /// </summary>
    public static Task VerifyFixAsync(string source, string fixedSource, string? editorConfig = null)
    {
        var test = new CSharpCodeFixTest<MemberOrderingAnalyzer, MemberOrderingCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };

        if (editorConfig is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        }

        return test.RunAsync();
    }
}
