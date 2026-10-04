using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.ConditionalLayoutAnalyzer, StyleBro.CodeFixes.Layout.ConditionalLayoutCodeFixProvider>;

namespace StyleBro.Tests;

public class ConditionalLayoutTests
{
    [Fact]
    public Task SplitConditionals_GetEveryPartOnItsOwnLine() => VerifyFixAsync(
        """
        public class C
        {
            public string M(bool a, string s, string t)
            {
                var x = a {|BRO1524:?|} s
                    : t;
                var y = a
                    ? s {|BRO1524::|} t;
                return y;
            }
        }
        """,
        """
        public class C
        {
            public string M(bool a, string s, string t)
            {
                var x = a
                    ? s
                    : t;
                var y = a
                    ? s
                    : t;
                return y;
            }
        }
        """);

    [Fact]
    public Task OnlyTheMissingLineBreakIsReported() => VerifyFixAsync(
        """
        public class C
        {
            public string M(bool a, string s, string t) =>
                a {|BRO1524:?|} s :
                    t;
        }
        """,
        """
        public class C
        {
            public string M(bool a, string s, string t) =>
                a
                    ? s :
                    t;
        }
        """);

    [Fact]
    public Task EndOfLine_FromTheSdkKey() => VerifyFixAsync(
        """
        public class C
        {
            public string M(bool a, string s, string t)
            {
                var x = a ?
                    s {|BRO1524::|} t;
                return a {|BRO1524:?|} s :
                    t;
            }
        }
        """,
        """
        public class C
        {
            public string M(bool a, string s, string t)
            {
                var x = a ?
                    s :
                    t;
                return a ?
                    s :
                    t;
            }
        }
        """,
        "dotnet_style_operator_placement_when_wrapping = end_of_line\n");

    [Fact]
    public Task KeepsTheLineEndingsAndTabs() => VerifyFixAsync(
        "public class C\r\n{\r\n\tpublic int M(bool a) => a\r\n\t\t? 1 {|BRO1524::|} 2;\r\n}\r\n",
        "public class C\r\n{\r\n\tpublic int M(bool a) => a\r\n\t\t? 1\r\n\t\t: 2;\r\n}\r\n");

    [Fact]
    public Task NestedConditionals_AreCheckedOnTheirOwn() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool a, bool b) =>
                a ? (b {|BRO1524:?|} 1
                    : 2)
                    : 3;
        }
        """,
        """
        public class C
        {
            public int M(bool a, bool b) =>
                a ? (b
                    ? 1
                    : 2)
                    : 3;
        }
        """);

    [Fact]
    public Task Skipped() => VerifyNoDiagnosticsAsync(
        """
        using System;

        public class C
        {
            public object M(bool a, bool b, int x)
            {
                // On one line, also when a part spans lines.
                var one = a ? 1 : 2;
                Func<int> f = a ? () =>
                {
                    return 1;
                } : () => 2;
                var wrappedPart = a ? 1 : x
                    + 2;

                // Already split.
                var split = a
                    ? 1
                    : 2;

                // A comment or directive next to '?' or ':'.
                var comment = a ? 1 // one
                    : 2;
                var directive = a ? 1
        #if DEBUG
                    : 2;
        #else
                    : 3;
        #endif

                // Chains.
                var chain = a ? 1 :
                    b ? 2 :
                    3;
                var chain2 = a ? 1
                    : b ? 2
                    : 3;

                // The part that would move spans lines.
                var multi = a
                    ? 1 : Math.Max(
                        1,
                        2);
                return one;
            }
        }
        """);

    [Fact]
    public Task SyntaxErrors_AreSkipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(bool a) => a ? 1
                : {|CS1525:;|}
        }
        """);
}
