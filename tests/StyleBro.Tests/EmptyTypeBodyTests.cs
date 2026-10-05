using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Readability;
using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmptyTypeBodyAnalyzer, StyleBro.CodeFixes.Readability.EmptyTypeBodyCodeFixProvider>;

namespace StyleBro.Tests;

public class EmptyTypeBodyTests
{
    private const string On = "dotnet_diagnostic.BRO1145.severity = warning\n";

    [Fact]
    public Task EmptyBody_BecomesASemicolon() => Verify.VerifyFixAsync(
        """
        public class Marker {|BRO1145:{ }|}

        public struct Size
        {|BRO1145:{
        }|}

        public interface ITag<T> where T : class {|BRO1145:{ }|};

        public sealed class Point(int x) : Marker, ITag<string> {|BRO1145:{}|}

        public static partial class Outer
        {
            private class Inner {|BRO1145:{ }|}
        }
        """,
        """
        public class Marker;

        public struct Size;

        public interface ITag<T> where T : class;

        public sealed class Point(int x) : Marker, ITag<string>;

        public static partial class Outer
        {
            private class Inner;
        }
        """,
        On);

    [Fact]
    public Task BodiesThatStay_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class A
        {
            public int X { get; set; }
        }

        public class B
        {
            // later
        }

        public class C /* empty */ { }

        public class D
        {
        #if DEBUG
        #endif
        }

        public record E { }

        public enum F { }

        public class G;
        """,
        On);

    [Fact]
    public Task Off_ByDefault() => Verify.VerifyNoDiagnosticsAsync("public class Marker { }");

    [Fact]
    public Task BelowCSharp12_NotReported()
    {
        var test = new CSharpCodeFixTest<EmptyTypeBodyAnalyzer, EmptyTypeBodyCodeFixProvider, DefaultVerifier> { TestCode = "public class Marker { }" };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + On));
        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(
            projectId,
            ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithLanguageVersion(LanguageVersion.CSharp11)));
        return test.RunAsync();
    }

    [Theory]
    [InlineData(null, null, null, true)]
    [InlineData("net10.0", "14.0", "14.0", true)]
    [InlineData("net48", "7.3", "7.3", true)]
    [InlineData("net8.0,net10.0", "12.0", "12.0", true)]
    [InlineData("net48,net10.0", "14.0", "14.0", false)]
    [InlineData("netstandard2.0,net8.0", "12.0", "12.0", false)]
    [InlineData("net48,net10.0", "latest", "14.0", true)]
    [InlineData("net48,net10.0", "12", "14.0", true)]
    [InlineData("net48,net10.0", "", "14.0", false)]
    [InlineData("net48,net10.0", null, null, false)]
    public async Task MultiTargeted_OnlyWhenEveryFrameworkGetsCSharp12(string? frameworks, string? langVersion, string? defaultVersion, bool reported)
    {
        var global = "is_global = true\n";
        global += frameworks is null ? string.Empty : $"build_property.StyleBroTargetFrameworks = {frameworks}\n";
        global += langVersion is null ? string.Empty : $"build_property.LangVersion = {langVersion}\n";
        global += defaultVersion is null ? string.Empty : $"build_property.MaxSupportedLangVersion = {defaultVersion}\n";
        var test = new CSharpCodeFixTest<EmptyTypeBodyAnalyzer, EmptyTypeBodyCodeFixProvider, DefaultVerifier>
        {
            TestCode = reported ? "public class Marker {|BRO1145:{ }|}" : "public class Marker { }",
            FixedCode = reported ? "public class Marker;" : "public class Marker { }",
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", global));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + On));
        await test.RunAsync();
    }
}
