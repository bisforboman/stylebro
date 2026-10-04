using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.WrappingPlacementAnalyzer, StyleBro.CodeFixes.Layout.WrappingPlacementCodeFixProvider>;

namespace StyleBro.Tests;

public class WrappingPlacementTests
{
    [Fact]
    public Task Operators_MoveToTheBeginningOfTheLine() => VerifyFixAsync(
        """
        public class C
        {
            public string M(bool a, bool b, bool c, string s, string t)
            {
                if (a {|BRO1520:&&|}
                    b {|BRO1520:|||}

                    c)
                {
                    return s {|BRO1520:??|}
                        t;
                }

                var x = s {|BRO1520:+|}
                    t {|BRO1520:+|}
                    s;
                return a {|BRO1520:?|}
                    x {|BRO1520::|}
                    t;
            }
        }
        """.Replace("&&|}\n", "&&|}   \n"), // trailing whitespace goes
        """
        public class C
        {
            public string M(bool a, bool b, bool c, string s, string t)
            {
                if (a
                    && b

                    || c)
                {
                    return s
                        ?? t;
                }

                var x = s
                    + t
                    + s;
                return a
                    ? x
                    : t;
            }
        }
        """);

    [Fact]
    public Task Operators_EndOfLine_FromTheSdkKey() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool a, bool b, int x, int y) =>
                a
                    {|BRO1520:&&|} b
                    {|BRO1520:?|} x
                    {|BRO1520:-|} -y
                    {|BRO1520::|} y;
        }
        """,
        """
        public class C
        {
            public int M(bool a, bool b, int x, int y) =>
                a &&
                    b ?
                    x -
                    -y :
                    y;
        }
        """,
        "dotnet_style_operator_placement_when_wrapping = end_of_line\nstylebro_arrow_placement_when_wrapping = end_of_line\n");

    [Fact]
    public Task Arrows_MoveToTheEndOfTheLine() => VerifyFixAsync(
        """
        public class C
        {
            public int P
                {|BRO1521:=>|} 1;

            public int Q
            {
                get
                    {|BRO1521:=>|} 2;
            }

            public string M(int x)
                {|BRO1521:=>|} x switch
                {
                    1
                        {|BRO1521:=>|} "one",
                    _ => "other",
                };

            public int Already =>
                3;
        }
        """,
        """
        public class C
        {
            public int P =>
                1;

            public int Q
            {
                get =>
                    2;
            }

            public string M(int x) =>
                x switch
                {
                    1 =>
                        "one",
                    _ => "other",
                };

            public int Already =>
                3;
        }
        """);

    [Fact]
    public Task Arrows_BeginningOfLine() => VerifyFixAsync(
        """
        public class C
        {
            public int P {|BRO1521:=>|}
                1;

            public int Already
                => 3;
        }
        """,
        """
        public class C
        {
            public int P
                => 1;

            public int Already
                => 3;
        }
        """,
        "stylebro_arrow_placement_when_wrapping = beginning_of_line:warning\n");

    [Fact]
    public Task Equals_MoveToTheEndOfTheLine() => VerifyFixAsync(
        """
        public class C
        {
            private int field
                {|BRO1522:=|} 1;

            public int M(int a = 0)
            {
                var x
                    {|BRO1522:=|} a;
                x
                    {|BRO1522:+=|} field;
                x =
                    a;
                return x;
            }
        }
        """,
        """
        public class C
        {
            private int field =
                1;

            public int M(int a = 0)
            {
                var x =
                    a;
                x +=
                    field;
                x =
                    a;
                return x;
            }
        }
        """);

    [Fact]
    public Task Equals_BeginningOfLine() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a)
            {
                var x {|BRO1522:=|}
                    a;
                return x;
            }
        }
        """,
        """
        public class C
        {
            public int M(int a)
            {
                var x
                    = a;
                return x;
            }
        }
        """,
        "stylebro_equals_placement_when_wrapping = beginning_of_line\n");

    [Fact]
    public Task StringsKeepTheirContent() => VerifyFixAsync(
        """"
        public class C
        {
            public string M(int a) =>
                """
                a +
                b
                """ {|BRO1520:+|}
                $@"{a} +
                " {|BRO1520:+|}
                @"
                x";
        }
        """",
        """"
        public class C
        {
            public string M(int a) =>
                """
                a +
                b
                """
                + $@"{a} +
                "
                + @"
                x";
        }
        """");

    [Fact]
    public Task Skipped() => VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Linq;

        public class C
        {
            public int[] Numbers =
            {
                1,
            };

            public int[] More { get; set; }
                = [2];

            public bool M(bool a, bool b)
            {
                Func<int, int> f = x =>
                    x + 1;
                Func<int, int> g = x
                    => x + 1;
                var c = a && // why
                    b;
                var d = a /* why */
                    && b;
                var e = a
                    // why
                    && b;
                var h = a
        #if DEBUG
                    && b
        #endif
                    ;
                var i = a
                    &&
                    b;
                return c && d && e && h && i;
            }

            public System.Collections.Generic.IEnumerable<int> Query(int[] values)
                => from v in values
                select v;

            public static T Convert<T>(object value)
                where T : class
                => value as T;
        }
        """);

    [Fact]
    public Task TokenAloneOnItsLine_IsSkipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public bool M(bool a, bool b) => a
                &&
                b;
        }
        """,
        "dotnet_style_operator_placement_when_wrapping = end_of_line\n");

    [Fact]
    public Task InitializerOnItsOwnLine_IsSkipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int[] Numbers =
            {
                1,
            };
        }
        """,
        "stylebro_equals_placement_when_wrapping = beginning_of_line\n");

    [Fact]
    public Task SyntaxErrors_AreSkipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(int a) => a +{|CS1525:|}
                ;
        }
        """);
}
