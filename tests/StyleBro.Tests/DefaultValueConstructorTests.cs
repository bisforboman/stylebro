using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Readability;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.DefaultValueConstructorAnalyzer, StyleBro.CodeFixes.Readability.DefaultValueConstructorCodeFixProvider>;

namespace StyleBro.Tests;

public class DefaultValueConstructorTests
{
    private const string NativeIntegers = """
        using System;

        class C
        {
            void M()
            {
                nint a = {|BRO1104:new nint()|};
                nuint b = {|BRO1104:new nuint()|};
                var c = {|BRO1104:new IntPtr()|};
                var d = {|BRO1104:new UIntPtr()|};
            }
        }
        """;

    [Fact]
    public Task ValueTypes_BecomeDefault() => VerifyFixAsync(
        """
        using System;

        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = {|BRO1104:new int()|};
                var b = {|BRO1104:new DateTime()|};
                var c = {|BRO1104:new Plain()|};
                var d = {|BRO1104:new int?()|};
                Plain e = {|BRO1104:new()|};
                var f = {|BRO1104:new Plain()|}.X;
            }
        }
        """,
        """
        using System;

        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = default(int);
                var b = default(DateTime);
                var c = default(Plain);
                var d = default(int?);
                Plain e = default(Plain);
                var f = default(Plain).X;
            }
        }
        """);

    [Fact]
    public Task WellKnownTypesAndEnums_UseTheirEmptyMember() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }
        enum Flags { A = 1, B = 2 }

        class C
        {
            void M()
            {
                var a = {|BRO1104:new CancellationToken()|};
                var b = {|BRO1104:new Guid()|};
                var c = {|BRO1104:new IntPtr()|};
                var d = {|BRO1104:new Color()|};
                var e = {|BRO1104:new Flags()|};
            }
        }
        """,
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }
        enum Flags { A = 1, B = 2 }

        class C
        {
            void M()
            {
                var a = CancellationToken.None;
                var b = Guid.Empty;
                var c = IntPtr.Zero;
                var d = Color.Red;
                var e = default(Flags);
            }
        }
        """);

    [Fact]
    public Task ParameterDefaults_StayConstant() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }

        class C
        {
            void M(Guid g = {|BRO1104:new Guid()|}, CancellationToken ct = {|BRO1104:new CancellationToken()|}, Color c = {|BRO1104:new Color()|})
            {
            }
        }
        """,
        """
        using System;
        using System.Threading;

        enum Color { Red, Green }

        class C
        {
            void M(Guid g = default(Guid), CancellationToken ct = default(CancellationToken), Color c = Color.Red)
            {
            }
        }
        """);

    [Fact]
    public Task CodeThatDefaultWouldSkip_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        struct WithConstructor
        {
            public int X;
            public WithConstructor() { X = 1; }
        }

        struct Plain { public int X; }

        class C
        {
            void M<T>() where T : struct
            {
                var a = new WithConstructor();
                var b = new T();
                new Plain();
            }
        }
        """);

    [Fact]
    public Task ArgumentsInitializersAndClasses_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        struct Plain { public int X; }

        class C
        {
            void M()
            {
                var a = new Plain { X = 1 };
                var b = new Plain() { };
                var c = new System.DateTime(2000, 1, 1);
                var d = new C();
            }
        }
        """);

    // Like StyleCop master: 'nint.Zero' only compiles with C# 11 on a runtime with numeric IntPtr (.NET 7+); the test
    // framework's default references are .NET Core 3.1.
    [Fact]
    public Task NativeIntegers_WithoutNumericIntPtr_BecomeDefault() => VerifyFixAsync(
        NativeIntegers,
        Fixed("default(nint)", "default(nuint)"));

    [Theory]
    [InlineData(LanguageVersion.CSharp11, "nint.Zero", "nuint.Zero")]
    [InlineData(LanguageVersion.CSharp10, "default(nint)", "default(nuint)")]
    public Task NativeIntegers_OnNet8_DependOnTheLanguageVersion(LanguageVersion version, string nint, string nuint)
    {
        var test = new CSharpCodeFixTest<DefaultValueConstructorAnalyzer, DefaultValueConstructorCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = NativeIntegers,
            FixedCode = Fixed(nint, nuint),
        };
        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(
            projectId,
            ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithLanguageVersion(version)));
        return test.RunAsync();
    }

    private static string Fixed(string nint, string nuint) => NativeIntegers
        .Replace("{|BRO1104:new nint()|}", nint, StringComparison.Ordinal)
        .Replace("{|BRO1104:new nuint()|}", nuint, StringComparison.Ordinal)
        .Replace("{|BRO1104:new IntPtr()|}", "IntPtr.Zero", StringComparison.Ordinal)
        .Replace("{|BRO1104:new UIntPtr()|}", "UIntPtr.Zero", StringComparison.Ordinal);
}
