using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.CommentTextAnalyzer, StyleBro.CodeFixes.Readability.CommentTextCodeFixProvider>;

namespace StyleBro.Tests;

public class CommentTextTests
{
    [Fact]
    public Task DocumentationLines_GetOneSpace() => VerifyFixAsync("""
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
        """, """
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

    [Fact]
    public Task EmptyComments_AtTheEndsOfAGroup_AreRemoved() => VerifyFixAsync("""
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
        """, """
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