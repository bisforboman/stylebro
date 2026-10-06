using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmbeddedCommentAnalyzer, StyleBro.CodeFixes.Readability.EmbeddedCommentCodeFixProvider>;

namespace StyleBro.Tests;

public class DeclarationCommentTests
{
    [Fact]
    public Task CommentsBetweenTheHeaderAndTheBrace_MoveIntoTheBody() => VerifyFixAsync(
        """
        using System;

        namespace N
        {|BRO1134:// the namespace|}
        {
            public interface I
            {|BRO1134:// the interface|}
            {
                int P { get; }
            }

            [Serializable] // an attribute's comment stays
            public class C : I
            {|BRO1134:// the class|}
            {
                private int value;

                public C()
                {|BRO1134:// the constructor|}
                {
                    this.value = 1;
                }

                public int P
                {|BRO1134:// the property|}
                {
                    get
                    {|BRO1134:// the getter|}
                    {
                        return this.value;
                    }
                }

                [Obsolete]
                public void M()
                {|BRO1134:/* the method */|}
                {
                    void Local()
                    {|BRO1134:// the local function|}
                    {
                    }

                    Local();
                }
            }

            public enum E
            {|BRO1134:// the enum|}
            {
                A,
            }
        }
        """,
        """
        using System;

        namespace N
        {
            // the namespace
            public interface I
            {
                // the interface
                int P { get; }
            }

            [Serializable] // an attribute's comment stays
            public class C : I
            {
                // the class
                private int value;

                public C()
                {
                    // the constructor
                    this.value = 1;
                }

                public int P
                {
                    // the property
                    get
                    {
                        // the getter
                        return this.value;
                    }
                }

                [Obsolete]
                public void M()
                {
                    /* the method */
                    void Local()
                    {
                        // the local function
                    }

                    Local();
                }
            }

            public enum E
            {
                // the enum
                A,
            }
        }
        """);

    [Fact]
    public Task CommentsOnTheirOwnLines_MoveToo() => VerifyFixAsync(
        """
        public class C
        {
            public void M()
            {|BRO1134:// first|}
            {|BRO1134:// second|}
            {
            }
        }
        """,
        """
        public class C
        {
            public void M()
            {
                // first
                // second
            }
        }
        """);

    [Fact]
    public Task BlankLinesAfterTheBrace_StayAboveTheComment() => VerifyFixAsync(
        """
        public class C
        {|BRO1134:// note|}
        {

            private int x;
        }
        """,
        """
        public class C
        {

            // note
            private int x;
        }
        """);

    [Fact]
    public Task StatementsStayBro1132s() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool b)
            {|BRO1134:// method|}
            {
                if (b) {|BRO1132:// statement|}
                {
                    M(false);
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool b)
            {
                // method
                if (b)
                {
                    // statement
                    M(false);
                }
            }
        }
        """);

    // The owner's decision (docs/decisions.md): a comment ending the header's line is a note on the header and stays.
    [Fact]
    public Task CommentEndingTheHeadersLine_Stays_OneOnItsOwnLineMoves() => VerifyFixAsync(
        """
        public class C // legacy name
        {
            public int GetHashCollissions() // legacy incorrect spelling, oops
            {
                return 0;
            }

            public int P /* note */
            {|BRO1134:// moves|}
            {
                get { return 0; }
            }

            public void M() {|BRO1134:/* before the brace on its line */|} {
                M();
            }
        }
        """,
        """
        public class C // legacy name
        {
            public int GetHashCollissions() // legacy incorrect spelling, oops
            {
                return 0;
            }

            public int P /* note */
            {
                // moves
                get { return 0; }
            }

            public void M() {
                /* before the brace on its line */
                M();
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            // A header spanning several lines: the comment explains its last line.
            public void M(
                int a,
                int b) // b is optional
            {
            }

            public class D<T>
                where T : class // reference types only
            {
            }

            // Code after '{' on its line.
            public void N() // one line
            { }

            // '////' is commented-out code; a directive between.
            public void O() //// O2();
            {
            }

            public void P()
        #if DEBUG
            // debug
        #endif
            {
            }

            // Expression bodies have no '{'.
            public int Q() => 1; // one
        }
        """);
}
