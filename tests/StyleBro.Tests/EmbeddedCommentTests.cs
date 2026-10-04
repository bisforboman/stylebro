using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmbeddedCommentAnalyzer, StyleBro.CodeFixes.Readability.EmbeddedCommentCodeFixProvider>;

namespace StyleBro.Tests;

public class EmbeddedCommentTests
{
    [Fact]
    public Task CommentsAfterTheHeader_MoveIntoTheBlock() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            public int M(int x, object o, int[] items)
            {
                if (x == 0) {|BRO1132:// zero|}
                {
                    return 0;
                }
                else if (x == 1) {|BRO1132:// one|}
                {
                    return 1;
                }
                else {|BRO1132:// anything else|}
                {
                    x++;
                }

                while (x > 10) {|BRO1132:/* shrink */|} {
                    x--;
                }

                foreach (var i in items) {|BRO1132:// each|}
                {
                }

                for (var i = 0; i < 2; i++) {|BRO1132:// twice|}
                {
                    x += i;
                }

                do {|BRO1132:// at least once|}
                {
                    x--;
                }
                while (x > 5);

                lock (o) {|BRO1132:// guarded|}
                {
                    x++;
                }

                try {|BRO1132:// risky|}
                {
                    x = checked(x * 2);
                }
                catch (OverflowException) {|BRO1132:// too big|}
                {
                    x = 0;
                }
                finally {|BRO1132:// always|}
                {
                    x++;
                }

                checked {|BRO1132:// no overflow|}
                {
                    x++;
                }

                unchecked {|BRO1132:// overflow ok|}
                {
                    x++;
                }

                switch (x) {|BRO1132:// by value|}
                {
                    case 1:
                        return 1;
                }

                return x;
            }
        }
        """,
        """
        using System;

        public class C
        {
            public int M(int x, object o, int[] items)
            {
                if (x == 0)
                {
                    // zero
                    return 0;
                }
                else if (x == 1)
                {
                    // one
                    return 1;
                }
                else
                {
                    // anything else
                    x++;
                }

                while (x > 10) {
                    /* shrink */
                    x--;
                }

                foreach (var i in items)
                {
                    // each
                }

                for (var i = 0; i < 2; i++)
                {
                    // twice
                    x += i;
                }

                do
                {
                    // at least once
                    x--;
                }
                while (x > 5);

                lock (o)
                {
                    // guarded
                    x++;
                }

                try
                {
                    // risky
                    x = checked(x * 2);
                }
                catch (OverflowException)
                {
                    // too big
                    x = 0;
                }
                finally
                {
                    // always
                    x++;
                }

                checked
                {
                    // no overflow
                    x++;
                }

                unchecked
                {
                    // overflow ok
                    x++;
                }

                switch (x)
                {
                    // by value
                    case 1:
                        return 1;
                }

                return x;
            }
        }
        """);

    [Fact]
    public Task CommentsOnTheirOwnLines_AndSeveral_KeepTheirOrder() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a) {|BRO1132:/* first */|} {|BRO1132:// second|}
                {|BRO1132:// third|}
                {
                    // already inside
                    M(b, a);
                }

                if (b)
                    {|BRO1132:/* own line */|} {
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
                if (a)
                {
                    /* first */
                    // second
                    // third
                    // already inside
                    M(b, a);
                }

                if (b)
                    {
                    /* own line */
                    M(a, b);
                }
            }
        }
        """);

    [Fact]
    public Task NestedBlocks_AreFixedInOnePass() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a, bool b)
            {
                if (a) {|BRO1132:// outer|}
                {
                    if (b) {|BRO1132:// inner|}
                    {
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
                if (a)
                {
                    // outer
                    if (b)
                    {
                        // inner
                    }
                }
            }
        }
        """);

    [Fact]
    public Task EmptyBlock_UsesTheIndentationSetting() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool a)
            {
                if (a) {|BRO1132:// nothing yet|}
                {
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool a)
            {
                if (a)
                {
                  // nothing yet
                }
            }
        }
        """,
        "indent_size = 2");

    [Fact]
    public Task MultiLineHeaders_AreSkipped_EachClauseJudgedOnItsOwn() => VerifyFixAsync(
        """
        public class C
        {
            public int M(string path, bool a, bool b)
            {
                if (a
                    && !string.IsNullOrEmpty(path) // path must exist
                    && path.StartsWith("http")) // scheme must be HTTP
                {
                    // all conditions met
                    return 1;
                }
                else if (b
                    && a) // both
                {
                    return 2;
                }
                else {|BRO1132:// neither|}
                {
                    return 3;
                }
            }

            public void N(int x)
            {
                while (x > 0
                    && x < 10) // in range
                {
                    x--;
                }

                switch (x
                    + 1) // shifted
                {
                    default:
                        break;
                }

                try
                {
                    x++;
                }
                catch (System.Exception e)
                    when (e.Message.Length > 0) // with a message
                {
                    x = 0;
                }
            }
        }
        """,
        """
        public class C
        {
            public int M(string path, bool a, bool b)
            {
                if (a
                    && !string.IsNullOrEmpty(path) // path must exist
                    && path.StartsWith("http")) // scheme must be HTTP
                {
                    // all conditions met
                    return 1;
                }
                else if (b
                    && a) // both
                {
                    return 2;
                }
                else
                {
                    // neither
                    return 3;
                }
            }

            public void N(int x)
            {
                while (x > 0
                    && x < 10) // in range
                {
                    x--;
                }

                switch (x
                    + 1) // shifted
                {
                    default:
                        break;
                }

                try
                {
                    x++;
                }
                catch (System.Exception e)
                    when (e.Message.Length > 0) // with a message
                {
                    x = 0;
                }
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(bool a, int[] items)
            {
                // above is fine
                if (a)
                ////if (!a)
                {
                    // inside is fine
                }

                if (a)
                { // after the brace is fine
                }

                // Not one of StyleCop's statements.
                using (var d = new System.IO.MemoryStream()) // using
                {
                }

                foreach (var (x, y) in new[] { (1, 2) }) // deconstructing foreach
                {
                }

                { // a plain block
                }

                // Skipped: a single-line block (no line to put the comment on), a comment spanning lines, a directive.
                if (a) // single line
                { M(a, items); }

                if (a) /* two
                   lines */
                {
                }

                if (a) // directive
        #if DEBUG
                {
                }
        #else
                {
                }
        #endif

                if (a) // no block
                    M(a, items);
            }
        }
        """);
}
