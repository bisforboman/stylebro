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
                var analyzers = FindAnalyzer(id);
                var diagnostics = await GetDiagnosticsAsync(document, analyzers, id);
                if (diagnostics.Length > 0)
                {
                    document = await FixAllAsync(document, FindCodeFix(id), analyzers, id, diagnostics);
                }
            }

            foreach (var id in ids)
            {
                var left = await GetDiagnosticsAsync(document, FindAnalyzer(id), id);
                Assert.True(left.Length == 0, $"Order {string.Join(", ", order)} leaves {id}:\n{await document.GetTextAsync()}");
            }

            // Every order gives the same text, not only a clean one.
            var final = (await document.GetTextAsync()).ToString();
            first ??= final;
            Assert.True(!sameText || first == final, $"Order {string.Join(", ", order)} gives other text:\n{final}\nthan the first order:\n{first}");
        }
    }

    private static IEnumerable<string[]> Orders(string[] ids) => ids.Length <= 1
        ? new[] { ids }
        : ids.SelectMany(first => Orders(ids.Where(id => id != first).ToArray()).Select(rest => rest.Prepend(first).ToArray()));
}
