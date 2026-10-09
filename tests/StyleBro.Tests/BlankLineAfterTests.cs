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
    public Task CommentAfterAnArrow_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        using System;

        class C
        {
            int M(int x) => x switch
            {
                2 =>
                    // quoting from somewhere (StyleCop #3392)
                    20,
                _ => 0,
            };

            Func<int> F() => () =>
                // a lambda body
                1;

            int P =>
                // an expression body
                1;
        }
        """);

    [Fact]
    public Task CommentAfterCollectionExpressionBracket_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            int[] a =
            [
                // first, like after an opening brace (StyleCop #3766)
                1,
            ];
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

    [Fact]
    public Task CommentBeforeADeclarationsOpenBrace_IsLeftToBro1134() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M()
            // moved into the body by BRO1134
            {
            }
        }
        """);

    [Fact]
    public Task CommentBeforeADeclarationsOpenBrace_WithBro1134Off_GetsTheBlankLine() => VerifyFixAsync(
        """
        class C
        {
            void M()
            {|BRO1504:// stays here|}
            {
            }
        }
        """,
        """
        class C
        {
            void M()

            // stays here
            {
            }
        }
        """,
        "dotnet_diagnostic.BRO1134.severity = none\n");

    [Fact]
    public Task ExemptPrefixes_ToolMarkersNeedNoBlankLine() => VerifyFixAsync(
        """
        class C
        {
            int M(int a)
            {
                a++;
                // ReSharper disable once UnusedVariable
                var b = a;
                a++;
                //@formatter:off
                a++;
                // @formatter:on
                a++;
                {|BRO1504:// Resharper is a different word: the prefixes are case-sensitive.|}
                a++;
                {|BRO1504:// A plain comment.|}
                return a;
            }
        }
        """,
        """
        class C
        {
            int M(int a)
            {
                a++;
                // ReSharper disable once UnusedVariable
                var b = a;
                a++;
                //@formatter:off
                a++;
                // @formatter:on
                a++;

                // Resharper is a different word: the prefixes are case-sensitive.
                a++;

                // A plain comment.
                return a;
            }
        }
        """,
        "stylebro_comment_blank_line_exempt_prefixes = ReSharper, @formatter,\n");

    // eShop's RedisBasketRepository: a note below a field, then a blank line, describes the field.
    [Fact]
    public Task ACommentBelowCode_FollowedByABlankLine_IsNotReported() => VerifyFixAsync(
        """
        class C
        {
            private static int prefix = 1;
            // note on the prefix
            // and more

            private static int Key() => prefix;

            void M()
            {
                var x = 1;
                {|BRO1504:// no blank line after this one|}
                x++;
                {|BRO1504:// nor after this one, at the end|}
            }
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
                var x = 1;

                // no blank line after this one
                x++;

                // nor after this one, at the end
            }
        }
        """);
}
