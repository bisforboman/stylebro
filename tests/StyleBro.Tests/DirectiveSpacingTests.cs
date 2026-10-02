using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Spacing.DirectiveSpacingAnalyzer, StyleBro.CodeFixes.Spacing.DirectiveSpacingCodeFixProvider>;

namespace StyleBro.Tests;

public class DirectiveSpacingTests
{
    [Fact]
    public Task SpaceAfterHash_IsRemoved() => VerifyFixAsync(
        """
        # {|BRO1006:nullable|} enable
        public class C
        {
        #   {|BRO1006:region|} Members
            public int A;
        #	{|BRO1006:endregion|}

        #  {|BRO1006:if|} DEBUG
            public int B;
        # {|BRO1006:else|}
            public int D;
        # {|BRO1006:endif|}

            public void M()
            {
                # {|BRO1006:pragma|} warning disable CS0168
                int unused;
                #pragma warning restore CS0168
            }
        }
        """,
        """
        #nullable enable
        public class C
        {
        #region Members
            public int A;
        #endregion

        #if DEBUG
            public int B;
        #else
            public int D;
        #endif

            public void M()
            {
                #pragma warning disable CS0168
                int unused;
                #pragma warning restore CS0168
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        #nullable enable
        public class C
        {
            // # if this is a comment
            public string S = "# if";
        #if DEBUG
            public int B;
        #endif
        }
        """);
}
