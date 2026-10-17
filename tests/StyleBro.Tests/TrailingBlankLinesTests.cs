using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.TrailingBlankLinesAnalyzer, StyleBro.CodeFixes.Layout.TrailingBlankLinesCodeFixProvider>;

namespace StyleBro.Tests;

public class TrailingBlankLinesTests
{
    [Fact]
    public Task BlankLinesAfterComments_AreRemoved() => VerifyFixAsync(
        """
        using System;
        {|BRO1506:// After the usings.|}

        namespace N
        {
            class C
            {
                // Before a member: right after '{', standalone.

                void M()
                {
                    // Before a directive, right after '{': standalone.

        #if !NEVER
                    M();
        #endif
                    {|BRO1506:// Last in the block.|}

                }
            }
        }
        """,
        """
        using System;
        // After the usings.
        namespace N
        {
            class C
            {
                // Before a member: right after '{', standalone.

                void M()
                {
                    // Before a directive, right after '{': standalone.

        #if !NEVER
                    M();
        #endif
                    // Last in the block.
                }
            }
        }
        """);

    [Fact]
    public Task StyleCopExceptions_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        // Copyright (c) Example.
        // Licensed under MIT.

        namespace N
        {
            class C
            {
                void M()
                {
                    M(); // trailing

                    ////commented out code

                    M();
                    // followed by another comment

                    // the other comment
                    M();
                    // followed by a block comment

                    /* block */
                    M();
                }
            }
        }
        """);

    [Fact]
    public Task BlankLinesAtTheEndOfTheFile_AreRemoved()
    {
        var source = "class C\n{\n}{|BRO1507:|}\n\n\n";
        var fixedSource = "class C\n{\n}\n";
        return VerifyFixAsync(source, fixedSource);
    }

    [Fact]
    public Task WhitespaceLineAtTheEnd_IsRemoved()
    {
        var source = "class C\n{\n}{|BRO1507:|}\n    \n";
        var fixedSource = "class C\n{\n}\n";
        return VerifyFixAsync(source, fixedSource);
    }

    [Fact]
    public Task EndingWithOneOrNoLineBreak_NoDiagnostic() => Task.WhenAll(
        VerifyNoDiagnosticsAsync("class C\n{\n}\n"),
        VerifyNoDiagnosticsAsync("class C\n{\n}"),
        VerifyNoDiagnosticsAsync("class C\n{\n}\n// the end\n"),
        VerifyNoDiagnosticsAsync("class C\n{\n}\n// the end"));

    [Fact]
    public Task CommentAtTheEnd_LosesItsBlankLines() => VerifyFixAsync(
        "class C\n{\n}\n{|BRO1506:// the end|}{|BRO1507:|}\n\n",
        "class C\n{\n}\n// the end\n");

    // eShop's RedisBasketRepository: a note below a field, then a blank line, describes the field.
    [Fact]
    public Task ACommentBelowCode_KeepsTheBlankLineBelowIt() => VerifyFixAsync(
        """
        class C
        {
            private static int prefix = 1;
            // note on the prefix
            // and more

            private static int Key() => prefix;

            void M()
            {
                // a comment right after '{': standalone

                M();
            }

            void P()
            {
                P();
                {|BRO1506:// last in the block: BRO1518 would remove the blank line anyway|}

            }

            void N()
            {
            }
            {|BRO1506:// below a member ending in a brace|}

            int O() => 2;
        }
        """,
        """
        class C
        {
            private static int prefix = 1;
            // note on the prefix
            // and more

            private static int Key() => prefix;

            void M()
            {
                // a comment right after '{': standalone

                M();
            }

            void P()
            {
                P();
                // last in the block: BRO1518 would remove the blank line anyway
            }

            void N()
            {
            }
            // below a member ending in a brace
            int O() => 2;
        }
        """);

    // Trial 21 (AzureEventGridSimulator; owner's choice 2026-10-14): a comment with a blank line above and below stands on
    // its own (a note on the code above, a section header), so its blank line stays. Right below '{' too (trial 27: a
    // block's first section header lost its blank line, the others kept theirs); before '}' it's still reported;
    // stylebro_blank_line_below_standalone_comments = remove reports it too, like SA1512.
    [Fact]
    public Task StandaloneComments_KeepTheirBlankLine() => VerifyFixAsync(
        """
        class C
        {
            private int a;

            // Properties

            public int A => a;

            #region Methods
            // Methods
            // more

            #endregion
            void M()
            {
                // below a brace

                M();

                {|BRO1506:// before a brace|}

            }
        }
        """,
        """
        class C
        {
            private int a;

            // Properties

            public int A => a;

            #region Methods
            // Methods
            // more

            #endregion
            void M()
            {
                // below a brace

                M();

                // before a brace
            }
        }
        """);

    [Fact]
    public Task StandaloneComments_WithRemove_LoseTheirBlankLine() => VerifyFixAsync(
        """
        class C
        {
            private int a;

            {|BRO1506:// Properties|}

            public int A => a;

            #region Methods
            // Methods
            {|BRO1506:// more|}

            #endregion
            void M()
            {
            }
        }
        """,
        """
        class C
        {
            private int a;

            // Properties
            public int A => a;

            #region Methods
            // Methods
            // more
            #endregion
            void M()
            {
            }
        }
        """,
        "stylebro_blank_line_below_standalone_comments = remove");
}
