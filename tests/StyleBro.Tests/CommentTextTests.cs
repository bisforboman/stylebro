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

    // Trial 21 (AzureEventGridSimulator): a lone '//' in an empty block marks it as empty on purpose. With other text in the
    // block, or code, it's reported as before.
    [Fact]
    public Task EmptyComments_ThatAreAllABlockHolds_AreKept() => VerifyFixAsync(
        """
        public class C
        {
            public void M()
            {
                try
                {
                    M();
                }
                catch
                {
                    //
                }

                try
                {
                    M();
                }
                catch { /* */ }

                if (true)
                {
                    // Nothing to do.
                    {|BRO1120://|}
                }

                if (true)
                {
                    M();
                    {|BRO1120://|}
                }
            }
        }
        """,
        """
        public class C
        {
            public void M()
            {
                try
                {
                    M();
                }
                catch
                {
                    //
                }

                try
                {
                    M();
                }
                catch { /* */ }

                if (true)
                {
                    // Nothing to do.
                }

                if (true)
                {
                    M();
                }
            }
        }
        """);

    // The file header's empty lines stay (owner's decision 2026-10-29; StyleCop's SA1120 reports them), with or without a
    // file_header_template (IDE0073 writes them from one starting with a line break, Ical.Net: 222 files).
    [Fact]
    public Task EmptyLinesInTheHeader_Stay() => VerifyFixAsync(
        """
        //
        // Copyright (c) Example Authors.
        // Licensed under the MIT license.
        //

        public class C
        {
            {|BRO1120://|}
            // Not the header.
            public int M() => 1;
        }
        """,
        """
        //
        // Copyright (c) Example Authors.
        // Licensed under the MIT license.
        //

        public class C
        {
            // Not the header.
            public int M() => 1;
        }
        """);

    // Trial 60, log4net: the Apache license header inside '#region Apache License' (325 files) is the header too.
    [Fact]
    public Task EmptyLinesInAHeaderBelowARegionLine_Stay() => VerifyFixAsync(
        """
        #region Apache License
        //
        // Licensed to the Apache Software Foundation (ASF).
        //
        #endregion

        {|BRO1120://|}
        // Not the header: code-less, but below the region.
        public class C
        {
        }
        """,
        """
        #region Apache License
        //
        // Licensed to the Apache Software Foundation (ASF).
        //
        #endregion

        // Not the header: code-less, but below the region.
        public class C
        {
        }
        """);
}
