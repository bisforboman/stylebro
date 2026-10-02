using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.BlankLineBeforeAnalyzer, StyleBro.CodeFixes.Layout.BlankLineBeforeCodeFixProvider>;

namespace StyleBro.Tests;

public class BlankLineBeforeTests
{
    [Fact]
    public Task OpeningBraces_BlankLinesAreRemoved() => VerifyFixAsync(
        """
        class C

        {|BRO1501:{|}
            int P

            {|BRO1501:{|}
                get { return 1; }
            }

            void M(bool b)

            {|BRO1501:{|}
                if (b)

                {|BRO1501:{|}
                }

                var a = new[]

                {|BRO1501:{|}
                    1,
                };
            }
        }
        """,
        """
        class C
        {
            int P
            {
                get { return 1; }
            }

            void M(bool b)
            {
                if (b)
                {
                }

                var a = new[]
                {
                    1,
                };
            }
        }
        """);

    [Fact]
    public Task SeveralBlankLines_AreAllRemoved()
    {
        // The second blank line contains only spaces.
        var source = "class C\n{\n    void M()\n\n    \n\n    {|BRO1501:{|}\n    }\n}\n";
        var fixedSource = "class C\n{\n    void M()\n    {\n    }\n}\n";
        return VerifyFixAsync(source, fixedSource);
    }

    [Fact]
    public Task BlankLineBetweenCommentAndBrace_IsRemoved() => VerifyFixAsync(
        """
        class C
        {
            void M(bool b)
            {
                if (b)
                // why

                {|BRO1501:{|}
                }
            }
        }
        """,
        """
        class C
        {
            void M(bool b)
            {
                if (b)
                // why
                {
                }
            }
        }
        """);

    [Fact]
    public Task CommentDirectlyAboveBrace_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(bool b)
            {
                if (b)

                // why
                {
                }
            }
        }
        """);

    [Fact]
    public Task BraceOnTheSameLine_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C {
            void M() {
            }
        }
        """);

    [Fact]
    public Task BraceInsideRawString_IsNotTouched() => VerifyNoDiagnosticsAsync(""""
        class C
        {
            string M(int x) => $"""

                {x}
                """;
        }
        """");

    [Fact]
    public Task DirectiveDirectlyAboveBrace_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M()
        #if !NEVER

        #endif
            {
            }
        }
        """);

    [Fact]
    public Task StandaloneBlocks_ReportedUnlessTheyFollowAClosingBrace() => VerifyFixAsync(
        """
        class C
        {
            void M(int x)
            {
                var y = x;

                {|BRO1501:{|}
                }

                {
                }
                // a comment between the braces doesn't matter

                {
                }

                switch (x)
                {
                    case 1:

                    {|BRO1501:{|}
                        break;
                    }
                }
            }
        }
        """,
        """
        class C
        {
            void M(int x)
            {
                var y = x;
                {
                }

                {
                }
                // a comment between the braces doesn't matter

                {
                }

                switch (x)
                {
                    case 1:
                    {
                        break;
                    }
                }
            }
        }
        """);

    [Fact]
    public Task ChainedBlocks_BlankLinesAreRemoved() => VerifyFixAsync(
        """
        class C
        {
            void M(bool b)
            {
                if (b)
                {
                }

                {|BRO1502:else|}
                {
                }

                try
                {
                }

                {|BRO1502:catch|}
                {
                }

                {|BRO1502:finally|}
                {
                }

                if (b)
                    M(b);

                {|BRO1502:else|} if (!b)
                    M(b);
            }
        }
        """,
        """
        class C
        {
            void M(bool b)
            {
                if (b)
                {
                }
                else
                {
                }

                try
                {
                }
                catch
                {
                }
                finally
                {
                }

                if (b)
                    M(b);
                else if (!b)
                    M(b);
            }
        }
        """);

    [Fact]
    public Task DoWhile_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(bool b)
            {
                do
                {
                }

                while (b);
            }
        }
        """);

    [Fact]
    public Task CommentDirectlyAboveElse_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(bool b)
            {
                if (b)
                {
                }

                // otherwise
                else
                {
                }
            }
        }
        """);
}
