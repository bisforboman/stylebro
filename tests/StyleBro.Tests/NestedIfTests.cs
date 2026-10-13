using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.NestedIfAnalyzer, StyleBro.CodeFixes.Readability.NestedIfCodeFixProvider>;

namespace StyleBro.Tests;

public class NestedIfTests
{
    private const string BracesOff = "dotnet_diagnostic.BRO1514.severity = none\ndotnet_diagnostic.BRO1515.severity = none\ndotnet_diagnostic.BRO1516.severity = none\n";

    [Fact]
    public Task NestedBlocks_AreMerged() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a)
                {
                    {|BRO1149:if|} (b)
                    {
                        M(a, b);

                        M(b, a);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a && b)
                {
                    M(a, b);

                    M(b, a);
                }
            }
        }
        """);

    // StabilityMatrix: CSharpier relaid every merged condition that got longer than its line width. With CSharpier laying
    // the code out, the merge writes CSharpier's layout: '&&' operands on lines of their own, ')' on its own line; and
    // a body without braces gets them in the same edit (CSharpier's split makes it multi-line, BRO1515).
    [Fact]
    public Task WithCSharpier_ALongMergedCondition_IsLaidOutAsCSharpierWould() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool first, bool second, int count)
            {
                if (first && second)
                {
                    {|BRO1149:if|} (count > 100 && count < 1000000)
                    {
                        M(first, second, count);
                    }
                }

                if (
                    first
                )
                {
                    {|BRO1149:if|} (second)
                        return;
                }

                if (first)
                {
                    {|BRO1149:if|} (count > 100 && count < 1000000 && second)
                        return;
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool first, bool second, int count)
            {
                if (
                    first
                    && second
                    && count > 100
                    && count < 1000000
                )
                {
                    M(first, second, count);
                }

                if (first && second)
                    return;

                if (
                    first
                    && count > 100
                    && count < 1000000
                    && second
                )
                {
                    return;
                }
            }
        }
        """,
        "csharp_prefer_braces = when_multiline:warning\nstylebro_layout_by = csharpier\nstylebro_layout_print_width = 60\n");

    // EntityGraphQL's GraphQLParser: the inner condition was an '||' chain CSharpier had already broken over lines. Merged, it
    // becomes '&& (...)' and breaks inside its parentheses the way CSharpier does; one that fits its line stays on it.
    [Fact]
    public Task WithCSharpier_AnOrChainOperand_BreaksInsideItsParentheses() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool first, bool second, int count)
            {
                if (first)
                {
                    {|BRO1149:if|} (
                        (count > 100 && second)
                        || (count < 5 && first)
                    )
                    {
                        return;
                    }
                }

                if (first && second && count > 1000)
                {
                    {|BRO1149:if|} (
                        count < 5
                        || second
                    )
                    {
                        return;
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool first, bool second, int count)
            {
                if (
                    first
                    && (
                        (count > 100 && second)
                        || (count < 5 && first)
                    )
                )
                {
                    return;
                }

                if (
                    first
                    && second
                    && count > 1000
                    && (count < 5 || second)
                )
                {
                    return;
                }
            }
        }
        """,
        "stylebro_layout_by = csharpier\nstylebro_layout_print_width = 60\n");

    // What CSharpier would break further (an operand over several lines or longer than the line) keeps the usual layout.
    [Fact]
    public Task WithCSharpier_AnOperandCSharpierWouldBreak_KeepsTheUsualLayout() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool first, bool second, int count)
            {
                if (first)
                {
                    {|BRO1149:if|} (Check(count, count + 1, count + 2, count + 3, count + 4))
                    {
                        return;
                    }
                }

                if (second)
                {
                    {|BRO1149:if|} (Check(
                        count, count))
                    {
                        return;
                    }
                }
            }

            private static bool Check(params int[] values) => values.Length > 0;
        }
        """,
        """
        public class C
        {
            public void M(bool first, bool second, int count)
            {
                if (first && Check(count, count + 1, count + 2, count + 3, count + 4))
                {
                    return;
                }

                if (second && Check(
                    count, count))
                {
                    return;
                }
            }

            private static bool Check(params int[] values) => values.Length > 0;
        }
        """,
        "stylebro_layout_by = csharpier\nstylebro_layout_print_width = 60\n");

    // Flow.Launcher's ShellContextMenu: the inner condition's arguments were left behind ('...DRAWITEM) && x.HandleMenuMsg(' and
    // a line indented by the old dedent). An inner condition over several lines gets a line of its own, '&&' first.
    [Fact]
    public Task AnInnerConditionOverSeveralLines_StartsALine() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null
                    && (msg == 1
                        || msg == 2))
                {
                    {|BRO1149:if|} (Handle(
                            msg, msg) == 0)
                    {
                        return;
                    }
                }
            }

            private int Handle(int a, int b) => a + b;
        }
        """,
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null
                    && (msg == 1
                        || msg == 2)
                    && Handle(
                            msg, msg) == 0)
                {
                    return;
                }
            }

            private int Handle(int a, int b) => a + b;
        }
        """);

    [Fact]
    public Task AnInnerConditionOverSeveralLines_EndOfLineOperators() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null &&
                    msg > 0)
                {
                    {|BRO1149:if|} (msg == 1 ||
                        msg == 2)
                    {
                        return;
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null &&
                    msg > 0 &&
                    (msg == 1 ||
                        msg == 2))
                {
                    return;
                }
            }
        }
        """,
        editorConfig: "dotnet_style_operator_placement_when_wrapping = end_of_line\n");

    // sourcegit: the key says beginning_of_line, but nothing enforces it (init turned BRO1520 off: the code puts its
    // operators at the end), so the merged '&&' stays where the outer condition ended.
    [Fact]
    public Task TheOperatorKeyWithBRO1520Off_EndOfLine() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null &&
                    msg > 0)
                {
                    {|BRO1149:if|} (msg == 1)
                    {
                        return;
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null &&
                    msg > 0 &&
                    msg == 1)
                {
                    return;
                }
            }
        }
        """,
        editorConfig: "dotnet_style_operator_placement_when_wrapping = beginning_of_line\ndotnet_diagnostic.BRO1520.severity = none\n");

    // An '#endregion' right after the 'if' kept it until BRO1113 removed the region, so the merge came on a second run.
    [Fact]
    public Task RegionLinesAroundTheIf_AreMerged() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                #region Menu

                if (a)
                {
                    {|BRO1149:if|} (b)
                    {
                        #region Inner
                        M(b, a);
                        #endregion
                    }
                }

                #endregion
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                #region Menu

                if (a && b)
                {
                    #region Inner
                    M(b, a);
                    #endregion
                }

                #endregion
            }
        }
        """);

    [Fact]
    public Task LooserOperands_GetParentheses() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b, bool? c, int x)
            {
                if (a || b)
                {
                    {|BRO1149:if|} (x > 0 ? a : b)
                    {
                        M(a, b, c, x);
                    }
                }

                if (c ?? false)
                {
                    {|BRO1149:if|} (a && b)
                    {
                        M(a, b, c, x);
                    }
                }

                if ((a || b))
                {
                    {|BRO1149:if|} (!a || x is > 1 and < 5)
                    {
                        M(a, b, c, x);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b, bool? c, int x)
            {
                if ((a || b) && (x > 0 ? a : b))
                {
                    M(a, b, c, x);
                }

                if ((c ?? false) && a && b)
                {
                    M(a, b, c, x);
                }

                if ((a || b) && (!a || x is > 1 and < 5))
                {
                    M(a, b, c, x);
                }
            }
        }
        """);

    [Fact]
    public Task ThreeLevels_BecomeOneCondition() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(object o, bool a)
            {
                if (a)
                {
                    {|BRO1149:if|} (o is string s)
                    {
                        {|BRO1149:if|} (s.Length > 0)
                        {
                            M(s, a);
                        }
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(object o, bool a)
            {
                if (a && o is string s && s.Length > 0)
                {
                    M(s, a);
                }
            }
        }
        """);

    [Fact]
    public Task ChainsInsideTheBody_AreMergedInTheSameFix() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b, int[] items)
            {
                if (a)
                {
                    {|BRO1149:if|} (b)
                    {
                        foreach (var item in items)
                        {
                            if (item > 0)
                            {
                                {|BRO1149:if|} (item < 9)
                                {
                                    M(a, b, items);
                                }
                            }
                        }
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b, int[] items)
            {
                if (a && b)
                {
                    foreach (var item in items)
                    {
                        if (item > 0 && item < 9)
                        {
                            M(a, b, items);
                        }
                    }
                }
            }
        }
        """);

    [Fact]
    public Task EmbeddedStatements_AreMerged() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a)
                    {|BRO1149:if|} (b)
                        M(a, b);

                if (a) {|BRO1149:if|} (b) M(b, a);
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a && b)
                    M(a, b);

                if (a && b) M(b, a);
            }
        }
        """,
        BracesOff);

    [Fact]
    public Task BraceRulesOn_TheMergedIfGetsBraces() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a)
                {
                    {|BRO1149:if|} (b)
                        M(a, b);
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a && b)
                {
                    M(a, b);
                }
            }
        }
        """);

    [Fact]
    public Task ElseIf_TheChainGetsItsBraces() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (c)
                    M(a, b, c);
                else if (a)
                    {|BRO1149:if|} (b)
                        M(b, a, c);
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (c)
                {
                    M(a, b, c);
                }
                else if (a && b)
                {
                    M(b, a, c);
                }
            }
        }
        """);

    [Fact]
    public Task MergesInSeveralClausesOfAChain_KeepTheirOwnEdits() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (c)
                {
                    if (a)
                    {
                        {|BRO1149:if|} (b)
                        {
                            M(a, b, c);
                        }
                    }
                }
                else if (a)
                    {|BRO1149:if|} (b)
                        M(b, a, c);
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (c)
                {
                    if (a && b)
                    {
                        M(a, b, c);
                    }
                }
                else if (a && b)
                {
                    M(b, a, c);
                }
            }
        }
        """);

    [Fact]
    public Task MultiLineInnerCondition_MovesLeft() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (a)
                {
                    {|BRO1149:if|} (b
                        && c)
                    {
                        M(a, b, c);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (a && b
                    && c)
                {
                    M(a, b, c);
                }
            }
        }
        """);

    [Fact]
    public Task KAndRBraces_AreKept() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a) {
                    {|BRO1149:if|} (b) {
                        M(a, b);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a && b) {
                    M(a, b);
                }
            }
        }
        """,
        "csharp_new_line_before_open_brace = none\n");

    [Fact]
    public Task Skipped() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(bool a, bool b) => M(a, b, null);

            public void M(bool a, bool b, object o)
            {
                if (a)
                {
                    if (b)
                    {
                        M(a, b);
                    }
                }
                else
                {
                    M(b, a);
                }

                if (a)
                {
                    if (b)
                    {
                        M(a, b);
                    }
                    else
                    {
                        M(b, a);
                    }
                }

                if (a)
                {
                    // Only when b.
                    if (b)
                    {
                        M(a, b);
                    }
                }

                if (a)
                {
                    if (b /* b */)
                    {
                        M(a, b);
                    }
                }

                if (a) // a first
                {
                    if (b)
                    {
                        M(a, b);
                    }
                }

                if (a)
                {
                    if (b)
                    {
                        M(a, b);
                    }

                    // done
                }

                if (a)
                {
                    if (b)
                    {
                        #if DEBUG
                        M(a, b);
                        #endif
                    }
                }

                if (a)
                {
                    if (b)
                    {
                        M(a, b);
                    }

                    M(b, a);
                }

                if (a)
                {
                    if (b)
                    {
                        M(a, b);
                    }
                }
                #if DEBUG
                else
                {
                    M(b, a);
                }
                #endif

                M(b, a);

                if (a)
                {
                    if (o is string t)
                    {
                        M(t.Length > 0, b);
                    }
                }

                if (b)
                {
                    var t = o;
                    M(t != null, a);
                }

                if (a)
                {
                    if (b)
                    {
                        var s = @"one
                            two";
                        M(s.Length > 0, b);
                    }
                }
            }
        }
        """);

    [Fact]
    public Task CommentsInTheKeptParts_StayWhereTheyAre() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a)
                {
                    {|BRO1149:if|} (b && /* also */ a)
                    {
                        // both
                        M(a, b);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a && b && /* also */ a)
                {
                    // both
                    M(a, b);
                }
            }
        }
        """);

    // Sonar's S1066 skips an inner condition with a part of type dynamic; with the option, BRO1149 skips any merge with one.
    [Fact]
    public Task AllowNestedDynamicIfs_LeavesDynamicConditionsAlone() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b, dynamic d)
            {
                if (a)
                {
                    if (d == 1)
                    {
                        M(a, b, d);
                    }
                }

                if (d)
                {
                    if (b)
                    {
                        M(a, b, d);
                    }
                }

                if (a)
                {
                    if (b)
                    {
                        if (d.X)
                        {
                            M(a, b, d);
                        }
                    }
                }

                if (a)
                {
                    {|BRO1149:if|} (b)
                    {
                        M(a, b, d);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, bool b, dynamic d)
            {
                if (a)
                {
                    if (d == 1)
                    {
                        M(a, b, d);
                    }
                }

                if (d)
                {
                    if (b)
                    {
                        M(a, b, d);
                    }
                }

                if (a)
                {
                    if (b)
                    {
                        if (d.X)
                        {
                            M(a, b, d);
                        }
                    }
                }

                if (a && b)
                {
                    M(a, b, d);
                }
            }
        }
        """,
        "stylebro_allow_nested_dynamic_ifs = true" + "\n");

    [Fact]
    public Task DynamicConditions_AreMergedByDefault() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, dynamic d)
            {
                if (a)
                {
                    {|BRO1149:if|} (d == 1)
                    {
                        M(a, d);
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a, dynamic d)
            {
                if (a && d == 1)
                {
                    M(a, d);
                }
            }
        }
        """);
}
