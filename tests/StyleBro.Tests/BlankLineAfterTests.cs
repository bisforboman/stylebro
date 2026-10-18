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

    // jint: with the blank line above, the run would stand on its own and BRO1506 would leave the one below, while
    // BRO1506 first removed it: BRO1504's fix removes it too. Not when BRO1506 is off.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task CommentBelowCode_LosesTheBlankLineBelowIt_WhenBRO1506IsOn(bool on) => VerifyFixAsync(
        """
        class C
        {
            int M(bool x)
            {
                if (x)
                {
                    return 1;
                }
                {|BRO1504:// a note|}
                // and more

                return 0;
            }
        }
        """,
        $$"""
        class C
        {
            int M(bool x)
            {
                if (x)
                {
                    return 1;
                }

                // a note
                // and more
        {{(on ? string.Empty : "\n")}}        return 0;
            }
        }
        """,
        on ? null : "dotnet_diagnostic.BRO1506.severity = none\n");

    // Stryker: a comment between the parts of one expression explains a part; a blank line would split the expression.
    [Fact]
    public Task CommentInsideAnExpression_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        public static class C
        {
            public static int Pick(bool ok, int value) =>
                ok ? value
                    // if this happens, something is wrong
                    : throw new System.InvalidOperationException("no");

            public static bool Small(int x) =>
                x > 0
                // and not too big
                && x < 10;

            public static bool Kind(object o) => o is int
                // or text
                or string;
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

    // Pinta (trial 32, tab_width = 8): the formatter aligns a comment below a trailing comment to that comment's column as
    // shown, tabs counted as wide as tab_width. BRO1504 compared character offsets, put a blank line between the two, and
    // the next run's whitespace pass moved the separated comment to the field's indentation.
    [Fact]
    public Task CommentAlignedWithTabs_ContinuesTheTrailingComment() => VerifyNoDiagnosticsAsync(
        "class C\n{\n\tint tools = 0; // NRT - set in factory method\n\t\t       // Stores the button corresponding to each tool.\n\tint buttons = 1;\n}\n",
        "indent_style = tab\ntab_width = 8\nindent_size = 8");

    // The same lines with tabs 4 wide: the comment starts at column 15, the trailing one at 19, so it's a comment of its own.
    [Fact]
    public Task CommentAlignedWithTabs_OtherTabWidth_GetsABlankLine() => VerifyFixAsync(
        "class C\n{\n\tint tools = 0; // NRT - set in factory method\n\t\t       {|BRO1504:// Stores the button corresponding to each tool.|}\n\tint buttons = 1;\n}\n",
        "class C\n{\n\tint tools = 0; // NRT - set in factory method\n\n\t\t       // Stores the button corresponding to each tool.\n\tint buttons = 1;\n}\n",
        "indent_style = tab\ntab_width = 4\nindent_size = 4");

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
                var d = M(a,
                    b
                    // last
                    );
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

    // A comment between an element's attributes and the element stays with them (BRO1525 removes blank lines there).
    // An assembly attribute isn't the element's: the comment below it is reported like one below code.
    [Fact]
    public Task CommentBelowAnAttribute_NoDiagnostic_UnlessAnAssemblyAttribute() => VerifyFixAsync(
        """
        using System;

        [assembly: CLSCompliant(true)]
        {|BRO1504:// The class.|}
        class C
        {
            [Obsolete]
            // Kept for old callers.
            void M([In]
                // The value.
                int x)
            {
            }
        }

        class InAttribute : Attribute
        {
        }
        """,
        """
        using System;

        [assembly: CLSCompliant(true)]

        // The class.
        class C
        {
            [Obsolete]
            // Kept for old callers.
            void M([In]
                // The value.
                int x)
            {
            }
        }

        class InAttribute : Attribute
        {
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

    // Trial 25 (Flow Launcher): the line above is the statement's own header.
    [Fact]
    public Task CommentLeadingAnEmbeddedStatement_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            string M(string path, bool folder, int[] items)
            {
                if (folder)
                    // the separator is needed for folders
                    path += "/";
                else
                    // files keep it
                    path += "";
                foreach (var item in items)
                    // one by one
                    path += item;
                return path;
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
