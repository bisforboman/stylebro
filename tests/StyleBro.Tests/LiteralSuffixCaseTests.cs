using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.LiteralSuffixAnalyzer, StyleBro.CodeFixes.Readability.LiteralSuffixCodeFixProvider>;

namespace StyleBro.Tests;

public class LiteralSuffixCaseTests
{
    [Fact]
    public Task IntegerSuffixes_AreUpperCased() => Verify.VerifyFixAsync(
        """
        public class C
        {
            private const long A = {|BRO1135:1l|};
            private const uint B = {|BRO1135:2u|};
            private const ulong D = {|BRO1135:3ul|} + {|BRO1135:4Ul|} + {|BRO1135:5uL|} + {|BRO1135:6lu|} + 7LU;
        }
        """,
        """
        public class C
        {
            private const long A = 1L;
            private const uint B = 2U;
            private const ulong D = 3UL + 4UL + 5UL + 6LU + 7LU;
        }
        """);

    [Fact]
    public Task HexAndBinaryLiterals() => Verify.VerifyFixAsync(
        """
        public class C
        {
            private const uint A = {|BRO1135:0x9E3779B1u|};
            private const ulong B = {|BRO1135:0b1010_1010ul|};
            private const int D = 0xfd;
        }
        """,
        """
        public class C
        {
            private const uint A = 0x9E3779B1U;
            private const ulong B = 0b1010_1010UL;
            private const int D = 0xfd;
        }
        """);

    [Fact]
    public Task RealLiterals_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private const float A = 1.5f;
            private const double B = 1e5d;
            private const decimal D = 2m;
            private const long E = 1L;
            private const int F = 10;
        }
        """);
}
