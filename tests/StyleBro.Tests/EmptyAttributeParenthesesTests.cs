using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.EmptyAttributeParenthesesAnalyzer, StyleBro.CodeFixes.Maintainability.EmptyAttributeParenthesesCodeFixProvider>;

namespace StyleBro.Tests;

public class EmptyAttributeParenthesesTests
{
    [Fact]
    public Task EmptyParentheses_AreRemoved() => VerifyFixAsync(
        """
        using System;

        [Obsolete{|BRO1402:()|}]
        public class C
        {
            [Obsolete{|BRO1402:( )|}]
            public void A()
            {
            }

            [Obsolete {|BRO1402:()|}, System.Diagnostics.DebuggerHidden{|BRO1402:()|}]
            public void B()
            {
            }

            [return: System.Diagnostics.CodeAnalysis.NotNull{|BRO1402:()|}]
            public string D() => string.Empty;
        }
        """,
        """
        using System;

        [Obsolete]
        public class C
        {
            [Obsolete]
            public void A()
            {
            }

            [Obsolete, System.Diagnostics.DebuggerHidden]
            public void B()
            {
            }

            [return: System.Diagnostics.CodeAnalysis.NotNull]
            public string D() => string.Empty;
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        public class C
        {
            [Obsolete("old")]
            public void A()
            {
            }

            [Obsolete(/* none */)]
            public void B()
            {
            }

            [Obsolete]
            public void D()
            {
            }
        }
        """);
}
