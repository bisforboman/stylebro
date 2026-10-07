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

    // 'nint.Zero' only compiles with C# 11 on .NET 7+ (StyleCop master checks for that), but a multi-targeted project
    // analyzes the same file once per target framework, so the fix would differ between the copies. 'default(nint)'
    // everywhere, also where 'nint.Zero' would compile. The test framework's default references are .NET Core 3.1.
    [Fact]
    public Task NativeIntegers_BecomeDefault() => VerifyFixAsync(
        NativeIntegers,
        Fixed("default(nint)", "default(nuint)"));

    [Theory]
    [InlineData(LanguageVersion.CSharp11)]
    [InlineData(LanguageVersion.CSharp10)]
    public Task NativeIntegers_OnNet8_BecomeDefaultToo(LanguageVersion version)
    {
        var test = new CSharpCodeFixTest<DefaultValueConstructorAnalyzer, DefaultValueConstructorCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = NativeIntegers,
            FixedCode = Fixed("default(nint)", "default(nuint)"),
        };
        test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(
            projectId,
            ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithLanguageVersion(version)));
        return test.RunAsync();
    }

    [Fact]
    public Task PreferSimpleDefault_WritesTheLiteralWhereItBindsTheSame() => VerifyFixAsync(
        """
        using System;
        using System.Threading;

        struct Plain { public int X; }

        class C
        {
            TimeSpan field = {|BRO1104:new TimeSpan()|};

            int Property => {|BRO1104:new int()|};

            void M(Plain p = {|BRO1104:new Plain()|}, CancellationToken token = {|BRO1104:new CancellationToken()|})
            {
                DateTime a = {|BRO1104:new DateTime()|};
                a = {|BRO1104:new DateTime()|};
                Take({|BRO1104:new int()|});
                Func<int> f = () => {|BRO1104:new int()|};
                var g = {|BRO1104:new Guid()|};
                Take(token == {|BRO1104:new CancellationToken()|} ? 1 : 2);
                Pair({|BRO1104:new int()|}, {|BRO1104:new long()|});
            }

            static void Take(int x)
            {
            }

            static void Pair(int x, long y)
            {
            }
        }
        """,
        """
        using System;
        using System.Threading;

        struct Plain { public int X; }

        class C
        {
            TimeSpan field = default;

            int Property => default;

            void M(Plain p = default, CancellationToken token = default)
            {
                DateTime a = default;
                a = default;
                Take(default);
                Func<int> f = () => default;
                var g = Guid.Empty;
                Take(token == CancellationToken.None ? 1 : 2);
                Pair(default, default);
            }

            static void Take(int x)
            {
            }

            static void Pair(int x, long y)
            {
            }
        }
        """,
        "csharp_prefer_simple_default_expression = true:warning\n");

    // 'var', a conversion (boxing, nullable), an overload the literal can't pick, a member access on it, and creations
    // or 'default(T)'s that can't all become 'default' in one statement ('Two(default, default)' is ambiguous where
    // each alone isn't) keep 'default(T)'.
    [Fact]
    public Task PreferSimpleDefault_KeepsTheTypeWhereTheLiteralWouldChangeTheMeaning() => VerifyFixAsync(
        """
        class C
        {
            void M()
            {
                var a = {|BRO1104:new int()|};
                object o = {|BRO1104:new int()|};
                int? n = {|BRO1104:new int()|};
                Take({|BRO1104:new int()|});
                Two({|BRO1104:new int()|}, {|BRO1104:new long()|});
                Two(default(int), {|BRO1104:new long()|});
                var s = {|BRO1104:new int()|}.ToString();
            }

            static void Take(int x)
            {
            }

            static void Take(string x)
            {
            }

            static void Two(int x, long y)
            {
            }

            static void Two(string x, string y)
            {
            }
        }
        """,
        """
        class C
        {
            void M()
            {
                var a = default(int);
                object o = default(int);
                int? n = default(int);
                Take(default(int));
                Two(default(int), default(long));
                Two(default(int), default(long));
                var s = default(int).ToString();
            }

            static void Take(int x)
            {
            }

            static void Take(string x)
            {
            }

            static void Two(int x, long y)
            {
            }

            static void Two(string x, string y)
            {
            }
        }
        """,
        "csharp_prefer_simple_default_expression = true\n");

    [Theory]
    [InlineData("csharp_prefer_simple_default_expression = false\n", LanguageVersion.Latest)]
    [InlineData("csharp_prefer_simple_default_expression = true\n", LanguageVersion.CSharp7)]
    public Task PreferSimpleDefault_OffOrBeforeCSharp71_KeepsTheType(string editorConfig, LanguageVersion version)
    {
        var test = new CSharpCodeFixTest<DefaultValueConstructorAnalyzer, DefaultValueConstructorCodeFixProvider, DefaultVerifier>
        {
            TestCode = "class C\n{\n    int field = {|BRO1104:new int()|};\n}\n",
            FixedCode = "class C\n{\n    int field = default(int);\n}\n",
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
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
