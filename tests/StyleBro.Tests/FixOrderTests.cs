using static StyleBro.Tests.DocExamplesTests;

namespace StyleBro.Tests;

/// <summary>
/// 'dotnet format' fixes one diagnostic id at a time (one Fix All each, on fresh diagnostics), in an order that isn't
/// fixed. One run must converge in every order: a fix that changes which rule reports another finding, or what another
/// rule wants, must take care of that itself. Each case runs every order of its ids and expects no diagnostic left.
/// </summary>
public class FixOrderTests
{
    // The base class of the same_line cases at max_line_length.
    private const string BaseClass = """
        public class B
        {
            public B(int x)
            {
            }
        }

        """;

    [Fact]
    public Task Braces_AnElseChainThatBecomesInconsistent() => AssertConvergesInEveryOrderAsync(
        """
        using System.Collections.Generic;

        public static class C
        {
            public static IEnumerable<T> M<T>(IEnumerator<T> e, T defaultValue)
            {
                if (!e.MoveNext())
                    yield return defaultValue;
                else
                    do
                    {
                        yield return e.Current;
                    } while (e.MoveNext());
            }
        }
        """,
        "BRO1514",
        "BRO1515",
        "BRO1516");

    // akka.net: a comment below a ';' with a blank line under it describes the code above (BRO1504 leaves it); once
    // BRO1515's braces put a '}' there, BRO1504 wants the blank line, so the wrap adds it. BRO1602's '//' below a '}' too.
    [Fact]
    public Task Braces_AndTripleSlash_GiveBRO1504sBlankLine() => AssertConvergesInEveryOrderAsync(
        """
        using System;

        public class C
        {
            public int Ticks { get; }

            public C(int ticks)
            {
                if (ticks > 1073741824)
                    throw new ArgumentOutOfRangeException(nameof(ticks), ticks,
                        "Cannot be greater than 2^30.");
                // ReSharper restore NotResolvedInText

                Ticks = ticks;
            }

            public string Join(bool windows)
            {
                if (windows)
                {
                    return "w";
                }
                /// On Unix: the rules are ignored.
                else
                {
                    return "u";
                }
            }
        }
        """,
        "BRO1504",
        "BRO1515",
        "BRO1519",
        "BRO1602");

    // akka.net: 'dotnet format' splits '};' (csharp_preserve_single_line_statements = false); BRO1519's blank line before
    // the ';' stayed when BRO1101 removed it, and BRO1518 removed it on the next run. BRO1519 leaves that ';' to BRO1101.
    [Fact]
    public Task EmptyStatementAfterABlock_LeavesNoBlankLine() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        using System;
        using System.Collections.Generic;
        using System.Threading.Tasks;

        public class C
        {
            public async Task Run(Func<Func<Task>, int, Task> assert, List<int> expected)
            {
                await assert(async () =>
                {
                    foreach (var v in expected)
                    {
                        await Task.Delay(v);
                    }
                    ;
                }, 1);
            }
        }
        """,
        "csharp_preserve_single_line_statements = false\n",
        "BRO1101",
        "BRO1518",
        "BRO1519",
        SdkFormatter);

    // akka.net: blank lines below a trailing comment and above a '}' were nobody's (BRO1518 left them to BRO1506, which
    // skips trailing comments), until BRO1516's braces put a '}' there.
    [Fact]
    public Task Braces_BlankLinesBelowATrailingComment_AreRemoved() => AssertConvergesInEveryOrderAsync(
        """
        using System.Collections.Generic;
        using System.Threading.Tasks;

        public class C
        {
            public async Task Probe(IEnumerable<int> locs, int first)
            {
                foreach (var loc in locs)
                {
                    if (loc == first)
                    {
                        await Task.Delay(loc);
                    }
                    else
                        await Task.Delay(1); // should not move

                }
            }
        }
        """,
        "BRO1516",
        "BRO1518",
        "BRO1519");

    // akka.net: BRO1504's blank line split a trailing comment from its continuation below, which the next run's whitespace
    // formatting then moved to the code's indentation.
    [Fact]
    public Task TrailingCommentContinuedBelow_StaysTogether() => AssertConvergesInEveryOrderAsync(
        """
        using System;

