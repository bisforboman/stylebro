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
