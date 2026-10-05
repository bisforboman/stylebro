using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmptyRecordBodyAnalyzer, StyleBro.CodeFixes.Readability.EmptyRecordBodyCodeFixProvider>;

namespace StyleBro.Tests;

public class EmptyRecordBodyTests
{
    [Fact]
    public Task EmptyBody_BecomesASemicolon() => Verify.VerifyFixAsync(
        """
        public record A(int X) {|BRO1140:{ }|}

        public record struct B(int X)
        {|BRO1140:{
        }|}

        public sealed record D<T>(T Value) : A(1) where T : class {|BRO1140:{ }|};

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """,
        """
        public record A(int X);

        public record struct B(int X);

        public sealed record D<T>(T Value) : A(1) where T : class;

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """);

    [Fact]
    public Task BodiesThatStay_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public record A(int X)
        {
            public int Y => this.X;
        }

        public record B
        {
        }

        public record D(int X)
        {
            // later
        }

        public record E(int X);

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """);
}
