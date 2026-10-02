using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.LiteralSuffixAnalyzer, StyleBro.CodeFixes.Readability.LiteralSuffixCodeFixProvider>;

namespace StyleBro.Tests;

public class LiteralSuffixTests
{
    [Fact]
    public Task Casts_BecomeSuffixes() => Verify.VerifyFixAsync("""
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
            long m = {|BRO1122:(long)1u|};
        }
        """, """
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
    public Task FloatFromADoubleLiteral_IsFixedWhenTheValueIsTheSame() => Verify.VerifyFixAsync("""
        class C
        {
            float a = {|BRO1122:(float)0.5|};
            float b = {|BRO1122:(float)0.1|};
        }
        """, """
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
}
