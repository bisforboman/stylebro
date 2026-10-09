using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.CommentTextAnalyzer, StyleBro.CodeFixes.Readability.CommentTextCodeFixProvider>;

namespace StyleBro.Tests;

public class CommentTextTests
{
    [Fact]
    public Task DocumentationLines_GetOneSpace() => VerifyFixAsync(
        """
        /// <summary>
        ///{|BRO1005:|}Text.
        ///   Indented text and <para>tags</para> are fine.
        ///   <para>Nested tag.</para>
        /// <code>
        /// if (x)
        ///     y();
        /// </code>
        ///
        /// </summary>
        ///{|BRO1005:   |}<remarks>Top-level tag.</remarks>
        public class C
        {
        }
        """,
        """
        /// <summary>
        /// Text.
        ///   Indented text and <para>tags</para> are fine.
        ///   <para>Nested tag.</para>
        /// <code>
        /// if (x)
        ///     y();
        /// </code>
        ///
        /// </summary>
        /// <remarks>Top-level tag.</remarks>
        public class C
        {
        }
        """);

    // Fonts: removing the empty comment between two blank lines left two blank lines in a row (BRO1517).
    [Fact]
    public Task EmptyCommentsBetweenBlankLines_TakeTheBlankLinesBelowAlong() => VerifyFixAsync(
        """
        public class C
        {
            public int M()
            {
                // The number of glyphs.

                {|BRO1120://|}
                //

                // The format.
                int a = 1;

                {|BRO1120://|}
                int b = a;
                {|BRO1120://|}

                int c = b; {|BRO1120://|}

                return c;
            }
        }
        """,
        """
        public class C
        {
            public int M()
            {
                // The number of glyphs.

                // The format.
                int a = 1;

                int b = a;

                int c = b;

                return c;
            }
        }
        """);

    [Fact]
    public Task EmptyComments_AtTheEndsOfAGroup_AreRemoved() => VerifyFixAsync(
        """
        public class C
        {
            public int M()
            {
                {|BRO1120://|}
                // first paragraph
                //
                // second paragraph
                //
                {|BRO1120:/* */|}
                int a = 1; {|BRO1120://|}
                ////
                {|BRO1120://|}
                return a;
            }
        }
        """,
        """
        public class C
        {
            public int M()
            {
                // first paragraph
                //
                // second paragraph
                int a = 1;
                ////
                return a;
            }
        }
        """);
}
