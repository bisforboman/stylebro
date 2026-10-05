using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ElseIfAnalyzer, StyleBro.CodeFixes.Readability.ElseIfCodeFixProvider>;

namespace StyleBro.Tests;

public class ElseIfTests
{
    private const string NoBraceRules = """
        dotnet_diagnostic.BRO1514.severity = none
        dotnet_diagnostic.BRO1515.severity = none
        dotnet_diagnostic.BRO1516.severity = none
        """;

    [Fact]
    public Task IfOnTheLineAfterElse_IsJoined() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public int M(int x)
            {
                if (x == 1)
                {
                    return 1;
                }
                {|BRO1139:else|}
                    if (x == 2)
                    {
                        return 2;
                    }
                    else
                    {
                        return 3;
                    }
            }
        }
        """,
        """
        public class C
        {
            public int M(int x)
            {
                if (x == 1)
                {
                    return 1;
                }
                else if (x == 2)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }
        }
        """);

    [Fact]
    public Task ElseBlockHoldingOnlyAnIf_BecomesElseIf() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1)
                {
                    M(1);
                }
                {|BRO1139:else|}
                {
                    if (x == 2)
                    {
                        M(2);
                    }
                    {|BRO1139:else|}
                    {
                        if (x == 3)
                        {
                            M(3);
                        }
                    }
                }

                if (x == 4) M(4); {|BRO1139:else|} { if (x == 5) M(5); else M(6); }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1)
                {
                    M(1);
                }
                else if (x == 2)
                {
                    M(2);
                }
                else if (x == 3)
                {
                    M(3);
                }

                if (x == 4) M(4); else if (x == 5) M(5); else M(6);
            }
        }
        """,
        NoBraceRules);

    [Fact]
    public Task WhenTheBraceRulesReportTheSameAfterTheJoin_OnlyTheJoinIsDone() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 4) M(4);
                {|BRO1139:else|}
                    if (x == 5) M(5);
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 4) M(4);
                else if (x == 5) M(5);
            }
        }
        """);

    [Fact]
    public Task WhenTheJoinChangesWhichBraceRuleReports_TheChainGetsItsBraces() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 4) M(4); {|BRO1139:else|} { if (x == 5) M(5); else M(6); }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 4)
                {
                    M(4);
                }
                else if (x == 5)
                {
                    M(5);
                }
                else
                {
                    M(6);
                }
            }
        }
        """);

    [Fact]
    public Task KAndRStyle_BecomesElseIf() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1) {
                    M(1);
                } {|BRO1139:else|} {
                    if (x == 2) {
                        M(2);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1) {
                    M(1);
                } else if (x == 2) {
                    M(2);
                }
            }
        }
        """);

    // With BRO1143 on, an 'else' after a jump is BRO1143's: it removes the 'else', which flattens more than joining it,
    // and it skips 'else if' chains, so joining first would leave a different result depending on the fix order.
    [Fact]
    public Task ElseAfterAJump_IsLeftToBRO1143WhenItIsOn() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(int age, bool student)
            {
                if (age < 6)
                {
                    return 0;
                }
                else
                {
                    if (student)
                    {
                        return 5;
                    }
                    else
                    {
                        return 10;
                    }
                }
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning");

    [Fact]
    public Task ElsesThatCantBeJoined_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1)
                {
                    M(1);
                }
                else
                {
                    M(0);
                    if (x == 2)
                    {
                        M(2);
                    }
                }

                if (x == 1)
                {
                    M(1);
                }
                else // two
                    if (x == 2)
                    {
                        M(2);
                    }

                if (x == 1)
                {
                    M(1);
                }
                else
                {
                    // three
                    if (x == 3)
                    {
                        M(3);
                    }
                }

                if (x == 1)
                {
                    M(1);
                }
                else
                {
                    if (x == 4)
                    {
                        M(4);
                    }
                    // four
                }

                if (x == 1)
                {
                    M(1);
                }
                else
                {
        #if !UNDEFINED
                    if (x == 5)
                    {
                        M(5);
                    }
        #endif
                }

                if (x > 0)
                    if (x == 1) M(1); else { if (x == 6) M(6); }
                else M(0);

                if (x == 1)
                {
                    M(1);
                }
                else
                    if (x == 8)
                    {
          M(8);
                    }

                if (x == 1)
                {
                    M(1);
                }
                else
            if (x == 9)
            {
                M(9);
            }

                if (x == 1)
                {
                    M(1);
                }
                else
                {
                    if (x == 7)
                    {
                        M(@"
                            multi-line".Length);
                    }
                }
            }
        }
        """);

    [Fact]
    public Task MultiLineStringThatDoesNotMove_IsFine() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1) M(1); {|BRO1139:else|} { if (x == 2) M(@"a
        b".Length); }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                if (x == 1) M(1); else if (x == 2) M(@"a
        b".Length);
            }
        }
        """,
        NoBraceRules);
}
