using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.PrecedenceAnalyzer, StyleBro.CodeFixes.Maintainability.PrecedenceCodeFixProvider>;

namespace StyleBro.Tests;

public class PrecedenceTests
{
    [Fact]
    public Task Operands_GetParentheses() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a, int b, int c, int d, int e)
            {
                var x = a + {|BRO1406:b * c|};
                var y = {|BRO1406:a * b|} % c;
                var z = a << {|BRO1406:b + c|};
                var w = {|BRO1406:a * b|} + {|BRO1406:{|BRO1406:c * d|} % e|};
                return x + y + z + w;
            }

            public bool N(bool a, bool b, bool c, int v)
            {
                return a || {|BRO1407:b && c|} || {|BRO1407:v is {|BRO1407:> 1 and < 5|} or 10 && a|};
            }

            public bool P(int v) => v is {|BRO1407:> 1 and < 5|} or 10;
        }
        """,
        """
        public class C
        {
            public int M(int a, int b, int c, int d, int e)
            {
                var x = a + (b * c);
                var y = (a * b) % c;
                var z = a << (b + c);
                var w = (a * b) + ((c * d) % e);
                return x + y + z + w;
            }

            public bool N(bool a, bool b, bool c, int v)
            {
                return a || (b && c) || (v is (> 1 and < 5) or 10 && a);
            }

            public bool P(int v) => v is (> 1 and < 5) or 10;
        }
        """);

    [Fact]
    public Task NeverIfUnnecessary_TurnsTheGroupOff() => VerifyFixAsync(
        """
        public class C
        {
            public bool M(int a, int b, bool f, bool g) => a + b * a > 0 || {|BRO1407:f && g|};
        }
        """,
        """
        public class C
        {
            public bool M(int a, int b, bool f, bool g) => a + b * a > 0 || (f && g);
        }
        """,
        editorConfig: "dotnet_style_parentheses_in_arithmetic_binary_operators = never_if_unnecessary:silent");

    // Like StyleCop 1.3 (measured on 1.3.177-beta): '&', '|' and '^' are each a family of their own, '>>>' is a shift.
    // Under a bitwise operator a comparison is an operand of another kind too.
    [Fact]
    public Task BitwiseAndUnsignedShiftOperands_GetParentheses() => VerifyFixAsync(
        """
        public class C
        {
            public void M(int a, int b, int c, bool f, bool g, bool h, object o)
            {
                var x01 = {|BRO1406:a & b|} | c;
                var x02 = a | {|BRO1406:b & c|};
                var x03 = a ^ {|BRO1406:b & c|};
                var x04 = {|BRO1406:a ^ b|} | c;
                var x05 = {|BRO1406:a + b|} & c;
                var x06 = a & {|BRO1406:b + c|};
                var x07 = {|BRO1406:a * b|} | c;
                var x08 = {|BRO1406:a << b|} & c;
                var x09 = a >>> {|BRO1406:b + c|};
                var x10 = {|BRO1406:a + b|} >>> c;
                var x11 = {|BRO1406:a >>> b|} & c;
                var x12 = {|BRO1406:a == b|} & {|BRO1406:c == a|};
                var x13 = {|BRO1406:f & g|} | h;
                var x14 = f & {|BRO1406:g == h|};
                var x15 = {|BRO1406:a < b|} | f;
                var x16 = {|BRO1406:a % b|} & c;
                var x17 = a ^ {|BRO1406:b + c|};
                var x18 = a | {|BRO1406:b << c|};
                var x19 = {|BRO1406:a & b|} | {|BRO1406:c ^ {|BRO1406:a * b|}|};
                var x20 = f & {|BRO1406:o is string|};
            }
        }
        """,
        """
        public class C
        {
            public void M(int a, int b, int c, bool f, bool g, bool h, object o)
            {
                var x01 = (a & b) | c;
                var x02 = a | (b & c);
                var x03 = a ^ (b & c);
                var x04 = (a ^ b) | c;
                var x05 = (a + b) & c;
                var x06 = a & (b + c);
                var x07 = (a * b) | c;
                var x08 = (a << b) & c;
                var x09 = a >>> (b + c);
                var x10 = (a + b) >>> c;
                var x11 = (a >>> b) & c;
                var x12 = (a == b) & (c == a);
                var x13 = (f & g) | h;
                var x14 = f & (g == h);
                var x15 = (a < b) | f;
                var x16 = (a % b) & c;
                var x17 = a ^ (b + c);
                var x18 = a | (b << c);
                var x19 = (a & b) | (c ^ (a * b));
                var x20 = f & (o is string);
            }
        }
        """);

    [Fact]
    public Task BitwiseAndShiftOperations_OfOneFamilyOrUnderOtherOperators_NotReported() => VerifyNoDiagnosticsAsync("""
        public class C
        {
            public void M(int a, int b, int c, bool f, bool g, bool h, object o)
            {
                var x01 = a & b & c;
                var x02 = a | b | c;
                var x03 = a ^ b ^ c;
                var x04 = a >>> b >> c;
                var x05 = a >>> b << c;
                var x06 = a >>> b >>> c;
                var x07 = f | g && h;
                var x08 = f && g | h;
                var x09 = f || g & h;
                var x10 = a + b == c;
                var x11 = (a & b) == c ? 1 : 2;
                var x12 = a + b > c;
                var x13 = (a & b) | (c & a);
                var x14 = a & -b & ~c & (int)a;
                var x15 = f & o is string { Length: 1 };
                a |= b & c;
            }
        }
        """);

    // Like StyleCop 1.3: '??' is a third family next to '&&' and '||'.
    [Fact]
    public Task CoalesceMixedWithLogicalOperators_GetsParentheses() => VerifyFixAsync(
        """
        public class T
        {
            public static T operator &(T left, T right) => left;

            public static T operator |(T left, T right) => left;

            public static bool operator true(T value) => true;

            public static bool operator false(T value) => false;
        }

        public class C
        {
            public void M(bool f, bool g, bool h, bool? n, T t, T u, T v)
            {
                var y01 = n ?? {|BRO1407:f && g|};
                var y02 = n ?? {|BRO1407:f || g|};
                var y03 = {|BRO1407:t && u|} ?? v;
                var y04 = {|BRO1407:t || u|} ?? v;
                var y05 = n ?? {|BRO1407:{|BRO1407:f && g|} || h|};
                var y06 = {|BRO1407:{|BRO1407:t && u|} || v|} ?? t;
                var y07 = f ? g && h : n ?? {|BRO1407:g && h|};
                var y08 = {|BRO1407:t && u|} ?? {|BRO1407:u || v|};
            }
        }
        """,
        """
        public class T
        {
            public static T operator &(T left, T right) => left;

            public static T operator |(T left, T right) => left;

            public static bool operator true(T value) => true;

            public static bool operator false(T value) => false;
        }

        public class C
        {
            public void M(bool f, bool g, bool h, bool? n, T t, T u, T v)
            {
                var y01 = n ?? (f && g);
                var y02 = n ?? (f || g);
                var y03 = (t && u) ?? v;
                var y04 = (t || u) ?? v;
                var y05 = n ?? ((f && g) || h);
                var y06 = ((t && u) || v) ?? t;
                var y07 = f ? g && h : n ?? (g && h);
                var y08 = (t && u) ?? (u || v);
            }
        }
        """);

    [Fact]
    public Task Coalesce_AloneOrWithOtherOperators_NotReported() => VerifyNoDiagnosticsAsync("""
        public class C
        {
            public void M(int a, int b, int c, bool f, bool g, bool? n, int? m, object o, object p)
            {
                var y01 = n ?? n ?? f;
                var y02 = o ?? p ?? o;
                var y03 = n ?? f == g;
                var y04 = n ?? f & g;
                var y05 = m + b ?? c;
                var y06 = m ?? b + c;
                var y07 = f && (n ?? g);
                var y08 = n ?? !f;
                var y09 = m ?? b << c;
                var y10 = n ?? (f && g);
                var y11 = n ?? a < b;
                var y12 = (n ?? f) || g;
                o ??= f && g;
            }
        }
        """);

    // The SDK's groups: '&', '|', '^' and the shifts are arithmetic operators, '??' is one of the other operators.
    [Theory]
    [InlineData("arithmetic", "a & b", "{|BRO1407:f && g|}", "a & b", "(f && g)")]
    [InlineData("other", "{|BRO1406:a & b|}", "f && g", "(a & b)", "f && g")]
    public Task NeverIfUnnecessary_TurnsTheNewOperatorsOffWithTheirGroup(string group, string bitwise, string logical, string fixedBitwise, string fixedLogical)
    {
        const string Source = """
            public class C
            {
                public bool M(int a, int b, int c, bool f, bool g, bool? n) => (BITWISE | c) > 0 && (n ?? LOGICAL);
            }
            """;
        return VerifyFixAsync(
            Source.Replace("BITWISE", bitwise, StringComparison.Ordinal).Replace("LOGICAL", logical, StringComparison.Ordinal),
            Source.Replace("BITWISE", fixedBitwise, StringComparison.Ordinal).Replace("LOGICAL", fixedLogical, StringComparison.Ordinal),
            editorConfig: $"dotnet_style_parentheses_in_{group}_binary_operators = never_if_unnecessary");
    }

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        #if DEBUG || TRACE && RELEASE
        #endif
        public class C
        {
            public bool M(int a, int b, int c, bool f, bool g)
            {
                var x = a + b - c;
                var y = a * b / c;
                var z = a >> b << c;
                var w = (a + b) * c;
                return (f && g && x > y) || false == (z < w);
            }
        }
        """);
}
