using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Readability;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.NullCheckAnalyzer, StyleBro.CodeFixes.Readability.NullCheckCodeFixProvider>;

namespace StyleBro.Tests;

public class NullCheckTests
{
    private const string Pattern = "stylebro_null_check_style = pattern_matching\n";

    private const string Equality = "stylebro_null_check_style = equality_operator\n";

    [Fact]
    public Task EqualityOperator_FromTheOption() => VerifyFixAsync(
        """
        using System;
        using System.Linq;

        public class C
        {
            public bool M<T>(string s, object o, int? n, T t, Func<object, bool> f)
            {
                if ({|BRO1133:s is null|} || {|BRO1133:o is not null|})
                {
                    return {|BRO1133:n is null|};
                }

                var x = {|BRO1133:t is null|} ? f({|BRO1133:o is null|}) : {|BRO1133:s?.Length is not null|};
                return new[] { o }.Any(a => {|BRO1133:a is null|}) & ({|BRO1133:s is null|});
            }
        }
        """,
        """
        using System;
        using System.Linq;

        public class C
        {
            public bool M<T>(string s, object o, int? n, T t, Func<object, bool> f)
            {
                if (s == null || o != null)
                {
                    return n == null;
                }

                var x = t == null ? f(o == null) : s?.Length != null;
                return new[] { o }.Any(a => a == null) & (s == null);
            }
        }
        """,
        Equality);

    [Fact]
    public Task PatternMatching_IsTheDefault() => VerifyFixAsync(
        """
        public class C
        {
            public int? P { get; set; }

            public bool M<T>(string s, object o, int? n, T t, C c)
            {
                if ({|BRO1133:s == null|} || {|BRO1133:o != null|} || {|BRO1133:(object)c == null|})
                {
                    return {|BRO1133:n == null|};
                }

                return {|BRO1133:t != null|} && {|BRO1133:c?.P == null|} && {|BRO1133:this.P != null|} && {|BRO1133:M(s, o, n, t, c).ToString() != null|};
            }
        }
        """,
        """
        public class C
        {
            public int? P { get; set; }

            public bool M<T>(string s, object o, int? n, T t, C c)
            {
                if (s is null || o is not null || (object)c is null)
                {
                    return n is null;
                }

                return t is not null && c?.P is null && this.P is not null && M(s, o, n, t, c).ToString() is not null;
            }
        }
        """);

    [Fact]
    public Task NullOnTheLeft_BecomesAPattern() => VerifyFixAsync(
        """
        public class C
        {
            public bool M(string s, object o) => {|BRO1133:null == s|} && {|BRO1133:null !=
                o|} && null /* kept */ == s;
        }
        """,
        """
        public class C
        {
            public bool M(string s, object o) => s is null && o is not null && null /* kept */ == s;
        }
        """,
        Pattern);

    [Fact]
    public Task UserDefinedOperators_AreLeftAlone() => VerifyNoDiagnosticsAsync(
        """
        public class Money
        {
            public static bool operator ==(Money a, Money b) => true;

            public static bool operator !=(Money a, Money b) => false;

            public override bool Equals(object o) => true;

            public override int GetHashCode() => 0;
        }

        public record Point { }

        public struct S
        {
            public static bool operator ==(S? a, S? b) => true;

            public static bool operator !=(S? a, S? b) => false;

            public override bool Equals(object o) => true;

            public override int GetHashCode() => 0;
        }

        public class C
        {
            public bool M(Money m, Point p, S? s) => m == null || m != null || p == null || s == null;
        }
        """,
        Pattern);

    [Fact]
    public Task UserDefinedOperators_AreLeftAlone_EqualityOperator() => VerifyNoDiagnosticsAsync(
        """
        public class Money
        {
            public static bool operator ==(Money a, Money b) => true;

            public static bool operator !=(Money a, Money b) => false;

            public override bool Equals(object o) => true;

            public override int GetHashCode() => 0;
        }

        public class C
        {
            public bool M(Money m, dynamic d) => m is null || m is not null || d is null;
        }
        """,
        Equality);

    [Fact]
    public Task LiftedOperators_AreFine() => VerifyFixAsync(
        """
        public struct S
        {
            public static bool operator ==(S a, S b) => true;

            public static bool operator !=(S a, S b) => false;

            public override bool Equals(object o) => true;

            public override int GetHashCode() => 0;
        }

        public class C
        {
            public bool M(S? s) => {|BRO1133:s == null|};
        }
        """,
        """
        public struct S
        {
            public static bool operator ==(S a, S b) => true;

            public static bool operator !=(S a, S b) => false;

            public override bool Equals(object o) => true;

            public override int GetHashCode() => 0;
        }

        public class C
        {
            public bool M(S? s) => s is null;
        }
        """,
        Pattern);

