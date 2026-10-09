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
                {|BRO1506:// Before a member.|}


                void M()
                {
                    {|BRO1506:// Before a directive.|}

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
                // Before a member.
                void M()
                {
                    // Before a directive.
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
                M();

                {|BRO1506:// a comment above code|}

                M();
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
                M();

                // a comment above code
                M();
            }

            void N()
            {
            }
            // below a member ending in a brace
            int O() => 2;
        }
        """);
}
