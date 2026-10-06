using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Readability;

namespace StyleBro.Tests;

public class NullForgivingTests
{
    [Fact]
    public Task NotNullOperands_LoseTheirBang() => VerifyAsync(
        """
        using System;

        public class C
        {
            private string name = "";

            public int M(string s, string? t, int i, int? n)
            {
                if (t == null)
                {
                    return s{|BRO1147:!|}.Length + this.name{|BRO1147:!|}.Length;
                }

                n = 1;
                var u = t{|BRO1147:!|};
                return u.Length + i{|BRO1147:!|} + n{|BRO1147:!|}.Value + "x"{|BRO1147:!|}.Length + Math.Abs(i){|BRO1147:!|};
            }
        }
        """,
        """
        using System;

        public class C
        {
            private string name = "";

            public int M(string s, string? t, int i, int? n)
            {
                if (t == null)
                {
                    return s.Length + this.name.Length;
                }

                n = 1;
                var u = t;
                return u.Length + i + n.Value + "x".Length + Math.Abs(i);
            }
        }
        """);

    [Fact]
    public Task LambdasLocalFunctionsConditionsAndInitializers() => VerifyAsync(
        """
        using System;

        public class C
        {
            private static readonly string Name = "a";

            private readonly int length = Name{|BRO1147:!|}.Length;

            public string P { get; } = Name{|BRO1147:!|};

            public Func<string, int> M(string? t)
            {
                if (t != null && t{|BRO1147:!|}.Length > 0)
                {
                    return s => s{|BRO1147:!|}.Length + L(t);
                }

                return s =>
                {
                    var u = s{|BRO1147:!|};
                    return u.Length + this.length;
                };

                static int L(string v) => v{|BRO1147:!|}.Length;
            }
        }
        """,
        """
        using System;

        public class C
        {
            private static readonly string Name = "a";

            private readonly int length = Name.Length;

            public string P { get; } = Name;

            public Func<string, int> M(string? t)
            {
                if (t != null && t.Length > 0)
                {
                    return s => s.Length + L(t);
                }

                return s =>
                {
                    var u = s;
                    return u.Length + this.length;
                };

                static int L(string v) => v.Length;
            }
        }
        """);

    [Fact]
    public Task RefAndOutArguments_AreLeftAlone() => VerifyAsync(
        """
        public class C
        {
            public void M()
            {
                string? s = "a";
                N(ref s!);
                O(out s!);
            }

            private static void N(ref string x) => x = "b";

            private static void O(out string x) => x = "b";
        }
        """);

    [Fact]
    public Task NeededBangs_AreLeftAlone() => VerifyAsync(
        """
        public class C
        {
            public string F = null!;

            public string? G { get; set; }

            public int M(string? t) => t!.Length + G!.Length;

            public string N() => default!;

            public string O() => default(string)!;

            public int P() => default!;

            public int Q() => default(int)!;
        }
        """);

    // The '!' also hides nested nullability mismatches, which the operand's own flow state doesn't show.
    [Fact]
    public Task TypeArgumentsArraysTuplesAndTypeParameters_AreLeftAlone() => VerifyAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public List<string> M(List<string?> list) => list!;

            public string[] N(string?[] array) => array!;

            public (string, int) O((string?, int) pair) => pair!;

            public T P<T>(T value)
                where T : class
            {
                return value!;
            }
        }
        """);

    [Fact]
    public Task TypesNestedInGenericTypes_AreLeftAlone() => VerifyAsync(
        """
        public class Outer<T>
        {
            public class Inner
            {
            }

            public static Outer<string>.Inner M(Outer<string?>.Inner inner) => inner!;
        }
        """);

    [Fact]
    public Task AFileThatEnablesNullableWarningsInAnAnnotationsProject_IsLeftAlone() => VerifyAsync(
        """
        #nullable enable warnings
        public class C
        {
            public int M(string s) => s!.Length;
        }
        """,
        nullable: NullableContextOptions.Annotations);

    [Fact]
    public Task ANestedPlainValueType_IsFine() => VerifyAsync(
        """
        public class C
        {
            public int M(int? n) => n is null ? 0 : n{|BRO1147:!|}.Value;
        }
        """,
        """
        public class C
        {
            public int M(int? n) => n is null ? 0 : n.Value;
        }
        """);

    [Fact]
    public Task CommentsAroundTheBang_AreLeftAlone() => VerifyAsync(
        """
        public class C
        {
            public int M(string s) => s /* known */!.Length + s
                !.Length;
        }
        """);

    [Fact]
    public Task NullableRegionsThatDifferFromTheProject_AreLeftAlone() => VerifyAsync(
        """
        public class C
        {
        #nullable disable annotations
            public int M(string s) => s!.Length;
        #nullable restore

        #nullable enable
            public int N(string s) => s{|BRO1147:!|}.Length;
        }
        """,
        """
        public class C
        {
        #nullable disable annotations
            public int M(string s) => s!.Length;
        #nullable restore

        #nullable enable
            public int N(string s) => s.Length;
        }
        """);

    [Fact]
    public Task AFileThatEnablesNullableInADisabledProject_IsLeftAlone() => VerifyAsync(
        """
        #nullable enable
        public class C
        {
            public int M(string s) => s!.Length;
        }
        """,
        nullable: NullableContextOptions.Disable);

    [Fact]
    public Task AnnotationsOnly_IsLeftAlone() => VerifyAsync(
        """
        public class C
        {
            public int M(string s) => s!.Length;
        }
        """,
        nullable: NullableContextOptions.Annotations);

    [Theory]
    [InlineData("net8.0,netstandard2.0", false)]
    [InlineData("net8.0", true)]
    public Task SeveralTargetFrameworks_AreLeftAlone(string frameworks, bool reported) => VerifyAsync(
        reported ? "public class C { public int M(string s) => s{|BRO1147:!|}.Length; }" : "public class C { public int M(string s) => s!.Length; }",
        reported ? "public class C { public int M(string s) => s.Length; }" : null,
        frameworks: frameworks);

    private static Task VerifyAsync(string source, string? fixedSource = null, NullableContextOptions nullable = NullableContextOptions.Enable, string? frameworks = null)
    {
        var test = new CSharpCodeFixTest<NullForgivingAnalyzer, NullForgivingCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource ?? source,

            // A removed '!' that was needed shows up as a nullable warning in the fixed code.
            CompilerDiagnostics = CompilerDiagnostics.Warnings,
        };
        if (frameworks is not null)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", $"is_global = true\nbuild_property.StyleBroTargetFrameworks = {frameworks}\n"));
        }

        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var options = (CSharpCompilationOptions)solution.GetProject(projectId)!.CompilationOptions!;
            return solution.WithProjectCompilationOptions(projectId, options.WithNullableContextOptions(nullable)
                .WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItem("CS1591", ReportDiagnostic.Suppress)));
        });
        return test.RunAsync();
    }
}
