using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmptyStatementAnalyzer, StyleBro.CodeFixes.Readability.EmptyStatementCodeFixProvider>;

namespace StyleBro.Tests;

public class EmptyStatementTests
{
    [Fact]
    public Task EmptyStatementOnItsOwnLine_LineIsRemoved() => VerifyFixAsync("""
        class C
        {
            void M()
            {
                M();
                {|BRO1101:;|}
                M();
            }
        }
        """, """
        class C
        {
            void M()
            {
                M();
                M();
            }
        }
        """);

    [Fact]
    public Task EmptyStatementAfterStatement_IsRemovedWithTheSpaceBeforeIt() => VerifyFixAsync("""
        class C
        {
            void M()
            {
                M(); {|BRO1101:;|}
            }
        }
        """, """
        class C
        {
            void M()
            {
                M();
            }
        }
        """);

    [Fact]
    public Task EmptyStatementBeforeComment_CommentKeepsItsIndentation() => VerifyFixAsync("""
        class C
        {
            void M()
            {
                {|BRO1101:;|} // done
            }
        }
        """, """
        class C
        {
            void M()
            {
                // done
            }
        }
        """);

    [Fact]
    public Task SeveralOnOneLine_AreRemovedTogether() => VerifyFixAsync("""
        class C
        {
            void M()
            {
                {|BRO1101:;|}{|BRO1101:;|}
                M(); {|BRO1101:;|} {|BRO1101:;|}
            }
        }
        """, """
        class C
        {
            void M()
            {
                M();
            }
        }
        """);

    [Fact]
    public Task EmptyStatementInSwitchSection_IsRemoved() => VerifyFixAsync("""
        class C
        {
            void M(int i)
            {
                switch (i)
                {
                    case 1:
                        {|BRO1101:;|}
                        break;
                }
            }
        }
        """, """
        class C
        {
            void M(int i)
            {
                switch (i)
                {
                    case 1:
                        break;
                }
            }
        }
        """);

    [Fact]
    public Task SemicolonAfterTypeAndNamespace_IsRemoved() => VerifyFixAsync("""
        namespace N
        {
            class C
            {
            }{|BRO1101:;|}

            enum E { A }{|BRO1101:;|}
        }{|BRO1101:;|}
        """, """
        namespace N
        {
            class C
            {
            }

            enum E { A }
        }
        """);

    [Fact]
    public Task RecordWithoutBody_NeedsItsSemicolon() => VerifyNoDiagnosticsAsync("""
        record R(int X);

        namespace System.Runtime.CompilerServices
        {
            class IsExternalInit { }
        }
        """);

    [Fact]
    public Task EmbeddedEmptyStatements_AreLeftToTheCompiler() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(bool b)
            {
                while (b) ;
                if (b) ; else M(b);
                for (;;) ;
            }
        }
        """);

    [Fact]
    public Task LabeledEmptyStatement_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M()
            {
                goto end;
            end: ;
            }
        }
        """);

    [Fact]
    public Task EmptyStatementBetweenDirectives_DirectivesAreKept() => VerifyFixAsync("""
        class C
        {
            void M()
            {
        #if !NEVER
                {|BRO1101:;|}
        #endif
                M();
            }
        }
        """, """
        class C
        {
            void M()
            {
        #if !NEVER
        #endif
                M();
            }
        }
        """);
}