    [Fact]
    public Task LiftedOperators_AreNotIntroduced() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            public bool M(Guid? g, int? n) => g is null || g is not null || {|BRO1133:n is null|};
        }
        """,
        """
        using System;

        public class C
        {
            public bool M(Guid? g, int? n) => g is null || g is not null || n == null;
        }
        """,
        Equality);

    [Fact]
    public Task ExpressionTrees_AreLeftAlone() => VerifyFixAsync(
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        public class C
        {
            public object M(IQueryable<string> q, string[] a)
            {
                Expression<Func<string, bool>> e = s => s == null || new[] { s }.Any(x => x != null);
                var fromQueryable = from s in q where s != null select s == null;
                var fromArray = from s in a where {|BRO1133:s != null|} select {|BRO1133:s == null|};
                Func<string, bool> f = s => {|BRO1133:s == null|};
                return e;
            }
        }
        """,
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        public class C
        {
            public object M(IQueryable<string> q, string[] a)
            {
                Expression<Func<string, bool>> e = s => s == null || new[] { s }.Any(x => x != null);
                var fromQueryable = from s in q where s != null select s == null;
                var fromArray = from s in a where s is not null select s is null;
                Func<string, bool> f = s => s is null;
                return e;
            }
        }
        """,
        Pattern);

    [Fact]
    public Task OtherComparisons_AreLeftAlone() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public bool M(string s, string t, object o, dynamic d, int i)
            {
                return s == default || s == default(string) || s + t == null || o as string == null
                    || d == null || i == null || s == t || s is "" || s is { } || s is not (null) || s is null or "";
            }
        }
        """,
        Pattern);

    [Fact]
    public Task LooseOperandsAndParents_AreLeftAlone() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public bool M(bool b, object o) => b == o is null || b != o is not null || o is
                not null || o is /* c */ not null || o is "" || o is { } || o is not (null);
        }
        """,
        Equality);

    [Fact]
    public Task Pointers_AreLeftAlone() => VerifyAsync(
        """
        public unsafe class C
        {
            public bool M(int* p) => p == null;
        }
        """,
        LanguageVersion.Latest,
        Pattern,
        allowUnsafe: true);

    [Fact]
    public Task IsNot_NeedsCSharp9() => VerifyAsync(
        """
        public class C
        {
            public bool M(string s) => {|BRO1133:s == null|} || s != null;
        }
        """,
        LanguageVersion.CSharp8,
        Pattern,
        fixedSource: """
        public class C
        {
            public bool M(string s) => s is null || s != null;
        }
        """);

    [Fact]
    public Task Patterns_NeedCSharp7() => VerifyAsync(
        """
        public class C
        {
            public bool M(string s) => s == null;
        }
        """,
        LanguageVersion.CSharp6,
        Pattern);

    [Fact]
    public Task IsNot_NeedsCSharp9_InEveryCopyOfTheFile()
    {
        // The file is compiled with C# 8 in another project too (LibGit2Sharp: net472's default C# 7.3 in a multi-targeted
        // project). The fix doesn't write 'is not null' there; 'is null' works in both.
        var test = new CSharpCodeFixTest<NullCheckAnalyzer, NullCheckCodeFixProvider, DefaultVerifier>
        {
            NumberOfIncrementalIterations = -3,
            NumberOfFixAllIterations = -3,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck,

            // The framework's '#pragma warning disable' check edits the first project's copy only.
            TestBehaviors = TestBehaviors.SkipSuppressionCheck,
        };
        const string Old = "public class C\n{\n    public bool M(string s) => {|BRO1133:s == null|} || s != null;\n}\n";
        test.TestState.Sources.Add(("/0/Test0.cs", "public class C\n{\n    public bool M(string s) => {|BRO1133:s == null|} || {|BRO1133:s != null|};\n}\n"));
        test.TestState.AdditionalProjects["Old"].Sources.Add(("/0/Test0.cs", Old));
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.FixedState.Sources.Add(("/0/Test0.cs", "public class C\n{\n    public bool M(string s) => s is null || {|BRO1133:s != null|};\n}\n"));
        test.FixedState.AdditionalProjects["Old"].Sources.Add(("/0/Test0.cs", "public class C\n{\n    public bool M(string s) => s is null || s != null;\n}\n"));
        test.SolutionTransforms.Add((solution, _) =>
        {
            var old = solution.Projects.Single(p => p.Name == "Old");
            return solution.WithProjectParseOptions(old.Id, ((CSharpParseOptions)old.ParseOptions!).WithLanguageVersion(LanguageVersion.CSharp8));
        });
        return test.RunAsync();
    }

    private static Task VerifyAsync(string source, LanguageVersion version, string editorConfig, string? fixedSource = null, bool allowUnsafe = false)
    {
        var test = new CSharpCodeFixTest<NullCheckAnalyzer, NullCheckCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource ?? source,
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            solution = solution.WithProjectParseOptions(projectId, ((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(version));
            return solution.WithProjectCompilationOptions(projectId, ((CSharpCompilationOptions)project.CompilationOptions!).WithAllowUnsafe(allowUnsafe));
        });
        return test.RunAsync();
    }
}
