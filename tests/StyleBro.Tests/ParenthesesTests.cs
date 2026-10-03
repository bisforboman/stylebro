using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.ParenthesesAnalyzer, StyleBro.CodeFixes.Maintainability.ParenthesesCodeFixProvider>;

namespace StyleBro.Tests;

public class ParenthesesTests
{
    [Fact]
    public Task UnnecessaryParentheses_AreRemoved() => VerifyFixAsync(
        """
        public class C
        {
            private int field = {|BRO1405:(1 + 2)|};

            public int M(int a, int b)
            {
                var x = {|BRO1405:(a)|};
                x = {|BRO1405:(a * b)|};
                Use({|BRO1405:(a + b)|}, {|BRO1405:( b )|});
                var s = {|BRO1405:(a.ToString())|}.Length;
                var w = -{|BRO1405:(a)|};
                System.Func<int, int> h = {|BRO1405:(v => v + 1)|};
                return{|BRO1405:(x)|} + w + h(1);
            }

            public int N(int a) => {|BRO1405:({|BRO1405:(a)|})|};

            private static void Use(int x, int y)
            {
            }
        }
        """,
        """
        public class C
        {
            private int field = 1 + 2;

            public int M(int a, int b)
            {
                var x = a;
                x = a * b;
                Use(a + b, b);
                var s = a.ToString().Length;
                var w = -a;
                System.Func<int, int> h = v => v + 1;
                return x + w + h(1);
            }

            public int N(int a) => a;

            private static void Use(int x, int y)
            {
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        #if !(DEBUG)
        using System;
        #else
        using System;
        #endif

        public class C
        {
            public object M(int a, int b, string s, int? n, bool f)
            {
                // Inside an operator expression: they say how it groups, or make it clearer.
                var x = a + (b * a);
                var y = n ?? (n ?? 1);
                var z = (a + b).ToString();
                var c = (object)(-a);
                var d = $"{(f ? 1 : 2)}";
                var e = (s?.Length).ToString();
                var g = (a switch { 1 => "one", _ => "other" }).Length;

                // Removing them would make this a generic method call.
                Use(a < b, (b > (a + 1)));
                return x + y + z + c + d + e + g;
            }

            private static void Use(bool x, bool y)
            {
            }
        }
        """);
}
