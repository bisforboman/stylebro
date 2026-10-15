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
    public Task CommentContinuingATrailingComment_InItsColumn_NeedsNoBlankLine() => VerifyFixAsync(
        """
        class C
        {
            void M(int a)
            {
                System.Console.WriteLine(a); // This is because the identifier is hardwired, so it could not be
                                             // used to override another one.
                System.Console.WriteLine(a); // A comment that ends here.
                                                 {|BRO1504:// Not in its column: a comment of its own.|}
                System.Console.WriteLine(a);
            }
        }
        """,
        """
        class C
        {
            void M(int a)
            {
                System.Console.WriteLine(a); // This is because the identifier is hardwired, so it could not be
                                             // used to override another one.
                System.Console.WriteLine(a); // A comment that ends here.

                                                 // Not in its column: a comment of its own.
                System.Console.WriteLine(a);
            }
        }
        """);

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

    // A deviation from SA1515 (owner's decision, docs/decisions.md): a blank line would split the chain.
    [Fact]
    public Task CommentBetweenCallChainLinks_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        using System.Linq;

        class C
        {
            int? M(int[] items, string s)
            {
                var count = items
                    .Where(i => i > 0)
                    // only the first ones
                    // (two comment lines)
                    .Take(2)
                    .Count();
                return s
                    // may be null
                    ?.Length + count;
            }
        }
        """);

    // A deviation from SA1515 (owner's decision, docs/decisions): a blank line would split the list (Chickensoft's
    // GameDemo: a comment between two arguments).
    [Fact]
    public Task CommentBetweenListItems_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        using System;

        [AttributeUsage(
            AttributeTargets.Class,
            // one per class
            AllowMultiple = false)]
        class A : Attribute
        {
        }

        class C
        {
            int[] a = new[]
            {
                1,
                // two
                2,
            };

            int[] b =
            [
                1,
                // two
                2
                // (last)
            ];

            (int First,
                // the second
                int Second) t;

            int this[int a,
                // b
                int b] => M(a,
                    // b
                    b);

            int M(
                int a,
                // b
                int b)
            {
                var c = this[a,
                    // b
                    b];
                var t = (a,
                    // b
                    b);
                return a + b;
            }
        }
        """);

    // The exemption is for the gaps between items only: inside an item, the comment follows code as usual.
    [Fact]
    public Task CommentInsideAListItem_IsReported() => VerifyFixAsync(
        """
        class C
        {
            int M(int a, System.Func<int> f) => M(a, () =>
            {
                int x = a,
                    {|BRO1504:// y|}
                    y = a;
                if (
                    {|BRO1504:// x|}
                    x > y)
                {
                }

                // x
                return x;
            });
        }
        """,
        """
        class C
        {
            int M(int a, System.Func<int> f) => M(a, () =>
            {
                int x = a,

                    // y
                    y = a;
                if (

                    // x
                    x > y)
                {
                }

                // x
                return x;
            });
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

    [Theory]
    [InlineData("omit")]
    [InlineData("omit_after_block")]
    public Task CommentAboveASwitchSection_IsFine_WhenBro1526OmitsTheBlankLine(string mode) => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                    {
                        break;
                    }
                    // Two.
                    case 2:
                        break;
                }
            }
        }
        """,
        "stylebro_blank_line_between_switch_sections = " + mode + "\n");

    [Fact]
    public Task CommentAboveASwitchSection_IsReported_WhenOmitAfterBlockWantsTheBlankLine() => VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                        break;
                    {|BRO1504:// Two.|}
                    case 2:
                        break;
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                        break;

                    // Two.
                    case 2:
                        break;
                }
            }
        }
        """,
        "stylebro_blank_line_between_switch_sections = omit_after_block\n");
}
