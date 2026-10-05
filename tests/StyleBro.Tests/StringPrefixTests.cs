using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.StringPrefixAnalyzer, StyleBro.CodeFixes.Readability.StringPrefixCodeFixProvider>;

namespace StyleBro.Tests;

public class StringPrefixTests
{
    // '$$' is the test markup's caret, so '$$"""{x}"""' is covered by samples/Messy (Strings.cs) instead.
    [Fact]
    public Task InterpolatedStringsWithoutHoles_LoseTheDollar() => Verify.VerifyFixAsync(
        """"
        public class C
        {
            public string A = {|BRO1138:$"Done"|};
            public string B = {|BRO1138:$"{{x}} \t"|};
            public string D = {|BRO1138:$@"C:\{{temp}}"|};
            public string E = {|BRO1138:@$"plain {{x}}"|};
            public string F = {|BRO1138:$"""Say "hi" """|};
            public string G = {|BRO1138:$"""plain"""|};
            public object H = {|BRO1138:$"boxed"|};
            public string I = $"{nameof(C)}";
        }
        """",
        """"
        public class C
        {
            public string A = "Done";
            public string B = "{x} \t";
            public string D = @"C:\{temp}";
            public string E = "plain {x}";
            public string F = """Say "hi" """;
            public string G = "plain";
            public object H = "boxed";
            public string I = $"{nameof(C)}";
        }
        """");

    [Fact]
    public Task MultiLineRawInterpolatedString_StaysRaw() => Verify.VerifyFixAsync(
        """"
        public class C
        {
            public string A = {|BRO1138:$"""
                text
                """|};
        }
        """",
        """"
        public class C
        {
            public string A = """
                text
                """;
        }
        """");

    [Fact]
    public Task VerbatimAndRawStrings_BecomePlainWhenNothingNeedsEscaping() => Verify.VerifyFixAsync(
        """"
        public class C
        {
            public string A = {|BRO1138:@"Done"|};
            public string B = {|BRO1138:"""Done"""|};
            public System.ReadOnlySpan<byte> D => {|BRO1138:@"bytes"u8|};
            public System.ReadOnlySpan<byte> E => {|BRO1138:"""bytes"""u8|};
            public string F = @"C:\temp";
            public string G = @"Say ""hi""";
            public string H = """Say "hi" """;
            public string I = """C:\temp""";
            public string J = @"two
        lines";
            public string K = @"";
            public string L = "plain";
        }
        """",
        """"
        public class C
        {
            public string A = "Done";
            public string B = "Done";
            public System.ReadOnlySpan<byte> D => "bytes"u8;
            public System.ReadOnlySpan<byte> E => "bytes"u8;
            public string F = @"C:\temp";
            public string G = @"Say ""hi""";
            public string H = """Say "hi" """;
            public string I = """C:\temp""";
            public string J = @"two
        lines";
            public string K = @"";
            public string L = "plain";
        }
        """");

    [Fact]
    public Task InterpolatedStringsThatAreNotStrings_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;

        public class C
        {
            public FormattableString A = $"Done";
            public IFormattable B = $"Done";
            public string D = $"";
        }
        """);
}
