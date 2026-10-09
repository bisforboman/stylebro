using StyleBro.Analyzers.Maintainability;
using StyleBro.CodeFixes.Maintainability;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.ParenthesesAnalyzer, StyleBro.CodeFixes.Maintainability.ParenthesesCodeFixProvider>;

namespace StyleBro.Tests;

public class PatternParenthesesTests
{
    [Fact]
    public Task UnnecessaryPatternParentheses_AreRemoved() => VerifyFixAsync(
        """
        public class C
        {
            public bool M(int x, object o, string s) =>
                x is {|BRO1410:(> 0)|}
                && o is{|BRO1410:(string t)|}
                && s is not {|BRO1405:(null)|}
                && x is {|BRO1410:({|BRO1410:(1 or 2)|})|}
                && x is {|BRO1410:(1 or 2)|} or 3
                && x is not {|BRO1410:(< 0)|} and not 5;

            public string N(int x) => x switch
            {
                {|BRO1410:(> 0)|} => "positive",
                _ => "other",
            };

            public int P(object o)
            {
                switch (o)
                {
                    case {|BRO1410:(int i)|}:
                        return i;
                    default:
                        return 0;
                }
            }
        }
        """,
        """
        public class C
        {
            public bool M(int x, object o, string s) =>
                x is > 0
                && o is string t
                && s is not null
                && x is 1 or 2
                && x is 1 or 2 or 3
                && x is not < 0 and not 5;

            public string N(int x) => x switch
            {
                > 0 => "positive",
                _ => "other",
            };

            public int P(object o)
            {
                switch (o)
                {
                    case int i:
                        return i;
                    default:
                        return 0;
                }
            }
        }
        """);

    [Fact]
    public Task ParenthesesThatChangeTheMeaning_AreKept() => VerifyNoDiagnosticsAsync("""
        public class C
        {
            public bool M(int x) =>
                x is not (1 or 2)
                || x is (1 or 2) and > 0
                || x is 1 or (2 or 3)
                || x is (> 0 and < 5) or 10
                || x is 1 or (> 5 and < 9)
                || x is (/* one */ 1);
        }
        """);

    [Fact]
    public Task ParenthesesAroundAndInsideOr_AreLeftToBro1407() => VerifyFixAsync(
        """
        public class C
        {
            public bool M(int x) => x is 1 or ({|BRO1410:(> 5 and < 9)|});
        }
        """,
        """
        public class C
        {
            public bool M(int x) => x is 1 or (> 5 and < 9);
        }
        """);

    [Fact]
    public Task ParenthesesAroundAndInsideOr_AreRemoved_WhenBro1407IsOff() => VerifyFixAsync(
        """
        public class C
        {
            public bool M(int x) => x is {|BRO1410:(> 0 and < 5)|} or 10 || x is 1 or {|BRO1410:(> 5 and < 9)|};
        }
        """,
        """
        public class C
        {
            public bool M(int x) => x is > 0 and < 5 or 10 || x is 1 or > 5 and < 9;
        }
        """,
        "dotnet_diagnostic.BRO1407.severity = none");

    [Fact]
    public Task ParenthesesAroundAndInsideOr_AreRemoved_WhenTheSdkSettingSaysNever() => VerifyFixAsync(
        """
        public class C
        {
            public bool M(int x) => x is 1 or {|BRO1410:(> 5 and < 9)|};
        }
        """,
        """
        public class C
        {
            public bool M(int x) => x is 1 or > 5 and < 9;
        }
        """,
        "dotnet_style_parentheses_in_other_binary_operators = never_if_unnecessary");

    [Fact]
    public Task FadedParentheses_AreReportedForBothRules() => new Verifier<ParenthesesAnalyzer, ParenthesesCodeFixProvider>.Test
    {
        IncludeFades = true,
        TestCode = """
            public class C
            {
                public bool M(int x) => {|BRO1405:{|BRO1405_p:(|}x is {|BRO1410:{|BRO1410_p:(|}> 0{|BRO1410_p:)|}|}{|BRO1405_p:)|}|};
            }
            """,
        FixedCode = """
            public class C
            {
                public bool M(int x) => x is > 0;
            }
            """,
    }.RunAsync();

    [Fact]
    public Task FadedParentheses_AreNotReported_WhenTheRuleIsOff() => new Verifier<ParenthesesAnalyzer, ParenthesesCodeFixProvider>.Test
    {
        IncludeFades = true,
        TestCode = """
            public class C
            {
                public bool M(int x) => x is (> 0);
            }
            """,
        TestState = { AnalyzerConfigFiles = { ("/.editorconfig", "root = true\n\n[*]\ndotnet_diagnostic.BRO1410.severity = none\n") } },
    }.RunAsync();
}
