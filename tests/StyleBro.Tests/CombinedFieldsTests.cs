using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.CombinedFieldsAnalyzer, StyleBro.CodeFixes.Readability.CombinedFieldsCodeFixProvider>;

namespace StyleBro.Tests;

public class CombinedFieldsTests
{
    [Fact]
    public Task Fields_AreSplit() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            {|BRO1114:private|} int a, b;
            {|BRO1114:private|} readonly string c = "c", d = "d";

            {|BRO1114:[|}Obsolete]
            public static int e, f;

            /// <summary>Documented.</summary>
            {|BRO1114:private|} int g, h;

            {|BRO1114:public|} event EventHandler E1, E2;

            {|BRO1114:const|} string
                First = "a",
                Second = "b";

            public void M()
            {
                {|BRO1142:int|} k, l;
                k = l = 0;
            }
        }
        """,
        """
        using System;

        public class C
        {
            private int a;
            private int b;
            private readonly string c = "c";
            private readonly string d = "d";

            [Obsolete]
            public static int e;
            [Obsolete]
            public static int f;

            /// <summary>Documented.</summary>
            private int g;
            /// <summary>Documented.</summary>
            private int h;

            public event EventHandler E1;

            public event EventHandler E2;

            const string First = "a";
            const string Second = "b";

            public void M()
            {
                int k;
                int l;
                k = l = 0;
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        public class C
        {
            private int a, /* the second */ b;
        }

        public class D { private int c, d; }
        """);

    [Fact]
    public Task Locals_AreSplit() => VerifyFixAsync(
        """
        using System;
        using System.IO;

        public class C
        {
            public int M(int[] values)
            {
                {|BRO1142:int|} a = 1, b, c = a + 1;
                {|BRO1142:const|} int D = 1, E = D + 1;
                {|BRO1142:int|} x = 0, y = 1;
                {|BRO1142:int|} m = Math.Max(
                    1,
                    2), n = 3;
                Func<int> f = () =>
                {
                    {|BRO1142:int|} g = 1, h = 2;
                    return g + h;
                };
                b = 2;
                return a + b + c + D + E + f() + values.Length + x + y + m + n;
            }

            public int N(int v)
            {
                switch (v)
                {
                    case 1: {|BRO1142:int|} e = 1, f = 2; return e + f;
                    default: return 0;
                }
            }
        }
        """,
        """
        using System;
        using System.IO;

        public class C
        {
            public int M(int[] values)
            {
                int a = 1;
                int b;
                int c = a + 1;
                const int D = 1;
                const int E = D + 1;
                int x = 0;
                int y = 1;
                int m = Math.Max(
                    1,
                    2);
                int n = 3;
                Func<int> f = () =>
                {
                    int g = 1;
                    int h = 2;
                    return g + h;
                };
                b = 2;
                return a + b + c + D + E + f() + values.Length + x + y + m + n;
            }

            public int N(int v)
            {
                switch (v)
                {
                    case 1: int e = 1; int f = 2; return e + f;
                    default: return 0;
                }
            }
        }
        """);

    [Fact]
    public Task LocalsThatStayTogether_AreNotReported() => VerifyNoDiagnosticsAsync("""
        using System.IO;

        public class C
        {
            public int M(int[] values)
            {
                for (int i = 0, j = 1; i < j; i++)
                {
                }

                using Stream a = new MemoryStream(), b = new MemoryStream();
                int c = 1, /* why */ d = 2;
                return c + d + values.Length;
            }
        }
        """);
}
