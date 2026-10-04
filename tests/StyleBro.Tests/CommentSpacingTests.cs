using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Spacing.CommentSpacingAnalyzer, StyleBro.CodeFixes.Spacing.CommentSpacingCodeFixProvider>;

namespace StyleBro.Tests;

public class CommentSpacingTests
{
    [Fact]
    public Task CommentsWithoutSpace_GetOne() => VerifyFixAsync(
        """
        class C
        {
            {|BRO1002://note|}
            void M()
            {
                M(); {|BRO1002://trailing|}
                {|BRO1002://TODO: later|}
                {|BRO1002://-x|}
                {|BRO1002://=====|}
            }
        }
        """,
        """
        class C
        {
            // note
            void M()
            {
                M(); // trailing
                // TODO: later
                // -x
                // =====
            }
        }
        """);

    [Fact]
    public Task TabsAfterSlashes_BecomeOneSpace()
    {
        var source = "class C\n{\n    {|BRO1002://\t\tx|}\n    {|BRO1002://\t|}\n}\n";
        var fixedSource = "class C\n{\n    // x\n    //\n}\n";
        return VerifyFixAsync(source, fixedSource);
    }

    [Fact]
    public Task StyleCopExceptions_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            // fine
            //  two spaces
            //
            ////commented out code
            ///not a doc position
            //---------------------
            //-:cnd:noEmit
            //+:cnd:noEmit
            /*block*/
            /// <summary>A doc comment.</summary>
            void M() { }
        #if NEVER
            //disabled code is not a comment
        #endif
        }
        """);
}
