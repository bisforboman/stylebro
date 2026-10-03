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
