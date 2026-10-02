using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EnumValueLinesAnalyzer, StyleBro.CodeFixes.Readability.EnumValueLinesCodeFixProvider>;

namespace StyleBro.Tests;

public class EnumValueLinesTests
{
    [Fact]
    public Task EnumValues_GetTheirOwnLine() => VerifyFixAsync(
        """
        public enum OneLine { A, {|BRO1121:B|}, {|BRO1121:C|} }

        public enum Shared
        {
            A, {|BRO1121:B|},

            C,
        }
        """,
        """
        public enum OneLine
        {
            A,
            B,
            C,
        }

        public enum Shared
        {
            A,
            B,

            C,
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        public enum Fine
        {
            A,
            B,
        }

        public enum Commented
        {
            A, /* b */ B,
        }
        """);
}
