using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.CombinedAttributesAnalyzer, StyleBro.CodeFixes.Readability.CombinedAttributesCodeFixProvider>;

namespace StyleBro.Tests;

public class CombinedAttributesTests
{
    [Fact]
    public Task ListOnItsOwnLine_IsSplitIntoLines() => VerifyFixAsync("""
        using System;
        using System.Diagnostics;

        class C
        {
            [Obsolete("a, b"), {|BRO1102:DebuggerStepThrough|}]
            void M() { }
        }
        """, """
        using System;
        using System.Diagnostics;

        class C
        {
            [Obsolete("a, b")]
            [DebuggerStepThrough]
            void M() { }
        }
        """);

    [Fact]
    public Task ListSharingItsLine_IsSplitInPlace() => VerifyFixAsync("""
        using System;
        using System.Diagnostics;

        class C
        {
            [Obsolete, {|BRO1102:DebuggerStepThrough|}] void M() { }
        }
        """, """
        using System;
        using System.Diagnostics;

        class C
        {
            [Obsolete] [DebuggerStepThrough] void M() { }
        }
        """);

    [Fact]
    public Task Targets_AreRepeated() => VerifyFixAsync("""
        [assembly: System.CLSCompliant(false), {|BRO1102:System.Reflection.AssemblyTrademark("x")|}]

        class C
        {
            [return: A, {|BRO1102:B|}]
            int M() => 0;
        }

        class AAttribute : System.Attribute { }
        class BAttribute : System.Attribute { }
        """, """
        [assembly: System.CLSCompliant(false)]
        [assembly: System.Reflection.AssemblyTrademark("x")]

        class C
        {
            [return: A]
            [return: B]
            int M() => 0;
        }

        class AAttribute : System.Attribute { }
        class BAttribute : System.Attribute { }
        """);

    [Fact]
    public Task MultiLineList_BecomesOneLinePerAttribute() => VerifyFixAsync("""
        [A,
         {|BRO1102:B|},
         C]
        class D { }

        class AAttribute : System.Attribute { }
        class BAttribute : System.Attribute { }
        class CAttribute : System.Attribute { }
        """, """
        [A]
        [B]
        [C]
        class D { }

        class AAttribute : System.Attribute { }
        class BAttribute : System.Attribute { }
        class CAttribute : System.Attribute { }
        """);

    [Fact]
    public Task ParameterAttributes_AreNotReported() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M([A, B] int x) { }
        }

        class AAttribute : System.Attribute { }
        class BAttribute : System.Attribute { }
        """);

    [Fact]
    public Task CommentBetweenAttributes_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        using System;
        using System.Diagnostics;

        class C
        {
            [Obsolete, /* why */ DebuggerStepThrough]
            void M() { }
        }
        """);

    [Fact]
    public Task SingleAttribute_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            [System.Obsolete]
            void M() { }
        }
        """);
}
