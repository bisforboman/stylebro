using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmptyStringAnalyzer, StyleBro.CodeFixes.Readability.EmptyStringCodeFixProvider>;

namespace StyleBro.Tests;

public class EmptyStringTests
{
    [Fact]
    public Task EmptyLiterals_BecomeStringEmpty() => VerifyFixAsync("""
        class C
        {
            static readonly string ReadOnly = {|BRO1106:""|};
            string field = {|BRO1106:""|};

            bool M(string s)
            {
                var a = {|BRO1106:""|};
                var b = {|BRO1106:@""|};
                var c = nameof(M) + {|BRO1106:""|};
                return s == {|BRO1106:""|};
            }
        }
        """, """
        class C
        {
            static readonly string ReadOnly = string.Empty;
            string field = string.Empty;

            bool M(string s)
            {
                var a = string.Empty;
                var b = string.Empty;
                var c = nameof(M) + string.Empty;
                return s == string.Empty;
            }
        }
        """);

    [Fact]
    public Task ConstantContexts_KeepTheLiteral() => VerifyNoDiagnosticsAsync("""
        using System;

        class C
        {
            const string Constant = "";
            const string Combined = "" + "x";

            [Obsolete("")]
            int M(string s = "")
            {
                const string local = "";
                switch (s)
                {
                    case "":
                        break;
                }

                bool b = s is "";
                return s switch { "" => 1, _ => 2 };
            }
        }
        """);

    [Fact]
    public Task OtherLiterals_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        using System;

        class C
        {
            void M()
            {
                var a = $"";
                ReadOnlySpan<byte> b = ""u8;
                var c = "x";
                var d = " ";
            }
        }
        """);
}
