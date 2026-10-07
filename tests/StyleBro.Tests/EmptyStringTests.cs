using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.EmptyStringAnalyzer, StyleBro.CodeFixes.Readability.EmptyStringCodeFixProvider>;

namespace StyleBro.Tests;

public class EmptyStringTests
{
    private const string LiteralStyle = "stylebro_empty_string_style = literal\n";

    [Fact]
    public Task EmptyLiterals_BecomeStringEmpty() => VerifyFixAsync(
        """
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
        """,
        """
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

    [Fact]
    public Task LiteralStyle_StringEmptyBecomesTheLiteral() => VerifyFixAsync(
        """
        using System;

        class C
        {
            string field = {|BRO1106:string.Empty|};

            bool M(string s)
            {
                var a = {|BRO1106:String.Empty|};
                var b = {|BRO1106:System.String.Empty|} + {|BRO1106:global::System.String.Empty|};
                var c = {|BRO1106:string.Empty|}.Length;
                var d = $"{{|BRO1106:string.Empty|}}";
                var e = "";
                var f = @"";
                return s == {|BRO1106:string
                    .Empty|};
            }
        }
        """,
        """
        using System;

        class C
        {
            string field = "";

            bool M(string s)
            {
                var a = "";
                var b = "" + "";
                var c = "".Length;
                var d = $"{""}";
                var e = "";
                var f = @"";
                return s == "";
            }
        }
        """,
        LiteralStyle);

    [Fact]
    public Task LiteralStyle_OtherMembersNameofAndComments_NoDiagnostic() => VerifyNoDiagnosticsAsync(
        """
        class Other
        {
            public static readonly string Empty = "x";
        }

        class C
        {
            string Empty => "y";

            void M()
            {
                var a = Other.Empty;
                var b = this.Empty;
                var c = nameof(string.Empty);
                var d = nameof(string.Empty.Length);
                var e = string /* why */ .Empty;
            }
        }
        """,
        LiteralStyle);
}
