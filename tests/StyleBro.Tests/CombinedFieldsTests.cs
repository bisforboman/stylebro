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

    // BRO1505's stylebro_allow_adjacent_single_line_members: single-line event fields may sit together, so the split
    // adds no blank line between them (one whose declaration spans several lines still gets it, and so does a documented
    // one: BRO1513 wants a blank line above its documentation).
    [Fact]
    public Task EventFields_AdjacentSingleLineMembersAllowed() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            {|BRO1114:public|} event EventHandler E1, E2;

            {|BRO1114:public|} event
                EventHandler E3, E4;

            /// <summary>Documented.</summary>
            {|BRO1114:public|} event EventHandler E5, E6;
        }
        """,
        """
        using System;

        public class C
        {
            public event EventHandler E1;
            public event EventHandler E2;

            public event
                EventHandler E3;

            public event
                EventHandler E4;

            /// <summary>Documented.</summary>
            public event EventHandler E5;

            /// <summary>Documented.</summary>
            public event EventHandler E6;
        }
        """,
        editorConfig: "stylebro_allow_adjacent_single_line_members = true");

    // spreadcheetah: 'bool rowRelative = true, columnRelative = true;' was split into explicit 'bool' declarations, and the
    // repository's 'csharp_style_var_for_built_in_types = true:warning' then reported IDE0007 on each.
    [Fact]
    public Task Locals_FollowTheVarPreference() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public void M(object o)
            {
                {|BRO1142:bool|} rowRelative = true, columnRelative = true;
                {|BRO1142:List|}<int> a = new List<int>(), b = new List<int>();
                {|BRO1142:string|} s = (string)o, t = o as string;
                {|BRO1142:const|} int D = 1, E = 2;
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C
        {
            public void M(object o)
            {
                var rowRelative = true;
                var columnRelative = true;
                var a = new List<int>();
                var b = new List<int>();
                var s = (string)o;
                var t = o as string;
                const int D = 1;
                const int E = 2;
            }
        }
        """,
        editorConfig: "csharp_style_var_for_built_in_types = true:warning\ncsharp_style_var_when_type_is_apparent = true:warning\n");

    [Fact]
    public Task Locals_KeepTheirType_WhenVarIsPreferredOnlySilently_OrForOtherKinds() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public void M()
            {
                {|BRO1142:bool|} a = true, b = false;
                {|BRO1142:List|}<int> c = new List<int>(), d = new List<int>();
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C
        {
            public void M()
            {
                bool a = true;
                bool b = false;
                List<int> c = new List<int>();
                List<int> d = new List<int>();
            }
        }
        """,
        editorConfig: "csharp_style_var_for_built_in_types = true:silent\ncsharp_style_var_elsewhere = true:warning\n");

    [Fact]
    public Task Locals_WhereVarWouldChangeTheMeaningOrNotCompile_AreNotSplit() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M()
            {
                long a = 1, b = 2;
                string c = null, d = "x";
                int e, f = 1;
                object g = new(), h = new();
            }
        }
        """,
        "csharp_style_var_for_built_in_types = true:warning\ncsharp_style_var_elsewhere = true:warning\n");
}
