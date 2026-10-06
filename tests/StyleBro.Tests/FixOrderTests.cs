using static StyleBro.Tests.DocExamplesTests;

namespace StyleBro.Tests;

/// <summary>
/// 'dotnet format' fixes one diagnostic id at a time (one Fix All each, on fresh diagnostics), in an order that isn't
/// fixed. One run must converge in every order: a fix that changes which rule reports another finding, or what another
/// rule wants, must take care of that itself. Each case runs every order of its ids and expects no diagnostic left.
/// </summary>
public class FixOrderTests
{
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
