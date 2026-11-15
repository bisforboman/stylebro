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
    public Task OtherLiterals_NoDiagnostic() => VerifyNoDiagnosticsAsync(""""
        using System;

        class C
        {
            void M(string s)
            {
                ReadOnlySpan<byte> b = ""u8;
                var c = "x";
                var d = " ";
                var e = $" ";
                var f = $"{s}";
                var g = $"{s}{s}";
                var h = """ """;
                var i = """


                    """;
                var j = $"""


                    """;
                var k = $"{{";
            }
        }
        """");

    // An interpolated string without content and a raw literal whose value is empty are empty strings too.
    [Fact]
    public Task EmptyInterpolatedAndRawStrings_BecomeStringEmpty() => VerifyFixAsync(
        """"
        class C
        {
            string field = {|BRO1106:$""|};

            string M(string s, object o)
            {
                var a = {|BRO1106:$""|};
                var b = {|BRO1106:$@""|};
                var c = {|BRO1106:@$""|};
                var d = {|BRO1106:"""

                    """|};
                var e = {|BRO1106:$"""

                    """|};
                o = {|BRO1106:$""|};
                M({|BRO1106:$""|}, {|BRO1106:$""|});
                var g = {|BRO1106:$""|} + s;
                var h = $"{(s == {|BRO1106:$""|} ? {|BRO1106:""|} : s)}";
                var i = {|BRO1106:$""|}
                    .Length;
                return s == {|BRO1106:$""|} ? {|BRO1106:$""|} : "x";
            }
        }
        """",
        """"
        class C
        {
            string field = string.Empty;

            string M(string s, object o)
            {
                var a = string.Empty;
                var b = string.Empty;
                var c = string.Empty;
                var d = string.Empty;
                var e = string.Empty;
                o = string.Empty;
                M(string.Empty, string.Empty);
                var g = string.Empty + s;
                var h = $"{(s == string.Empty ? string.Empty : s)}";
                var i = string.Empty
                    .Length;
                return s == string.Empty ? string.Empty : "x";
            }
        }
        """");

    [Fact]
    public Task EmptyInterpolatedAndRawStrings_InConstantContexts_Stay() => VerifyNoDiagnosticsAsync(""""
        using System;

        class C
        {
            const string Constant = $"";
            const string Raw = """

                """;
            const string Combined = $"" + "x";

            [Obsolete($"")]
            int M(string s = $"", string t = """

                """)
            {
                const string local = $"";
                switch (s)
                {
                    case $"":
                        break;
                }

                bool b = s is $"";
                return s switch { $"" => 1, _ => 2 };
            }
        }
        """");

    // There the interpolated string isn't a string: 'string.Empty' wouldn't compile, or would pick another overload.
    [Fact]
    public Task EmptyInterpolatedStrings_AsFormattableOrHandler_Stay() => VerifyNoDiagnosticsAsync("""
        using System;
        using System.Runtime.CompilerServices;

        class C
        {
            void M()
            {
                FormattableString a = $"";
                IFormattable b = $"";
                Formattable($"");
                Handler($"");
            }

            void Formattable(FormattableString text)
            {
            }

            void Handler(DefaultInterpolatedStringHandler text)
            {
            }
        }
        """);

    [Fact]
    public Task EmptyInterpolatedString_WithAStringOverload_BecomesStringEmpty() => VerifyFixAsync(
        """
        using System;

        class C
        {
            void M() => Both({|BRO1106:$""|});

            void Both(FormattableString text)
            {
            }

            void Both(string text)
            {
            }
        }
        """,
        """
        using System;

        class C
        {
            void M() => Both(string.Empty);

            void Both(FormattableString text)
            {
            }

            void Both(string text)
            {
            }
        }
        """);

    [Fact]
    public Task LiteralStyle_EmptyInterpolatedAndRawStrings_NoDiagnostic() => VerifyNoDiagnosticsAsync(
        """"
        class C
        {
            string a = $"";
            string b = """

                """;
        }
        """",
        LiteralStyle);

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

    [Fact]
    public Task ExpressionTrees_KeepTheirForm() => VerifyFixAsync(
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        class C
        {
            // Trial 58 (ExpressionTreeToString): a constant and a field access are different trees (owner, 2026-10-27).
            Expression<Func<string>> e = () => "";
            Func<string> f = () => {|BRO1106:""|};

            IQueryable<string> Q(IQueryable<string> q) => q.Where(s => s != "");
        }
        """,
        """
        using System;
        using System.Linq;
        using System.Linq.Expressions;

        class C
        {
            // Trial 58 (ExpressionTreeToString): a constant and a field access are different trees (owner, 2026-10-27).
            Expression<Func<string>> e = () => "";
            Func<string> f = () => string.Empty;

            IQueryable<string> Q(IQueryable<string> q) => q.Where(s => s != "");
        }
        """);

    [Fact]
    public Task LiteralStyle_ExpressionTrees_KeepTheirForm() => VerifyFixAsync(
        """
        using System;
        using System.Linq.Expressions;

        class C
        {
            Expression<Func<string>> e = () => string.Empty;
            Func<string> f = () => {|BRO1106:string.Empty|};
        }
        """,
        """
        using System;
        using System.Linq.Expressions;

        class C
        {
            Expression<Func<string>> e = () => string.Empty;
            Func<string> f = () => "";
        }
        """,
        LiteralStyle);

    [Fact]
    public Task LiteralStyle_ExpressionTreeTwins_KeepTheirForm() => VerifyFixAsync(
        """
        using System;
        using System.Linq.Expressions;

        class C
        {
            // Trial 59 (DelegateDecompiler): the delegate is decompiled and compared with its twin tree (owner, 2026-10-28).
            void M()
            {
                Expression<Func<string, bool>> expected = s => (s ?? string.Empty).Contains("Test");
                Func<string, bool> compiled = s => (s ?? string.Empty).Contains("Test");
                Func<string> other = () => {|BRO1106:string.Empty|};
            }
        }
        """,
        """
        using System;
        using System.Linq.Expressions;

        class C
        {
            // Trial 59 (DelegateDecompiler): the delegate is decompiled and compared with its twin tree (owner, 2026-10-28).
            void M()
            {
                Expression<Func<string, bool>> expected = s => (s ?? string.Empty).Contains("Test");
                Func<string, bool> compiled = s => (s ?? string.Empty).Contains("Test");
                Func<string> other = () => "";
            }
        }
        """,
        LiteralStyle);
}
