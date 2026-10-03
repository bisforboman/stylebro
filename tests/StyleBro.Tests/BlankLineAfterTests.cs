using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.BlankLineAfterAnalyzer, StyleBro.CodeFixes.Layout.BlankLineAfterCodeFixProvider>;

namespace StyleBro.Tests;

public class BlankLineAfterTests
{
    [Fact]
    public Task BlankLinesAfterOpeningBraces_AreRemoved() => VerifyFixAsync(
        """
        namespace N
        {|BRO1503:{|}

            class C
            {|BRO1503:{|}


                int[] a = new[]
                {|BRO1503:{|}

                    1,
                };
            }
        }
        """,
        """
        namespace N
        {
            class C
            {
                int[] a = new[]
                {
                    1,
                };
            }
        }
        """);

    [Fact]
    public Task BraceFollowedByCommentOrStringContent_NoDiagnostic() => VerifyNoDiagnosticsAsync(""""
        class C
        { // a comment on the brace line

            string M() => """
                {

                text
                """;
        }
        """");

    [Fact]
    public Task CommentsAfterCode_GetABlankLineAbove() => VerifyFixAsync(
        """
        class C
        {
            int a;
            {|BRO1504:// after a field|}
            int b;

            void M(int x)
            {
                var y = x; // trailing
                {|BRO1504:// after code with a trailing comment|}
                if (x > 0)
                {
                }
                {|BRO1504:// after a closing brace|}
            label:
                {|BRO1504:// after a label|}
                return;
            }
        }
        """,
        """
        class C
        {
            int a;

            // after a field
            int b;

            void M(int x)
            {
                var y = x; // trailing

                // after code with a trailing comment
                if (x > 0)
                {
                }

                // after a closing brace
            label:

                // after a label
                return;
            }
        }
        """);

    [Fact]
    public Task StyleCopExceptions_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            // first in a block
            void M(int x)
            {
                var y = x;

                // after a blank line
                // second comment line
                switch (x)
                {
                    case 1:
                        // after a case label
                        break;
                    default:
                        // after default
                        break;
                }

                y++;
                ////commented out code
        #if !NEVER
                // after a directive
        #endif
            }
        }
        """);

    [Fact]
    public Task CommentBeforeOpenBrace_IsLeftToBro1132() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(bool a)
            {
                if (a)
                // moved into the block by BRO1132
                {
                }
            }
        }
        """);

    [Fact]
    public Task CommentBeforeOpenBrace_WithBro1132Off_GetsTheBlankLine() => VerifyFixAsync(
        """
        class C
        {
            void M(bool a)
            {
                if (a)
                {|BRO1504:// stays here|}
                {
                }
            }
        }
        """,
        """
        class C
        {
            void M(bool a)
            {
                if (a)

                // stays here
                {
                }
            }
        }
        """,
        "dotnet_diagnostic.BRO1132.severity = none\n");
}
