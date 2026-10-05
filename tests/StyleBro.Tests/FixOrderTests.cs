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
    public Task Braces_InASingleLineBlock() => AssertConvergesInEveryOrderAsync(
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
        public class C // note
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
        public class C // note
        {

            private int x;

            public int M() // why
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

    private static async Task AssertConvergesInEveryOrderAsync(string source, params string[] ids)
    {
        foreach (var order in Orders(ids))
        {
            var document = CreateDocument(source, null);
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
        }
    }

    private static IEnumerable<string[]> Orders(string[] ids) => ids.Length <= 1
        ? new[] { ids }
        : ids.SelectMany(first => Orders(ids.Where(id => id != first).ToArray()).Select(rest => rest.Prepend(first).ToArray()));
}
