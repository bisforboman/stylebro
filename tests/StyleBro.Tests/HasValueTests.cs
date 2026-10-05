using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Readability;
using StyleBro.CodeFixes.Readability;

namespace StyleBro.Tests;

public class HasValueTests
{
    private const string On = "dotnet_diagnostic.BRO1148.severity = warning\n";

    private const string Equality = On + "stylebro_null_check_style = equality_operator\n";

    [Fact]
    public Task PatternMatching_IsTheDefault() => VerifyAsync(
        """
        using System;
        using System.Linq;

        public class C
        {
            public int? P { get; set; }

            public bool M(int? n, DateTime? d, C c)
            {
                if ({|BRO1148:n.HasValue|} && {|BRO1148:!c.P.HasValue|})
                {
                    return {|BRO1148:!d.HasValue|};
                }

                var x = {|BRO1148:this.P.HasValue|} ? F({|BRO1148:n.HasValue|}) : ((int?)2).HasValue == F({|BRO1148:(n ?? P).HasValue|});
                return new[] { n }.Any(a => {|BRO1148:a.HasValue|}) || x;
            }

            private static bool F(bool b) => b;
        }
        """,
        """
        using System;
        using System.Linq;

        public class C
        {
            public int? P { get; set; }

            public bool M(int? n, DateTime? d, C c)
            {
                if (n is not null && c.P is null)
                {
                    return d is null;
                }

                var x = this.P is not null ? F(n is not null) : ((int?)2).HasValue == F((n ?? P) is not null);
                return new[] { n }.Any(a => a is not null) || x;
            }

            private static bool F(bool b) => b;
        }
        """,
        On);

    [Fact]
    public Task EqualityOperator_FromBro1133sOption() => VerifyAsync(
        """
        public class C
        {
            public bool M(int? n, long? l) => {|BRO1148:n.HasValue|} || {|BRO1148:!l.HasValue|};

            public bool G<T>(T? t)
                where T : struct => {|BRO1148:t.HasValue|};
        }
        """,
        """
        public class C
        {
            public bool M(int? n, long? l) => n != null || l == null;

            public bool G<T>(T? t)
                where T : struct => t != null;
        }
        """,
        Equality);

    [Fact]
    public Task LiftedUserDefinedOperators_AreNotIntroduced() => VerifyAsync(
        """
        using System;

        public class C
        {
            public bool M(Guid? g, decimal? d) => g.HasValue && {|BRO1148:!d.HasValue|};
        }
        """,
        """
        using System;

        public class C
        {
            public bool M(Guid? g, decimal? d) => g.HasValue && d == null;
        }
        """,
        Equality);

    [Fact]
    public Task ExpressionTrees_KeepHasValueForPatterns() => VerifyAsync(
        """
        using System;
        using System.Linq.Expressions;

        public class C
        {
            public Expression<Func<int?, bool>> M() => n => n.HasValue;
        }
        """,
        On);

    [Fact]
    public Task ExpressionTrees_AllowTheEqualityOperator() => VerifyAsync(
        """
        using System;
        using System.Linq.Expressions;

        public class C
        {
            public Expression<Func<int?, bool>> M() => n => {|BRO1148:n.HasValue|};
        }
        """,
        """
        using System;
        using System.Linq.Expressions;

        public class C
        {
            public Expression<Func<int?, bool>> M() => n => n != null;
        }
        """,
        Equality);

    [Fact]
    public Task TightParentsNameofAndCommentsAreLeftAlone() => VerifyAsync(
        """
        public class C
        {
            public bool M(int? n, bool b) => b == n.HasValue || !!n.HasValue || n.HasValue.Equals(b) || n /* x */.HasValue
                || n
                    .HasValue || ! n.HasValue || nameof(n.HasValue).Length > 0;
        }
        """,
        On);

    // BRO1405 removes the parentheses, which leaves 'b == n.HasValue': judged there, whichever fix runs first.
    [Fact]
    public Task Parentheses_JudgedWithoutThemWhileBro1405IsOn() => VerifyAsync(
        """
        public class C
        {
            public bool M(int? n, bool b) => b == (n.HasValue) && ({|BRO1148:n.HasValue|});
        }
        """,
        """
        public class C
        {
            public bool M(int? n, bool b) => b == (n.HasValue) && (n is not null);
        }
        """,
        On);

    [Fact]
    public Task Parentheses_ProtectTheCheckWhileBro1405IsOff() => VerifyAsync(
        """
        public class C
        {
            public bool M(int? n, bool b) => b == ({|BRO1148:n.HasValue|});
        }
        """,
        """
        public class C
        {
            public bool M(int? n, bool b) => b == (n is not null);
        }
        """,
        On + "dotnet_diagnostic.BRO1405.severity = none\n");

    [Fact]
    public Task OtherHasValueProperties_AreLeftAlone() => VerifyAsync(
        """
        public class Option
        {
            public bool HasValue { get; set; }

            public bool M(Option o) => o.HasValue;
        }
        """,
        On);

    [Fact]
    public Task IsNot_NeedsCSharp9() => VerifyAsync(
        """
        public class C
        {
            public bool M(int? n) => n.HasValue || {|BRO1148:!n.HasValue|};
        }
        """,
        """
        public class C
        {
            public bool M(int? n) => n.HasValue || n is null;
        }
        """,
        On,
        LanguageVersion.CSharp8);

    [Fact]
    public Task Patterns_NeedCSharp7() => VerifyAsync(
        """
        public class C
        {
            public bool M(int? n) { return !n.HasValue; }
        }
        """,
        On,
        LanguageVersion.CSharp6);

    private static Task VerifyAsync(string source, string editorConfig, LanguageVersion version = LanguageVersion.Latest) =>
        VerifyAsync(source, source, editorConfig, version);

    private static Task VerifyAsync(string source, string fixedSource, string editorConfig, LanguageVersion version = LanguageVersion.Latest)
    {
        var test = new CSharpCodeFixTest<HasValueAnalyzer, HasValueCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\n" + editorConfig));
        test.SolutionTransforms.Add((solution, projectId) =>
            solution.WithProjectParseOptions(projectId, ((CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!).WithLanguageVersion(version)));
        return test.RunAsync();
    }
}
