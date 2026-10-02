using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.QueryLayoutAnalyzer, StyleBro.CodeFixes.Readability.QueryLayoutCodeFixProvider>;

namespace StyleBro.Tests;

public class QueryLayoutTests
{
    [Fact]
    public Task BlankLinesBetweenClauses_AreRemoved() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                from x in items

                {|BRO1127:where|} x > 0
                select x;
        }
        """,
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                from x in items
                where x > 0
                select x;
        }
        """);

    [Fact]
    public Task MixedLayout_PutsEachClauseOnItsOwnLine() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        class C
        {
            void M(int[] items)
            {
                var q = {|BRO1128:from|} x in items where x > 0 orderby x
                    select x;
            }
        }
        """,
        """
        using System.Linq;

        class C
        {
            void M(int[] items)
            {
                var q = from x in items
                    where x > 0
                    orderby x
                    select x;
            }
        }
        """);

    [Fact]
    public Task ClausesAroundAMultiLineClause_StartOnTheirOwnLines() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                from x in items {|BRO1130:where|} x > 0 &&
                    x < 10 {|BRO1129:select|} x;
        }
        """,
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                from x in items
                where x > 0 &&
                    x < 10
                select x;
        }
        """);

    [Fact]
    public Task Continuations_AreLaidOutLikeClauses() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                {|BRO1128:from|} x in items
                group x by x % 2 into g select g.Key;
        }
        """,
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                from x in items
                group x by x % 2 into g
                select g.Key;
        }
        """);

    [Fact]
    public Task MultiLineClauseInAMixedQuery_IsFixedInOnePass() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                {|BRO1128:from|} x in items where x > 0 {|BRO1130:orderby|} x.ToString()
                    .Length
                select x;
        }
        """,
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                from x in items
                where x > 0
                orderby x.ToString()
                    .Length
                select x;
        }
        """);

    [Fact]
    public Task QueryInParentheses_KeepsTheParenthesisIndentation() => Verify.VerifyFixAsync(
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                ({|BRO1128:from|} x in items where x > 0
                select x).ToList();
        }
        """,
        """
        using System.Linq;

        class C
        {
            object M(int[] items) =>
                (from x in items
                where x > 0
                select x).ToList();
        }
        """);

    [Fact]
    public Task ConsistentLayoutsAndCommentedGaps_AreNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        using System.Linq;

        class C
        {
            object OneLine(int[] items) => from x in items where x > 0 select x;

            object OwnLines(int[] items) =>
                from x in items
                where x > 0
                select x;

            object Commented(int[] items) =>
                from x in items /* positive */ where x > 0
                select x;

            object CommentBeforeMultiLine(int[] items) =>
                from x in items /* small */ where x > 0 &&
                    x < 10
                select x;

            object CommentAndPlain(int[] items) =>
                from x in items /* positive */ where x > 0 orderby x
                select x;

            object CommentLine(int[] items) =>
                from x in items

                // only positive ones

                where x > 0
                select x;
        }
        """);
}