        public class C
        {
            public void Serializer(int a)
            {
                Console.WriteLine(a); // This is because identifier is hardwired, so it could not be
                                      // used to override other serializer identifier
                Console.WriteLine(a);
            }
        }
        """,
        "BRO1504",
        SdkFormatter);

    // sourcegit: with when_multiline, BRO1515 wraps the multi-line 'else if' and makes the single-line 'if' BRO1516's,
    // which used to need a second run.
    [Fact]
    public Task Braces_AMultiLineClauseMakesTheChainInconsistent() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public static class C
        {
            public static int M(string line, int count)
            {
                if (line.StartsWith("Binary"))
                    count = -1;
                else if (line.StartsWith("++<") ||
                    line.StartsWith("++>"))
                    count++;

                return count;
            }
        }
        """,
        "csharp_prefer_braces = when_multiline:warning\n",
        "BRO1514",
        "BRO1515",
        "BRO1516");

    // sourcegit: BRO1149's wrapped '&&' goes where BRO1520 wants it, so BRO1520 has nothing left on a second run.
    [Fact]
    public Task NestedIf_WrappedOperatorNextToBRO1520() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(object? menu, int msg)
            {
                if (menu != null &&
                    msg > 0)
                {
                    if (msg == 1)
                    {
                        return;
                    }
                }
            }
        }
        """,
        "BRO1149",
        "BRO1520");

    [Fact]
    public Task InternalMethods_SortedBelowPublicOnes() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        internal class C
        {
            public void Run()
            {
            }

            internal void Stop()
            {
            }

            public void Dispose()
            {
            }

            private void Wait()
            {
            }
        }
        """,
        "dotnet_diagnostic.BRO1409.severity = warning\n",
        "BRO1001",
        "BRO1409");

    // sameText: false because the order BRO1508 first expands P's inner block inside the single-line method body and
    // leaves 'public void P(bool x) { if (x)' (no rule reports it); the other orders expand the method too.
    [Fact]
    public Task Braces_InASingleLineBlock() => AssertConvergesInEveryOrderCoreAsync(
        """
        public class C
        {
            public void M(bool x) { if (x) return; }

            public int N(bool x) { if (x) M(x); else M(!x); return 1; }

            public void O(bool x)
            {
                if (x) { if (!x) return; }
            }

            public void P(bool x) { if (x) { if (!x) M(x); else M(!x); } }
        }
        """,
        null,
        false,
        "BRO1508",
        "BRO1509",
        "BRO1514",
        "BRO1519");

    // jint (BRO1509 off): BRO1508 expanded the foreach block alone, assuming the method body was expanded too, and left
    // 'return sum; }' behind its '}'; the second run put two statements on one line. A block inside a single-line block
    // that stays is left alone.
    [Fact]
    public async Task SingleLineBlocks_InsideABlockWhoseRuleIsOff_StayAsTheyAre()
    {
        const string Source = """
            using System.Collections.Generic;

            public class ListTaker
            {
                public int SumList(List<int> values) { var sum = 0; foreach (var v in values) { sum += v; } return sum; }

                public void M(bool x) { if (x) { M(!x); } }
            }
            """;
        await AssertConvergesInEveryOrderWithConfigAsync(Source, "dotnet_diagnostic.BRO1509.severity = none\n", "BRO1508", "BRO1514", "BRO1519");
        await StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.SingleLineBlocksAnalyzer, StyleBro.CodeFixes.Layout.SingleLineBlocksCodeFixProvider>.VerifyNoDiagnosticsAsync(Source, "dotnet_diagnostic.BRO1509.severity = none");
    }

    // jint: a comment right below a '}' with a blank line under it gave two results across identical runs.
    [Fact]
    public Task CommentBelowABlock_WithABlankLineUnderIt() => AssertConvergesInEveryOrderAsync(
        """
        public static class Fraction
        {
            public static string Format(int precision, int value)
            {
                var sb = new System.Text.StringBuilder();
                if (precision > 0)
                {
                    sb.Append(value);
                }
                // precision == 0: no fractional part

                return sb.ToString();
            }
        }
        """,
        "BRO1504",
        "BRO1506",
        "BRO1519");

    [Fact]
    public Task SingleLineBlocks_InsideOtherSingleLineBlocks() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(bool x) { }

            public void Q(bool x) { void L() { if (x) { M(x); } } L(); }

            public int R { get { if (true) { return 1; } return 0; } }

            public void S(bool x) { System.Action a = () => { if (x) { M(x); } }; a(); }

            public void T(bool x) { if (x) M(x); { if (!x) { M(x); } } }
        }

        public class D { public void M(bool x) { if (x) { return; } } }
        """,
        "BRO1505",
        "BRO1508",
        "BRO1509",
        "BRO1514",
        "BRO1519");

    [Fact]
    public Task DocumentationElements_OrderedWhileTagsAreSortedRenamedOrRewritten() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            /// <returns>Never <c>null</c>.</returns>
            /// <param name="b">B, see <see cref="M&lt;T, U&gt;"/>.</param>
            /// <remarks></remarks>
            /// <summary>Adds.</summary>
            /// <typeparam name="U">U.</typeparam>
            /// <param name="old">A.</param>
            /// <typeparam name="T">T.</typeparam>
            /// <param name="c">C.</param>
            public object M<T, U>(int a, int b, int c) => a + b + c;
        }
        """,
        "BRO1610",
        "BRO1611",
        "BRO1613",
        "BRO1618",
        "BRO1619");

    [Fact]
    public Task DeclarationComments_MovedNextToBlankLinesAndDocumentation() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        // note
        {

            /// <summary>Docs.</summary>
            public void M()
            // why
            {
                M();
            }
        }
        """,
        "BRO1134",
        "BRO1503",
        "BRO1504",
        "BRO1506",
        "BRO1513");

    [Fact]
    public Task EmbeddedComments_MovedAboveABlankLine() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        // note
        {

            private int x;

            public int M()
            // why
            {

                if (x > 0) // positive
                {

                    return x;
                }

                return 0;
            }
        }
        """,
        "BRO1132",
        "BRO1134",
        "BRO1503",
        "BRO1506");

    [Fact]
    public Task CallChains_WithSplitArgumentsAndAWrappedOperator() => AssertConvergesInEveryOrderAsync(
        """
        using System.Linq;

        public class C
        {
            public bool M(int[] items, int a, int b) =>
                items
                    .Where(i => Check(i, a,
                        b)).Select(i => i
                    ).Any() &&
                    items.Contains(b);

            private static bool Check(int i, int a, int b) => i > a + b;
        }
        """,
        "BRO1523",
        "BRO1108",
        "BRO1110",
        "BRO1520");

    // BRO1523 skips a link with a comment in its gap; BRO1504 leaves a comment between links alone, and a comment above
    // a link BRO1523 moves stays where it is.
    [Fact]
    public Task CallChains_WithCommentsBetweenLinks() => AssertConvergesInEveryOrderAsync(
        """
        using System.Linq;

        public class C
        {
            public int M(int[] items)
            {
                var a = 1;
                // first the positive ones
                return items
                    .Where(i => i > a)
                    // then two of them
                    .Take(2).Select(i => i)
                    .Count();
            }
        }
        """,
        "BRO1523",
        "BRO1504",
        "BRO1506");

    [Fact]
    public Task ClosingParenthesisOnOwnLine_Declarations() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class B
        {
            public B(int a, int b)
            {
            }
        }

        public class C : B
        {
            public C(int a,
                int b) : base(a, b)
            {
            }

            public T M<T>
                (int a, int b) where T : class => default!;

            public int N(
                int a,
                int b)
                => a + b;
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line\n",
        "BRO1105",
        "BRO1109",
        "BRO1110",
        "BRO1111",
        "BRO1521");

    [Fact]
    public Task ClosingParenthesisOnOwnLine_Calls() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(int a, int b) => a;

            public int N() => 1;

            public void Run()
            {
                M(1,
                    2);
                M(1, 2,
                    M(3, 4));
                N(
                );
                M(
                    N(), M(5,
                        6));
            }
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line\n",
        "BRO1107",
        "BRO1108",
        "BRO1110",
        "BRO1116");

    // The leading multi-line lambda option (owner's choice 2026-10-15) next to BRO1110's own_line: the ')' goes to its own line
    // and the shape stays, whichever fix runs first.
    [Fact]
    public Task LeadingMultiLineLambda_WithClosingParenthesisOnOwnLine() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public void Register(System.Action<int> action, int kind, int order) { }

            public void Run()
            {
                Register(x =>
                {
                    System.Console.WriteLine(x);
                }, 1, 2);
                Register(x =>
                {
                    System.Console.WriteLine(x);
                }, 1,
                    2);
            }
        }
        """,
        "stylebro_allow_leading_multiline_lambda = true\nstylebro_closing_parenthesis_placement = own_line\n",
        "BRO1107",
        "BRO1108",
        "BRO1110");

    // Trial 25 (Flow Launcher): a parenthesized leading lambda ('BeginInvoke((() => { ... }), X)') kept '}, X);' when
    // BRO1405 removed the parentheses first, and was split when BRO1108 ran first.
    [Fact]
    public Task LeadingMultiLineLambda_InParentheses() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public void Invoke(System.Action action, int priority) { }

            public void Run()
            {
                Invoke((() =>
                {
                    System.Console.WriteLine();
                }), 1);
                Invoke(new System.Action(() =>
                {
                    System.Console.WriteLine();
                }), 1);
            }
        }
        """,
        "stylebro_allow_leading_multiline_lambda = true\n",
        "BRO1107",
        "BRO1108",
        "BRO1405");

    [Fact]
    public Task ClosingParenthesisOnOwnLine_CallChains() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        using System.Linq;

        public class C
        {
            public bool M(int[] items, int a, int b) =>
                items
                    .Where(i => Check(i, a,
                        b)).Select(i => i
                    ).Any() &&
                    items.Contains(b);

            private static bool Check(int i, int a, int b) => i > a + b;
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line\n",
        "BRO1523",
        "BRO1108",
        "BRO1110",
        "BRO1520");

    [Fact]
    public Task ClosingParenthesisOnOwnLine_HuggedLambdas() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public bool Run(System.Func<int, bool> check) => check(1);

            public void M()
            {
                Run(x =>
                    (x > 1));
                Run(x =>
                    { if (x > 1) return true; return false; });
                Run(x => Run(y =>
                    y > x));
                Run(x =>
                {
                    if (x > 1) return true;
                    return false;
                }
                );
            }
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line\n",
        "BRO1110",
        "BRO1405",
        "BRO1508",
        "BRO1514");

    [Fact]
    public Task SplitListFirstItemOnSameLine() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(int a, int b, int c) => a;

            public int N(int a,
                         int b, int c)
                => a;

            public void Run()
            {
                M(1, 2,
                    M(3,
                      4, 5), 6);
                M
                    (1, 2,
                    3);
            }
        }
        """,
        "stylebro_split_list_first_item = same_line\nstylebro_closing_parenthesis_placement = own_line\n",
        "BRO1107",
        "BRO1108",
        "BRO1109",
        "BRO1110");

    [Fact]
    public Task LiteralSuffixes_ACastOfALowerCaseSuffix() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public long M(long a) => a + (long)1u;

            public ulong N() => (ulong)2l;
        }
        """,
        "BRO1122",
        "BRO1135");

    [Fact]
    public Task SwitchSections_Include() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                    {
                        break;
                    }
                    case 2:
                        break;


                    case 3:
                        if (x > 0)
                        {
                            return;
                        }

                        break;
                    default:
                    {
                        break;

                    }
                }
            }
        }
        """,
        "BRO1517",
        "BRO1518",
        "BRO1519",
        "BRO1526");

    [Theory]
    [InlineData("omit")]
    [InlineData("omit_after_block")]
    public Task SwitchSections_Omit(string mode) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                    {
                        break;
                    }
                    case 2:
                        break;


                    case 3:
                        break;

                    case 4: { break; }

                    default:
                        break;
                }
            }
        }
        """,
        "stylebro_blank_line_between_switch_sections = " + mode + "\n",
        "BRO1517",
        "BRO1519",
        "BRO1526");

    // Stateless (StateMachine.Async.cs): with 'omit', BRO1519 added a blank line after a section's '}' and BRO1504 one above
    // a comment that starts the next section.
    [Fact]
    public Task SwitchSections_Omit_Bro1519AndBro1504LeaveTheGaps() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(object x, bool b)
            {
                switch (x)
                {
                    case int:
                        return 0;
                    // A comment that starts the section.
                    case string s:
                    {
                        if (b) return 1;
                        return 2;
                    }

                    case long:
                        if (b)
                            return 3;
                        break;
                    default:
                        break;
                }

                return 4;
            }
        }
        """,
        "stylebro_blank_line_between_switch_sections = omit\n",
        "BRO1504",
        "BRO1514",
        "BRO1519",
        "BRO1526");

    // realworlddotnet: with BRO1107 off (init, mixed code), BRO1108 keeps the first item on the '(' line, in every order.
    [Fact]
    public Task ListsWithBro1107Off_KeepTheFirstItem() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int UpdateBySlug(string slug,
                int request, int cancellationToken
                ) => M(slug.Length
                    , request, cancellationToken);

            private static int M(int a, int b, int c) => a + b + c;
        }
        """,
        "dotnet_diagnostic.BRO1107.severity = none\n",
        "BRO1108",
        "BRO1110",
        "BRO1117");

    // The same list with BRO1107 on: the first item moves too, and every order gives the same text.
    [Fact]
    public Task ListsWithBro1107On_MoveTheFirstItem() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int UpdateBySlug(string slug,
                int request, int cancellationToken
                ) => M(slug.Length
                    , request, cancellationToken);

            private static int M(int a, int b, int c) => a + b + c;
        }
        """,
        null,
        "BRO1107",
        "BRO1108",
        "BRO1110",
        "BRO1117");

    [Fact]
    public Task ElseAfterJump_BracesAndBlankLines() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(bool x, bool y)
            {
                if (x)
                {
                    return 1;
                }
                else
                {
                    if (y) { return 2; } else { return 3; }
                }
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning\n",
        "BRO1143",
        "BRO1508",
        "BRO1517",
        "BRO1518",
        "BRO1519");

    [Fact]
    public Task ElseAfterJump_WithoutBraces() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                    return 1;
                else
                    return 2;
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning\n",
        "BRO1143",
        "BRO1514",
        "BRO1516",
        "BRO1519");

    // An 'if' branch without braces next to an 'else' block: BRO1143's fix adds BRO1516's braces itself (it needed a
    // second run before). With BRO1514 off too, so the braces come from BRO1516 alone.
    [Theory]
    [InlineData("")]
    [InlineData("dotnet_diagnostic.BRO1514.severity = none\n")]
    public Task ElseAfterJump_BranchWithoutBraces_ElseBlock(string config) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                    return 1;
                else
                {
                    return 2;
                }
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning\n" + config,
        "BRO1143",
        "BRO1514",
        "BRO1516",
        "BRO1519");

    [Fact]
    public Task ElseAfterJump_BranchesWithoutBraces_Nested() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(bool x, bool y)
            {
                if (x)
                    return 1;
                else
                {
                    y = !y;
                    if (y)
                        return 2;
                    else
                    {
                        return 3;
                    }
                }
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning\n",
        "BRO1143",
        "BRO1514",
        "BRO1516",
        "BRO1519");

    [Fact]
    public Task ElseAfterJump_BranchBlock_ElseWithoutBraces() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                {
                    return 1;
                }
                else
                    return 2;
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning\n",
        "BRO1143",
        "BRO1514",
        "BRO1516",
        "BRO1519");

    // BRO1139 joins 'else { if }' into 'else if'; BRO1143 removes an 'else' after a jump and skips 'else if' chains. With
    // both on, BRO1139 leaves such an 'else' to BRO1143, so the order 'dotnet format' picks doesn't change the result
    // (samples/Messy/Input/Fares.cs failed verify-format in 2 of 3 runs before).
    [Fact]
    public Task ElseIf_LeavesAnElseAfterAJumpToBRO1143() => AssertConvergesInEveryOrderWithConfigAsync(
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
        "dotnet_diagnostic.BRO1143.severity = warning\n",
        "BRO1139",
        "BRO1143",
        "BRO1519");

    // NLog (trial 26): BRO1602's '//' below '[InlineData]' got BRO1504's blank line, which BRO1525 (comment in the gap) left.
    [Fact]
    public Task TripleSlashBelowAnAttribute_StaysAttached() => AssertConvergesInEveryOrderAsync(
        """
        using System;

        public class C
        {
            [Obsolete]

            /// <summary>
            /// https://example.com/issues/3194
            /// </summary>
            public void M()
            {
            }
        }
        """,
        "BRO1504",
        "BRO1525",
        "BRO1602");

    [Fact]
    public Task Attributes_TheOnlyBlankLineBeforeAnElementIsBelowItsAttribute() => AssertConvergesInEveryOrderAsync(
        """
        using System;

        public class C
        {
            public void M()
            {
            }
            [Obsolete]


            public void N()
            {
            }
        }
        """,
        "BRO1505",
        "BRO1517",
        "BRO1525");

    [Fact]
    public Task Conditionals_ATokenOnTheWrongSideAndOneWithoutLineBreak() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public string M(bool a, string s, string t)
            {
                var x = a ? s :
                    t;
                return a ?
                    x : t;
            }
        }
        """,
        "BRO1520",
        "BRO1524");

    [Fact]
    public Task Conditionals_OperatorPlacementOff_EachConditionalMirrorsItsOwnLineBreak() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public string M(bool a, string s, string t)
            {
                var x = a ? s :
                    t;
                return a
                    ? x : t;
            }
        }
        """,
        "dotnet_diagnostic.BRO1520.severity = none\n",
        "BRO1520",
        "BRO1524");

    [Fact]
    public Task Ordering_AMultiLineFieldSortedAboveAField() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            private int b;
            private static int[] a = new[]
            {
                1,
            };
        }
        """,
        "BRO1001",
        "BRO1505");

    [Fact]
    public Task CombinedFields_AMultiLineFieldSplitFromTheNext() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            private int[] a = new[]
            {
                1,
            }, b;
        }
        """,
        "BRO1114",
        "BRO1505");

    [Fact]
    public Task Ordering_MembersOfRegionsThatAreRemoved() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            #region Methods

            public void M()
            {
            }

            #endregion

            #region Fields

            private int a;

            #endregion
        }
        """,
        "BRO1001",
        "BRO1112");

    // Polly's Context.Dictionary.cs: sorting within the region first put the commented property next to another property,
    // which made its comment a group comment (GroupComments) and blocked the sort across the removed regions.
    [Fact]
    public Task Ordering_ACommentedMemberOfRegionsThatAreRemoved() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            // The values.
            private int P { get; set; }

            public C()
            {
            }

            #region Public

            public int Q { get; set; }

            #endregion
        }
        """,
        "BRO1001",
        "BRO1112");

    // Newtonsoft.Json's JsonSerializerTest.cs: moving the lambda first (BRO1107) indented the line where the string starts
    // deeper than the string's continuation lines, so BRO1108 then moved the string without them.
    [Fact]
    public Task ParameterLayout_AnItemOnTheLineWhereAMovedLambdaEnds() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(System.Action a, string s)
            {
            }

            public void N()
            {
                M(() =>
                {
                    N();
                }, "a" +
                   "b");
            }
        }
        """,
        "BRO1107",
        "BRO1108",
        "BRO1520");

    [Fact]
    public Task Regions_ACommentAboveTheRemovedLines() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public int M()
            {
                #region Usage
                var x = 1;
                // x is 1
                #endregion

                return x;
            }
        }
        """,
        "BRO1113",
        "BRO1506");

    [Fact]
    public Task Regions_ACommentAfterCodeAboveTheRemovedLines() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public int M()
            {
                #region Usage
                var x = 1;
                // x is 1
                #endregion

                return x;
            }
        }
        """.Replace("\n", "\r\n"),
        "BRO1113",
        "BRO1504",
        "BRO1506");

    [Fact]
    public Task Regions_AHeaderRegionBelowUsings() => AssertConvergesInEveryOrderAsync(
        """
        using System.Text;
        #region License
        // header
        #endregion

        namespace N
        {
            public class C
            {
            }
        }
        """,
        "BRO1112",
        "BRO1504",
        "BRO1505",
        "BRO1506");

    [Fact]
    public Task Regions_BlankLinesAroundAnEndRegionBeforeABrace() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            #region Methods

            public void M()
            {
            }

            #endregion

        }
        """,
        "BRO1112",
        "BRO1518");

    // Kavita: a chain skipped for the regions in its lambda was BRO1523's on the run after BRO1113 removed them.
    [Fact]
    public Task Regions_InsideACallChainsLambda() => AssertConvergesInEveryOrderAsync(
        """
        using System.Threading.Tasks;

        public class C
        {
            public void M()
            {
                Task.Run(async () =>
                    {
                        #region Work
                        await Task.Yield();
        #endregion
                    }).GetAwaiter()
                    .GetResult();
            }
        }
        """,
        "BRO1113",
        "BRO1523");

    // Scrutor: whether a comment introduces a group mustn't depend on blank lines that BRO1505/BRO1506/BRO1509 add or
    // remove in the same run (a first version judged by blank lines and sorted these on the second run).
    [Fact]
    public Task Ordering_GroupCommentsAndBlankLines() => AssertConvergesInEveryOrderAsync(
        """
        namespace N;

        public class Tests
        {
        }

        // Test interfaces and classes
        public interface IService { }
        public interface IOtherService { }

        public class Service : IService { }

        // ReSharper disable UnusedTypeParameter

        public class Query { }

        public interface IQuery { }
        """,
        "BRO1001",
        "BRO1505",
        "BRO1506",
        "BRO1509");

    // Fonts: an empty comment between two blank lines, between two comments.
    [Fact]
    public Task EmptyComment_BetweenBlankLinesAndComments() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M()
            {
                // The number of glyphs.

                //

                // The format.
                M();
            }
        }
        """,
        "BRO1120",
        "BRO1504",
        "BRO1506",
        "BRO1517");

    // Kavita: empty regions in a row, each with several blank lines, the last one before the type's '}'.
    [Fact]
    public Task Regions_EmptyRegionsWithBlankLineRuns() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            // TODO: Implement

            #region First



            #endregion

            #region Second



            #endregion

        }
        """,
        "BRO1112",
        "BRO1503",
        "BRO1506",
        "BRO1517",
        "BRO1518");

    [Fact]
    public Task RedundantReturn_InSingleLineBodiesAndBelowABlankLine() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M() { return; }

            public void N(bool b)
            {
                if (b) { M(); }

                return;
            }

            public void O(bool b) { if (b) M(); return; }
        }
        """,
        "BRO1137",
        "BRO1508",
        "BRO1509",
        "BRO1514",
        "BRO1518",
        "BRO1519");

    [Fact]
    public Task EmptyRecordBody_NextToASemicolonAndASingleLineBlock() => AssertConvergesInEveryOrderAsync(
        """
        public record A(int X) { };

        public record B(int X) { }

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """,
        "BRO1101",
        "BRO1140",
        "BRO1509");

    [Fact]
    public Task ElseIf_NextToTheBraceRules() => AssertConvergesInEveryOrderAsync(
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
                    if (x == 2)
                        M(2);
                    else
                        M(3);
                }

                if (x == 1)
                    M(1);
                else
                    if (x == 2) M(2);

                if (x == 1) { M(1); } else { if (x == 2) { M(2); } }
            }
        }
        """,
        "BRO1139",
        "BRO1508",
        "BRO1514",
        "BRO1515",
        "BRO1516");

    [Fact]
    public Task CombinedLocals_InASingleLineBlock() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public int M() { int a = 1, b = 2; return a + b; }
        }
        """,
        "BRO1142",
        "BRO1509");

    [Fact]
    public Task Strings_EmptyOnesAreLeftToBro1106() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public string A = @"";
            public string B = $"";
            public string D = $@"";
            public string E = @"text";
        }
        """,
        "BRO1106",
        "BRO1138");

    // The literal style: BRO1103 swaps 'string.Empty == s' (a static readonly field counts as a constant) or '"" == s'.
    [Fact]
    public Task Strings_TheLiteralStyle() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public string A = string.Empty;
            public string B = @"";

            public bool M(string s) => string.Empty == s;
        }
        """,
        "stylebro_empty_string_style = literal\n",
        "BRO1103",
        "BRO1106",
        "BRO1138");

    [Fact]
    public Task LiteralSuffixes_OnlyLInUpperCase() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public long M(long a) => a + (long)1u;

            public ulong N() => (ulong)2l + 3ul;
        }
        """,
        "stylebro_upper_case_literal_suffixes = l_only\n",
        "BRO1122",
        "BRO1135");

    // BRO1148 writes the form BRO1133 asks for, so BRO1133 never sees a check to rewrite; BRO1405 removes parentheses
    // around 'x.HasValue', and BRO1148 only replaces it where '!= null' binds the same.
    [Theory]
    [InlineData("pattern_matching")]
    [InlineData("equality_operator")]
    public Task HasValue_InTheFormOfBro1133(string style) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public bool M(int? n, string s, bool b)
            {
                var x = (n.HasValue);
                if (!n.HasValue || s == null || s is not null)
                {
                    return b == (n.HasValue);
                }

                return x && (!n.HasValue) && b;
            }
        }
        """,
        "dotnet_diagnostic.BRO1148.severity = warning\nstylebro_null_check_style = " + style + "\n",
        "BRO1133",
        "BRO1148",
        "BRO1405");

    [Fact]
    public Task ContextualKeywords_AFieldNamedFieldThatIsRenamed() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            private const int field = 2;
            private int other;

            public int P
            {
                get => field + this.other;
            }
        }

        public class D
        {
            private int field;

            public int P
            {
                get => field;
                set => field = value;
            }
        }
        """,
        "stylebro_private_field_naming = _camelCase\ndotnet_naming_rule.c.symbols = c\ndotnet_naming_rule.c.style = p\ndotnet_naming_rule.c.severity = warning\ndotnet_naming_symbols.c.applicable_kinds = field\ndotnet_naming_symbols.c.required_modifiers = const\ndotnet_naming_style.p.capitalization = pascal_case\n",
        "BRO1144",
        "BRO1303",
        "BRO1306");

    // StabilityMatrix (CSharpier's layout): IDE0055 put '{ get; init; }' on its own line below a split generic type, BRO1527
    // joined it back, on every run.
    [Fact]
    public Task AutoAccessors_BelowASplitGenericType_WithTheSdkFormatter() => AssertConvergesInEveryOrderAsync(
        """
        using System;
        using System.Collections.Generic;

        public class C
        {
            public Func<
                int,
                IReadOnlyDictionary<string, string>
            >? Factory { get; init; }

            public Func<
                int,
                string
            > Other
            {
                get;
                set;
            }

            public int Plain
            {
                get;
                set;
            }
        }
        """,
        "BRO1527",
        SdkFormatter);

    // eShop's CatalogApi: a lambda with its attribute list on its own line, after a first argument BRO1107 moves. The SDK's
    // formatter puts the lambda at the statement's indentation, below its own attribute line, in EVERY layout (the original
    // one too: probed with 'dotnet format whitespace', SDK 10.0.401); BRO1107 doesn't touch the lambda's lines.
    [Fact]
    public Task AttributedLambda_AfterTheMovedFirstArgument_WithTheSdkFormatter() => AssertConvergesInEveryOrderAsync(
        """
        using System;

        class ProducesAttribute : Attribute
        {
        }

        class C
        {
            static C Map(string pattern, Delegate handler) => new C();

            C Named(string name) => this;

            void M()
            {
                Map("/catalogtypes",
                    [Produces]
                    async (int context) => await System.Threading.Tasks.Task.FromResult(context))
                    .Named("ListItemTypes");
            }
        }
        """,
        "BRO1107",
        SdkFormatter);

    [Fact]
    public Task AutoAccessors_NextToTheBlankLineRules() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public int A { get; set; }
            public int B
            {
                get;
                set;
            }
            public int D { get; set; }
            public int E
            {
                get;
                private set;
            }
            public void M()
            {
            }
        }
        """,
        "BRO1505",
        "BRO1519",
        "BRO1527");

    [Fact]
    public Task AutoAccessors_BlankLineAfterTheBraceWithoutBro1505() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int B
            {
                get;
                set;
            }
            public int D { get; set; }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none\n",
        "BRO1519",
        "BRO1527");

    [Fact]
    public Task AutoAccessors_SortedNextToAOneLineProperty() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            internal int B
            {
                get;
                set;
            }

            public int A { get; set; }
            public int D { get; set; }
        }
        """,
        "BRO1001",
        "BRO1505",
        "BRO1527");

    // E counts as one line (its attribute line doesn't count); G and H stay multi-line after BRO1527's fix (the line break
    // before the name, the initializer), so they need their blank lines in every order.
    [Fact]
    public Task AutoAccessors_NextToPropertiesThatStayMultiLine() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public int A { get; set; }
            [System.Obsolete]
            public int E
            {
                get;
                set;
            }
            public int F { get; set; }
            public int
                G
            {
                get;
                set;
            }
            public int[] H
            {
                get;
                set;
            } = new[]
            {
                1,
            };
            public int I { get; set; }
        }
        """,
        "BRO1505",
        "BRO1519",
        "BRO1527");

    [Fact]
    public Task AutoAccessors_SortedNextToAOneLinePropertyWithoutBro1505() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            internal int B
            {
                get;
                set;
            }

            public int A { get; set; }
            public int D { get; set; }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none\n",
        "BRO1001",
        "BRO1519",
        "BRO1527");

    [Fact]
    public Task EmptyTypeBody_NextToASemicolonAndASingleLineBlock() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class A { };

        public struct B { }

        public interface I
        {
        };
        """,
        "dotnet_diagnostic.BRO1145.severity = warning\n",
        "BRO1101",
        "BRO1145",
        "BRO1509");

    // Trial 19: BRO1509's expansion puts embedded statements on their own lines (csharp_preserve_single_line_statements =
    // false), where the brace rules add no braces; with braces wanted they're the brace rules' edits.
    [Theory]
    [InlineData("csharp_preserve_single_line_statements = false\n")]
    [InlineData("csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = when_multiline\n")]
    public Task EmbeddedStatements_InAnExpandedBody(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public void M(int x) { if (x > 0) x++; else x--; while (x > 0) { x--; } }
        }
        """,
        editorConfig,
        "BRO1508",
        "BRO1509",
        "BRO1514",
        "BRO1516");

    // A braced 'if' with an unbraced 'else' in the expanded body: BRO1516 braces the 'else' (or, with csharp_prefer_braces =
    // false, the expansion puts it on its own line), BRO1508 expands the 'if' block.
    [Theory]
    [InlineData("csharp_preserve_single_line_statements = false\n")]
    [InlineData("csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = false\n")]
    public Task EmbeddedBlockAndUnbracedElse_InAnExpandedBody(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public void M(int x) { if (x > 0) { x++; } else x--; do { x--; } while (x > 0); }
        }
        """,
        editorConfig,
        "BRO1508",
        "BRO1509",
        "BRO1514",
        "BRO1516");

    // Trial 20: nested embedded statements and a switch in an expanded body come out as the formatter lays them out (with
    // csharp_preserve_single_line_statements = false it expands them once the block is expanded), braced or not.
    [Theory]
    [InlineData("csharp_preserve_single_line_statements = false\n")]
    [InlineData("csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = false\n")]
    [InlineData("csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = when_multiline\n")]
    public Task NestedEmbeddedStatements_InAnExpandedBody(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            private readonly object gate = new object();

            public int F(int n) { for (int i = 0; i < n; i++) if (i % 2 == 0) n--; while (n > 0) if (n-- > 5) return n; return 0; }

            public int U(System.IO.Stream s, System.IO.Stream t) { using (s) lock (gate) return 1; }

            public int V(System.IO.Stream s, System.IO.Stream t) { using (s) using (t) return 1; }

            public int I(bool a, bool b) { if (a) if (b) return 1; else return 2; return 3; }
        }
        """,
        editorConfig,
        "BRO1509",
        "BRO1514",
        "BRO1516",
        SdkFormatter);

    // Trial 21: standalone comments (a blank line above and below) keep their blank line by default, lose it with 'remove',
    // the same in every order of the sort and the blank-line rules.
    [Theory]
    [InlineData("indent_size = 4\n")]
    [InlineData("stylebro_blank_line_below_standalone_comments = remove\n")]
    public Task StandaloneComments_WithTheSortAndBlankLineRules(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public void B()
            {
            }

            // Fields


            private int a;

            public int A;

            // Helpers

            /// <summary>Doc.</summary>
            private void D()
            {
            }
        }
        """,
        editorConfig,
        "BRO1001",
        "BRO1506",
        "BRO1517",
        "BRO1505",
        "BRO1513");

    // Trial 21: with BRO1514 off (init's choice for code without braces), the expansion's statement lines make 'for (...)
    // if (x) y;' multi-line, which is BRO1515's (and a chain's BRO1516's): its braces come with the expansion.
    [Theory]
    [InlineData("csharp_preserve_single_line_statements = false\ndotnet_diagnostic.BRO1514.severity = none\n")]
    [InlineData("csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = when_multiline\ndotnet_diagnostic.BRO1514.severity = none\n")]
    public Task NestedEmbeddedStatements_InAnExpandedBody_WithoutBro1514(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public static class C
        {
            private static readonly object Gate = new object();
            private static int counter;

            public static int H(int n) { for (int i = 0; i < n; i++) if (i % 2 == 0) counter++; return counter; }

            public static int W(int n) { while (n > 0) if (n-- % 2 == 0) return n; return 0; }

            public static int U(System.IO.Stream s) { using (s) lock (Gate) return 1; }

            public static int Sw(int x) { switch (x) { case 1: return 10; default: return 30; } }

            public static int N(bool a, bool b) { if (a) if (b) counter++; else counter--; return counter; }
        }
        """,
        editorConfig,
        "BRO1509",
        "BRO1515",
        "BRO1516",
        SdkFormatter);

    [Theory]
    [InlineData("csharp_preserve_single_line_statements = false\n")]
    [InlineData("csharp_preserve_single_line_statements = false\nstylebro_blank_line_between_switch_sections = omit\ncsharp_indent_switch_labels = false\n")]
    [InlineData("csharp_preserve_single_line_statements = false\ncsharp_indent_case_contents = false\ncsharp_new_line_before_open_brace = methods, types\n")]
    public Task Switch_InAnExpandedBody(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int S(int x) { switch (x) { case 1: case 2: x++; break; default: return 30; } return x; }

            public void E(int x) { switch (x) { } }
        }
        """,
        editorConfig,
        "BRO1509",
        "BRO1519",
        "BRO1526",
        SdkFormatter);

    // actions/runner's VssStringComparer: one-line properties with block accessors in a row. BRO1509 expands each, and
    // BRO1505 then wants blank lines between them (two one-line properties may sit together, multi-line ones not).
    [Fact]
    public Task OneLineProperties_ExpandedWithBlankLinesBetween() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            private static readonly string s = "x";

            public static string A { get { return s; } }
            public static string B { get { return s; } }
            public static string C2 { get { return s; } }
            public static string D => s;
            public int E { get; set; }
        }

        public class D { public int A { get { return 1; } } public int B { get { return 2; } } }
        """,
        "BRO1505",
        "BRO1509");

    [Fact]
    public Task RecordClass_NextToAnEmptyRecordBody() => AssertConvergesInEveryOrderAsync(
        """
        public record class A(int X) { };

        public sealed record class B(int X)
        {
        }

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """,
        "BRO1101",
        "BRO1140",
        "BRO1146",
        "BRO1509");

    // actions/runner: '};' after a block, then a blank line or a comment. The SDK's formatter puts the ';' on a line of
    // its own; BRO1519 and BRO1504 then add blank lines around it, and removing the ';' line left two blank lines.
    [Fact]
    public Task EmptyStatement_AfterABlockAboveABlankLineOrAComment() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(bool x)
            {
                if (x)
                {
                    M(!x);
                };

                M(x);
                if (x)
                {
                    M(!x);
                }
                ;

                M(x);
                if (x)
                {
                    M(!x);
                }
                ;
                // next
                M(x);
            }
        }
        """,
        "BRO1101",
        "BRO1504",
        "BRO1517",
        "BRO1519",
        SdkFormatter);

    // actions/runner: '((Double)0).ToString(...)'. BRO1122's literal needs no parentheses, so BRO1405 removed them
    // in a second run.
    [Fact]
    public Task LiteralSuffix_InParenthesesBeforeAMemberAccess() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public string M() => ((double)0).ToString("G") + ((long)1).ToString() + ((long)-1).ToString() + (((float)2)).ToString();

            public double N() => 1 + ((double)2) * 3;
        }
        """,
        "BRO1122",
        "BRO1405");

    // stylebro_allow_adjacent_single_line_members: BRO1001's sort and BRO1527's collapse leave single-line members
    // without a block body together, as BRO1505 judges them.
    [Fact]
    public Task AdjacentSingleLineMembers_SortedAndCollapsed() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M() => P;
            public int P { get; set; }
            public int R
            {
                get;
                set;
            }
            public void N() { }
        }
        """,
        "stylebro_allow_adjacent_single_line_members = true\n",
        "BRO1001",
        "BRO1505",
        "BRO1519",
        "BRO1527");

    // The same for BRO1114's split (a documented event field keeps BRO1513's blank line) and BRO1509's expansion.
    [Fact]
    public Task AdjacentSingleLineMembers_SplitAndExpanded() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public event System.EventHandler? A, B;
            /// <summary>Docs.</summary>
            public event System.EventHandler? D, E;
        }

        public interface I { void A(); int B(); void C() { } int D => 1; }
        """,
        "stylebro_allow_adjacent_single_line_members = true\n",
        "BRO1114",
        "BRO1505",
        "BRO1509",
        "BRO1513");

    // stylebro_constructor_initializer_placement = same_line next to the parameter list rules: BRO1110's ')' moves in
    // BRO1105's edit, the others don't change the ')' line's end; BRO1509 expands the body after the initializer.
    [Fact]
    public Task ConstructorInitializerSameLine_NextToParameterListRules() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class B
        {
            public B(int x)
            {
            }
        }

        public class C : B
        {
            public C(
                int a
            )
                : base(a) { }

            public C(int a, long b,
                long c)
                : base(a)
            {
            }

            public C
                (long a)
                : base(1)
            {
            }
        }
        """,
        "stylebro_constructor_initializer_placement = same_line\n",
        "BRO1105",
        "BRO1107",
        "BRO1108",
        "BRO1109",
        "BRO1110",
        "BRO1509");

    // stylebro_constraint_placement = same_line next to BRO1110 (its ')' moves in BRO1111's edit), BRO1521 (it leaves an
    // '=>' after a constraint alone) and BRO1509 (it expands the body after the constraints).
    [Fact]
    public Task ConstraintSameLine_NextToParenthesesArrowsAndBodies() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public static class C
        {
            public static void M<T>(
                T value
            )
                where T : class { }

            public static int N<T>(T value)
                where T : class
                => 1;

            public static int O<T>(T value)
                where T : class =>
                1;
        }

        public class Box<T>
            where T : class { }
        """,
        "stylebro_constraint_placement = same_line\n",
        "BRO1110",
        "BRO1111",
        "BRO1505",
        "BRO1509",
        "BRO1521");

    // The same with BRO1110's own_line mode: a split list's ')' stays on its own line and the initializer or constraints
    // join it there; a ')' BRO1110 pulls up to the last item (a list that isn't split) moves in the join's edit.
    [Fact]
    public Task SameLineJoins_WithClosingParenthesisOnItsOwnLine() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class B
        {
            public B(int x)
            {
            }
        }

        public class C : B
        {
            public C(
                int a
            )
                : base(a) { }

            public C(int a, long b,
                long c)
                : base(a)
            {
            }

            public C(long a
                )
                : base(1)
            {
            }

            public static void M<T>(
                T value)
                where T : class { }
        }
        """,
        "stylebro_constructor_initializer_placement = same_line\nstylebro_constraint_placement = same_line\nstylebro_closing_parenthesis_placement = own_line\n",
        "BRO1105",
        "BRO1109",
        "BRO1110",
        "BRO1111",
        "BRO1509");

    [Fact]
    public Task OmittedTrailingCommas_NextToEnumExpansionAndValueLines() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public enum A { X, Y, }

        public enum B { X, Y }

        public enum C
        {
            X, Y,
        }

        public class D
        {
            public int[] M() => new[] { 1, 2, };
        }
        """,
        "stylebro_trailing_comma = omit\n",
        "BRO1121",
        "BRO1401",
        "BRO1509");

    [Fact]
    public Task AllowedEmptyBlocks_InsideExpandedOnes() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class A { public A() { } public void M(bool b) { if (b) { } else { M(b); } } }

        public enum E { }

        public class B { }
        """,
        "stylebro_allow_empty_single_line_blocks = true\n",
        "BRO1505",
        "BRO1508",
        "BRO1509",
        "BRO1519");

    // BRO1410 leaves the parentheses BRO1407 adds between 'and' and 'or' (and removes only extra pairs around them);
    // BRO1405 leaves those of BRO1406 by StyleCop's own logic.
    [Fact]
    public Task PatternParentheses_NextToPrecedence() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public bool M(int x, int a, int b) =>
                x is (1 or 2 and 3)
                || x is 1 or ((> 5 and < 9))
                || x is (> 0 and < 5) or 10
                || x is 4 or 5 and 6
                || (a + b * a) > 0;
        }
        """,
        "BRO1405",
        "BRO1406",
        "BRO1407",
        "BRO1410");

    [Fact]
    public Task MemberOrder_InAOneLineType() => AssertConvergesInEveryOrderAsync(
        """
        public interface I { void B(); void A(); }

        public class C { public void B() { } private int x; public void A() { } }
        """,
        "BRO1001",
        "BRO1505",
        "BRO1509");

    [Fact]
    public Task MemberOrder_BlankLinesInOtherSlots() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void B()
            {
            }
            private int y;

            private int x;
            public void A()
            {
            }
        }

        public class D
        {
            private int y;
            public int P { get; set; }

            public const int X = 1;
        }
        """,
        "BRO1001",
        "BRO1505");

    [Fact]
    public Task CombinedFields_DocumentedFieldsSplit() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            /// <summary>Docs.</summary>
            public int A, B;

            /// <summary>Docs.</summary>
            private int c, d;
        }
        """,
        "BRO1114",
        "BRO1513");

    [Fact]
    public Task EmptyParentheses_InsideASplitList() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public int M(int a, int b) => a;

            public int N() => 1;

            public int P() => M(
                N(
                ), M(5,
                6));
        }
        """,
        "BRO1108",
        "BRO1116");

    [Fact]
    public Task Regions_RemovedAroundMembersMissingBlankLines() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            #region Methods
            public void B()
            {
            }
            private int y;
            #endregion

            private int x;
            public void A()
            {
            }
        }
        """,
        "dotnet_diagnostic.BRO1112.severity = warning\n",
        "BRO1001",
        "BRO1112",
        "BRO1505");

    [Fact]
    public Task EmptyParentheses_InsideASplitListWithTheFirstItemOnTheSameLine() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(int a, int b) => a;

            public int N() => 1;

            public int P() => M(N(
                ), M(5,
                6));

            public int Q() => M(N(
                    ),
                M(N(
                ), 6));
        }
        """,
        "stylebro_split_list_first_item = same_line\n",
        "BRO1107",
        "BRO1108",
        "BRO1116");

    [Fact]
    public Task ClosingParenthesisOnOwnLine_Reindented() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class B
        {
            public B(int a, int b)
            {
            }
        }

        public class C : B
        {
            public C(int a,
                int b
                    ) : base(
                a, b
                    )
            {
            }

            public int M(int a, int b) => a;

            public void Run()
            {
                M
                    (1,
                    2
                    );
                M(M(
                    1, 2
                        ), M(
                    3, 4));
            }
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line\n",
        "BRO1105",
        "BRO1109",
        "BRO1110");

    // Trial 20: a '//' comment, a blank line, then an attributed override without docs. BRO1506 first removed the blank line,
    // BRO1601 first hid it behind the new '///' (BRO1506 then saw a comment below the comment): now both remove it.
    [Fact]
    public Task InheritDoc_BelowACommentAndABlankLine() => AssertConvergesInEveryOrderAsync(
        """
        /// <summary>A.</summary>
        public class C
        {
            // Explains the override below.

            [System.Obsolete("x")]
            public override string ToString() => "c";
        }
        """,
        "BRO1506",
        "BRO1513",
        "BRO1601");

    // Ocelot's FileAggregateRoute: BRO1601 inserting '<inheritdoc/>' before BRO1001's sort left the blank lines it added
    // with the positions, so the result depended on the order.
    [Fact]
    public Task InheritDoc_OnPropertiesTheSortMoves() => AssertConvergesInEveryOrderAsync(
        """
        public interface IRoute
        {
            /// <summary>Gets or sets the priority.</summary>
            int Priority { get; set; }

            /// <summary>Gets or sets the host.</summary>
            string Host { get; set; }
        }

        public class Route : IRoute
        {
            public string Aggregator { get; set; }
            public int Priority { get; set; } = 1;
            public string Keys { get; set; }
            public string Host { get; set; }
            public string Path { get; set; }

            public Route()
            {
                Aggregator = default;
            }
        }
        """,
        "BRO1001",
        "BRO1505",
        "BRO1513",
        "BRO1601");

    // Serilog's DummyWithLevelSwitchSink: the sort put the constructor between a field and the comment below it, and the
    // comment then wanted other blank lines on the next run.
    [Fact]
    public Task CommentBelowAField_TheSortDoesntSeparateThem() => AssertConvergesInEveryOrderAsync(
        """
        using System;
        using System.Collections.Generic;

        public class Sink
        {
            public Sink(int level)
            {
                Level = level;
            }

            [ThreadStatic]
            public static int Level;

            [ThreadStatic]
            // ReSharper disable ThreadStaticFieldHasInitializer
            public static List<int> Emitted = new List<int>();
            // ReSharper restore ThreadStaticFieldHasInitializer

            public void Emit(int value)
            {
                Emitted.Add(value);
            }
        }
        """,
        "BRO1001",
        "BRO1504",
        "BRO1505",
        "BRO1506");

    // eShop's RedisBasketRepository: a comment below a field, then a blank line, describes the field; nothing moves it to
    // the method below.
    [Fact]
    public Task CommentBelowAField_StaysWithIt() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public static int Count;

            private static int prefix = 1;
            // note on the prefix
            // and more

            public static int Key(string id) => prefix + id.Length;
            private int x;

            public void M()
            {
                x++;
                // last in the block

            }
        }
        """,
        "BRO1001",
        "BRO1504",
        "BRO1505",
        "BRO1506",
        "BRO1518");

    // same_line joins at max_line_length: the length is judged on the line as the other fixes leave it. Each case sits
    // right at the limit: the join fits before the other fix and not after it, or the other way round.
    // BRO1404 adds 'private ': '    private C(int a) : base(a)' (30) is too long, '    C(int a) : base(a)' (22) wasn't.
    [Fact]
    public Task ConstructorInitializerSameLine_AtTheLimit_WithAnAddedModifier() => AssertConvergesInEveryOrderWithConfigAsync(
        BaseClass + """
        public class C : B
        {
            C(int a)
                : base(a)
            {
            }
        }
        """,
        "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 25\n",
        "BRO1105",
        "BRO1404");

    // BRO1108 splits the parameters: '        long d) : base(a)' (25) fits, '        long c, long d) : base(a)' (33) didn't.
    [Fact]
    public Task ConstructorInitializerSameLine_AtTheLimit_WithSplitParameters() => AssertConvergesInEveryOrderWithConfigAsync(
        BaseClass + """
        public class C : B
        {
            public C(int a, long b,
                long c, long d)
                : base(a)
            {
            }
        }
        """,
        "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 28\n",
        "BRO1105",
        "BRO1107",
        "BRO1108");

    // BRO1109 moves '(' up: '        long a) : base(1)' (25) fits, '        (long a) : base(1)' (26) didn't.
    [Fact]
    public Task ConstructorInitializerSameLine_AtTheLimit_WithAMovedOpeningParenthesis() => AssertConvergesInEveryOrderWithConfigAsync(
        BaseClass + """
        public class C : B
        {
            public C
                (long a)
                : base(1)
            {
            }
        }
        """,
        "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 25\n",
        "BRO1105",
        "BRO1109");

    // BRO1110's own_line mode moves ')' down: '    ) : base(a)' (15) fits, '        int a, int b) : base(a)' (31) didn't.
    [Fact]
    public Task ConstructorInitializerSameLine_AtTheLimit_WithAClosingParenthesisMovedDown() => AssertConvergesInEveryOrderWithConfigAsync(
        BaseClass + """
        public class C : B
        {
            public C(
                int a, int b)
                : base(a)
            {
            }
        }
        """,
        "stylebro_constructor_initializer_placement = same_line\nstylebro_closing_parenthesis_placement = own_line\nmax_line_length = 15\n",
        "BRO1105",
        "BRO1110");

    // BRO1404/BRO1007 add 'internal '/'private '/'public ' (another part's): each joined line fits only without it.
    [Fact]
    public Task ConstraintSameLine_AtTheLimit_WithAddedModifiers() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        class Box<T>
            where T : class
        {
            void M<U>(U v)
                where U : class
            {
            }
        }

        partial class P<T>
            where T : class
        {
        }

        public partial class P<T>
        {
        }
        """,
        "stylebro_constraint_placement = same_line\nmax_line_length = 36\n",
        "BRO1007",
        "BRO1111",
        "BRO1404");

    // BRO1108 splits the parameters, BRO1109 moves '(' up: both joined lines fit only afterwards.
    [Fact]
    public Task ConstraintSameLine_AtTheLimit_WithSplitParametersAndAMovedOpeningParenthesis() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public static class C
        {
            public static void M<T>(int a, int b,
                T cc, T dd)
                where T : class
            {
            }

            public static void N<T>
                (T abcdefg)
                where T : class
            {
            }
        }
        """,
        "stylebro_constraint_placement = same_line\nmax_line_length = 34\n",
        "BRO1108",
        "BRO1109",
        "BRO1111");

    // BRO1149: a merge moves the inner body into the outer 'if' (and its else chain), which changes which brace rule
    // reports it; the merge adds the braces they want itself.
    // Flow.Launcher's ShellContextMenu: the '#endregion' after the outer 'if' kept BRO1149 quiet until BRO1113 removed it.
    [Fact]
    public Task NestedIfs_InRegions() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                #region Menu

                if (a)
                {
                    if (b)
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
        "BRO1113",
        "BRO1149");

    [Fact]
    public Task NestedIfs_WithTheBraceRules() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(bool a, bool b, bool c)
            {
                if (a)
                {
                    if (b)
                        M(a, b, c);
                }

                if (a)
                    if (b)
                    {
                        M(a, b, c);
                    }

                if (c)
                    M(a, b, c);
                else if (a)
                    if (b)
                        M(b, a, c);

                if (c)
                {
                    M(a, b, c);
                }
                else if (a)
                {
                    if (b)
                        M(b, a, c);
                }

                if (a)
                    if (b)
                        if (c)
                            M(c, b,
                                a);
            }
        }
        """,
        "BRO1149",
        "BRO1514",
        "BRO1515",
        "BRO1516");

    [Fact]
    public Task NestedIfs_InSingleLineBlocks() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(bool a, bool b) { if (a) { if (b) { M(b, a); } } }

            public void N(bool a, bool b)
            {
                if (a) { if (b) { M(b, a); } }
                if (b) { if (a) M(b, a); }
            }
        }
        """,
        "BRO1149",
        "BRO1508",
        "BRO1509",
        "BRO1514",
        "BRO1519");

    [Fact]
    public Task NestedIfs_InElseClauses() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int M(bool a, bool b, bool c)
            {
                if (c)
                {
                    return 1;
                }
                else
                {
                    if (a)
                    {
                        if (b)
                        {
                            M(a, b, c);
                        }
                    }
                }

                if (a)
                {
                    M(a, b, c);
                }
                else
                {
                    if (b)
                    {
                        if (c)
                        {
                            M(a, b, c);
                        }
                    }
                }

                return 0;
            }
        }
        """,
        "dotnet_diagnostic.BRO1143.severity = warning\n",
        "BRO1139",
        "BRO1143",
        "BRO1149");

    [Fact]
    public Task NestedIfs_WithParenthesesRules() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public void M(bool a, bool b, bool c, bool d)
            {
                if (a || b && c)
                {
                    if (d || c && a)
                    {
                        M(a, b, c, d);
                    }
                }

                if ((a))
                {
                    if ((b || c))
                    {
                        M(a, b, c, d);
                    }
                }
            }
        }
        """,
        "BRO1149",
        "BRO1405",
        "BRO1407");

    [Fact]
    public Task WhereBeforeTerminal_InSplitChains() => AssertConvergesInEveryOrderAsync(
        """
        using System.Linq;

        public class C
        {
            public int M(int[] items) => items.Select(i => i * 2)
                .Where(i => i > 0).Count();

            public bool N(int[] items) =>
                items
                    .Where(i => i > 0)
                    .Any();
        }
        """,
        "BRO1150",
        "BRO1523");

    [Fact]
    public Task ParamsArrays_WithArgumentLayout() => AssertConvergesInEveryOrderAsync(
        """
        public class C
        {
            public static int Sum(int first, params int[] rest) => first + rest.Length;

            public int M() => Sum(1,
                new[] { 2, 3 }) + Sum(1, 2,
                new[] { 3, 4 }) + Sum(
                1, new[] { 2 }) + Sum(1, new[] { 2, 3 });
        }
        """,
        "BRO1107",
        "BRO1108",
        "BRO1151");

    // xbox's AuthClient: 'using DevTools.Common;' became 'using Microsoft.Xbox.Services.DevTools.Common;' out of order, and
    // the next run's sort moved it.
    [Fact]
    public Task QualifiedUsings_WhereUsingsAreSorted() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        namespace Microsoft.Xbox.Services.DevTools.Common
        {
            public class Helper
            {
            }
        }

        namespace Microsoft.Xbox.Services.DevTools.Authentication
        {
            using System;
            using DevTools.Common;
            using Microsoft.Win32;

            internal class AuthClient
            {
                public Helper? H { get; set; }

                public Type? T { get; set; }

                public RegistryKey? K { get; set; }
            }
        }

        namespace Microsoft.Win32
        {
            public class RegistryKey
            {
            }
        }
        """,
        "dotnet_sort_system_directives_first = true\n",
        "BRO1126",
        SdkUsingSort);

    [Fact]
    public Task UsingPlacement_Outside_WithQualifiedUsingsAndBlankLines() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        namespace System.Tools
        {
            using IO;
            public class C
            {
                public Stream? M(bool b)
                {
                    if (b)
                    {
                        return null;
                    }
                    return Stream.Null;
                }
            }
        }
        """,
        "csharp_using_directive_placement = outside_namespace\n",
        "BRO1008",
        "BRO1126",
        "BRO1505",
        "BRO1519");

    // Newtonsoft.Json: '#region License' around the header. BRO1112 removes it; BRO1008 must not wait for that (a second
    // run), so region lines above the usings don't stop it.
    [Fact]
    public Task UsingPlacement_RegionAroundTheHeader() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        #region License
        // Copyright (c) 2007 James Newton-King
        #endregion

        using System;
        using System.Collections.Generic;

        namespace Newtonsoft.Json
        {
            public class C
            {
                public List<Type> Types { get; } = new List<Type>();
            }
        }
        """,
        "dotnet_diagnostic.BRO1112.severity = warning\ncsharp_using_directive_placement = inside_namespace\n",
        "BRO1008",
        "BRO1112",
        "BRO1505");

    // Newtonsoft.Json's TestObjects/Friend.cs: the usings above the '#region License' header.
    [Fact]
    public Task UsingPlacement_RegionBelowTheUsings() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        using System;
        using System.Collections.Generic;
        #region License
        // Copyright (c) 2007 James Newton-King
        #endregion

        namespace Newtonsoft.Json
        {
            public class C
            {
                public List<Type> Types { get; } = new List<Type>();
            }
        }
        """,
        "dotnet_diagnostic.BRO1112.severity = warning\ncsharp_using_directive_placement = inside_namespace\n",
        "BRO1008",
        "BRO1112",
        "BRO1504",
        "BRO1505",
        "BRO1506");

    [Fact]
    public Task UsingPlacement_Outside_RegionAroundTheHeader() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        #region License
        // Copyright (c) 2007 James Newton-King
        #endregion

        namespace Newtonsoft.Json
        {
            using System;
            using System.Collections.Generic;

            public class C
            {
                public List<Type> Types { get; } = new List<Type>();
            }
        }
        """,
        "dotnet_diagnostic.BRO1112.severity = warning\ncsharp_using_directive_placement = outside_namespace\n",
        "BRO1008",
        "BRO1112",
        "BRO1505");

    [Fact]
    public Task UsingPlacement_Inside_TheFileHeaderStaysOnTop() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        using System;
        namespace App
        {
            public class C
            {
                public Type? T { get; set; }
            }
        }
        """,
        "csharp_using_directive_placement = inside_namespace\nstylebro_file_header_company = Contoso\n",
        "BRO1008",
        "BRO1505",
        "BRO1615");

    [Fact]
    public Task UsingPlacement_Outside_TheFileHeaderStaysOnTop() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        namespace App;
        using System;
        public class C
        {
            public Type? T { get; set; }
        }
        """,
        "csharp_using_directive_placement = outside_namespace\nstylebro_file_header_company = Contoso\n",
        "BRO1008",
        "BRO1505",
        "BRO1615");

    // Trial 24 (BRO1505 off): a one-line property BRO1509 expands gets BRO1519's blank line after its '}'; sorted after
    // that, the blank line stayed with the slot (between two fields) and the property's new neighbour had none.
    [Fact]
    public Task Sort_ExpandedBlocks_BRO1519sBlankLineFollowsTheBrace() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        namespace Lib
        {
            public class Store
            {
                private int count;
                public int Value { get { return count; } }
                private int other;
                public int Add() { other++; return count++; }
            }
        }
        """,
        "csharp_prefer_braces = when_multiline\ncsharp_preserve_single_line_statements = true\ndotnet_diagnostic.BRO1505.severity = none\n",
        "BRO1001",
        "BRO1509",
        "BRO1519");

    // Trial 24 (Dawnshard, CSharpier mode: BRO1505 off): the sort put a property right below a constructor's '}' with no
    // blank line, which BRO1519 added on the next run.
    [Fact]
    public Task Sort_AMemberBelowAConstructor_GetsBRO1519sBlankLine() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class Controller
        {
            private readonly int a;
            private int Doubled => this.a * 2;
            public Controller(int a)
            {
                this.a = a;
            }

            public int Sum() => this.Doubled;
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none\n",
        "BRO1001",
        "BRO1519");

    // Trial 25: with BRO1505 off, BRO1509's expansion added BRO1505's blank lines anyway, and the sort then moved one.
    [Fact]
    public Task Sort_ExpandedOneLineType_WithoutBro1505() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class Tiny { public void A() { } private int x; public int X => x; }
        """,
        "dotnet_diagnostic.BRO1505.severity = none\n",
        "BRO1001",
        "BRO1509",
        "BRO1519");

    // Trial 25 (MimeKit): the formatter keeps 'default (S)' with the space option, so one run is enough.
    [Fact]
    public Task DefaultValue_WithASpaceBeforeCallParentheses() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        struct S { }

        class C
        {
            S field = new S ();
        }
        """,
        "csharp_space_between_method_call_name_and_opening_parenthesis = true\n",
        "BRO1104",
        SdkFormatter);

    // The same with braces that stay on their line (an auto-property's, an empty body kept by the option): no blank line.
    [Theory]
    [InlineData("dotnet_diagnostic.BRO1505.severity = none\n")]
    [InlineData("dotnet_diagnostic.BRO1505.severity = none\nstylebro_allow_empty_single_line_blocks = true\n")]
    public Task Sort_ExpandedOneLineType_WithoutBro1505_OneLineBraces(string editorConfig) => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class Tiny { public int A { get; set; } public void M() { } private int x; public int B { get; set; } }
        """,
        editorConfig,
        "BRO1001",
        "BRO1509",
        "BRO1519");

    // The .NET runtime's style (Microsoft style gaps G1-G3): fields first, 's_' statics, internal fields named like private
    // ones. The sort and the renames converge in every order.
    [Fact]
    public Task MicrosoftStyle_FieldsFirstAndStaticPrefixes() => AssertConvergesInEveryOrderWithConfigAsync(
        """
        public class C
        {
            public int Get() => count + m_old + Total;
            private static int count;
            private static int m_old;

            internal int Total;
        }
        """,
        "stylebro_member_order_scope = fields_first\nstylebro_private_field_naming = _camelCase\nstylebro_private_static_field_naming = s_camelCase\nstylebro_private_mutable_static_field_naming = s_camelCase\nstylebro_internal_field_naming = like_private\n",
        "BRO1001",
        "BRO1303",
        "BRO1307",
        "BRO1505");

    /// <summary>
    /// Stands for the SDK's formatter (IDE0055, the whitespace pass of 'dotnet format') among a case's ids: a fix whose
    /// result the formatter changes back makes every run change the file.
    /// </summary>
    private const string SdkFormatter = "IDE0055";

    /// <summary>Stands for 'dotnet format' sorting usings (its style pass, when a sort key is set).</summary>
    private const string SdkUsingSort = "SortUsings";

    private static Task AssertConvergesInEveryOrderAsync(string source, params string[] ids) =>
        AssertConvergesInEveryOrderWithConfigAsync(source, null, ids);

    private static Task AssertConvergesInEveryOrderWithConfigAsync(string source, string? editorConfig, params string[] ids) =>
        AssertConvergesInEveryOrderCoreAsync(source, editorConfig, true, ids);

    private static async Task AssertConvergesInEveryOrderCoreAsync(string source, string? editorConfig, bool sameText, params string[] ids)
    {
        string? first = null;
        foreach (var order in Orders(ids))
        {
            var document = CreateDocument(source, editorConfig is null ? null : "root = true\n\n[*.cs]\n" + editorConfig);
            foreach (var id in order)
            {
                if (id is SdkFormatter or SdkUsingSort)
                {
                    document = await RunSdkAsync(document, id);
                    continue;
                }

                var analyzers = FindAnalyzer(id);
                var diagnostics = await GetDiagnosticsAsync(document, analyzers, id);
                if (diagnostics.Length > 0)
                {
                    document = await FixAllAsync(document, FindCodeFix(id), analyzers, id, diagnostics);
                }
            }

            foreach (var id in ids)
            {
                if (id is SdkFormatter or SdkUsingSort)
                {
                    var formatted = (await (await RunSdkAsync(document, id)).GetTextAsync()).ToString();
                    Assert.True(formatted == (await document.GetTextAsync()).ToString(), $"Order {string.Join(", ", order)} leaves formatting:\n{await document.GetTextAsync()}\nformatted:\n{formatted}");
                    continue;
                }

                var left = await GetDiagnosticsAsync(document, FindAnalyzer(id), id);
                Assert.True(left.Length == 0, $"Order {string.Join(", ", order)} leaves {id}:\n{await document.GetTextAsync()}");
            }

            // Every order gives the same text, not only a clean one.
            var final = (await document.GetTextAsync()).ToString();
            first ??= final;
            Assert.True(!sameText || first == final, $"Order {string.Join(", ", order)} gives other text:\n{final}\nthan the first order:\n{first}");
        }
    }

    private static Task<Microsoft.CodeAnalysis.Document> RunSdkAsync(Microsoft.CodeAnalysis.Document document, string id) => id == SdkFormatter
        ? Microsoft.CodeAnalysis.Formatting.Formatter.FormatAsync(document)
        : Microsoft.CodeAnalysis.Formatting.Formatter.OrganizeImportsAsync(document, CancellationToken.None);

    private static IEnumerable<string[]> Orders(string[] ids) => ids.Length <= 1
        ? new[] { ids }
        : ids.SelectMany(first => Orders(ids.Where(id => id != first).ToArray()).Select(rest => rest.Prepend(first).ToArray()));
}
