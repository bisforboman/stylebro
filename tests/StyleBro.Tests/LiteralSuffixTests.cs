using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.LiteralSuffixAnalyzer, StyleBro.CodeFixes.Readability.LiteralSuffixCodeFixProvider>;

namespace StyleBro.Tests;

public class LiteralSuffixTests
{
    [Fact]
    public Task Casts_BecomeSuffixes() => Verify.VerifyFixAsync(
        """
        class C
        {
            long a = {|BRO1122:(long)1|};
            ulong b = {|BRO1122:(ulong)1|};
            uint c = {|BRO1122:(uint)1|};
            float d = {|BRO1122:(float)1|};
            double e = {|BRO1122:(double)1|};
            decimal f = {|BRO1122:(decimal)1|};
            long g = {|BRO1122:(long)-1|};
            long h = {|BRO1122:(long)(1)|};
            long i = {|BRO1122:(long)0x10|};
            long j = {|BRO1122:(long)1_000|};
            float k = {|BRO1122:(float)1e3|};
            int l = {|BRO1122:(int)1L|};
            long m = {|BRO1122:(long){|BRO1135:1u|}|};
        }
        """,
        """
        class C
        {
            long a = 1L;
            ulong b = 1UL;
            uint c = 1U;
            float d = 1F;
            double e = 1D;
            decimal f = 1M;
            long g = -1L;
            long h = (1L);
            long i = 0x10L;
            long j = 1_000L;
            float k = 1e3F;
            int l = 1;
            long m = 1L;
        }
        """);

    // tfplan2md: init found '0u' and turned BRO1135 off, and BRO1122 still wrote '6U'. 'L' stays upper case (CS0078).
    [Fact]
    public Task WithBro1135Off_UnsignedSuffixesAreLowerCase() => Verify.VerifyFixAsync(
        """
        class C
        {
            uint a = {|BRO1122:(uint)6|};
            ulong b = {|BRO1122:(ulong)1|};
            long c = {|BRO1122:(long)1|};
            float d = {|BRO1122:(float)1|};
        }
        """,
        """
        class C
        {
            uint a = 6u;
            ulong b = 1ul;
            long c = 1L;
            float d = 1F;
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1135.severity = none");

    [Fact]
    public Task ValueOrTypeWouldChange_IsNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        class C
        {
            // Rounds through double to 15 digits; the M literal keeps every digit.
            decimal a = (decimal)0.1234567890123456789;
            // 1.5 vs "1.50": decimals keep their scale.
            decimal b = (decimal)1.50;
            // Rounds to a double halfway between two floats, then down; the F literal rounds up.
            float x = (float)1.00000005960464488641292746251565404236316680908203125;
            // 'F' is a hex digit.
            float c = (float)0x10;
            // Negating an unsigned literal makes it long.
            ulong d = (ulong)-0;
            int e = (int)1;
            long f = (long)(1 + 1);
            int g = unchecked((int)0xFFFFFFFF);
            short h = (short)1;
            long i = (long)/* one */1;
        }
        """);

    [Fact]
    public Task FloatFromADoubleLiteral_IsFixedWhenTheValueIsTheSame() => Verify.VerifyFixAsync(
        """
        class C
        {
            float a = {|BRO1122:(float)0.5|};
            float b = {|BRO1122:(float)0.1|};
        }
        """,
        """
        class C
        {
            float a = 0.5F;
            float b = 0.1F;
        }
        """);

    [Fact]
    public Task MinusDirectlyBefore_IsNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        class C
        {
            long M(long a) => a-(long)-1;
        }
        """);

    // actions/runner: '((Double)0).ToString(...)' became '(0D).ToString(...)', and BRO1405 removed the parentheses in a
    // second run. With BRO1405 on, the fix removes those it would; '(-1L)' keeps them (a sign isn't a simple expression).
    [Fact]
    public Task ParenthesesTheLiteralMakesUnnecessary_AreRemovedWithBro1405On() => Verify.VerifyFixAsync(
        """
        class C
        {
            string b = ({|BRO1122:(double)0|}).ToString();
            string c = ({|BRO1122:(long)-1|}).ToString();
            double d = 1 + ({|BRO1122:(double)2|}) * 3;
            long e = 1 + ({|BRO1122:(long)-1|});
        }
        """,
        """
        class C
        {
            string b = 0D.ToString();
            string c = (-1L).ToString();
            double d = 1 + 2D * 3;
            long e = 1 + (-1L);
        }
        """);

    [Fact]
    public Task ParenthesesAroundTheCast_StayWithBro1405Off() => Verify.VerifyFixAsync(
        """
        class C
        {
            string b = ({|BRO1122:(double)0|}).ToString();
        }
        """,
        """
        class C
        {
            string b = (0D).ToString();
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1405.severity = none");
}
