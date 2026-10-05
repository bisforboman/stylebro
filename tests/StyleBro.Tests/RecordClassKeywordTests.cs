using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.RecordClassKeywordAnalyzer, StyleBro.CodeFixes.Readability.RecordClassKeywordCodeFixProvider>;

namespace StyleBro.Tests;

public class RecordClassKeywordTests
{
    private const string IsExternalInit = """


        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """;

    [Fact]
    public Task RecordClass_LosesTheClassKeyword() => Verify.VerifyFixAsync(
        """
        public record {|BRO1146:class|} Point(int X, int Y);

        public sealed record  {|BRO1146:class|} Named
        {
            public string Name { get; init; } = "";
        }

        public partial record
            {|BRO1146:class|} Split<T>(T Value) where T : class;
        """ + IsExternalInit,
        """
        public record Point(int X, int Y);

        public sealed record Named
        {
            public string Name { get; init; } = "";
        }

        public partial record Split<T>(T Value) where T : class;
        """ + IsExternalInit);

    [Fact]
    public Task OtherRecords_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public record Point(int X, int Y);

        public record struct Size(int Width, int Height);

        public readonly record struct Pair(int A, int B);

        public record /* reference type */ class Commented(int X);
        """ + IsExternalInit);
}
