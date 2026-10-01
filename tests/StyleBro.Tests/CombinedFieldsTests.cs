using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.CombinedFieldsAnalyzer, StyleBro.CodeFixes.Readability.CombinedFieldsCodeFixProvider>;

namespace StyleBro.Tests;

public class CombinedFieldsTests
{
    [Fact]
    public Task Fields_AreSplit() => VerifyFixAsync("""
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
                int k, l;
                k = l = 0;
            }
        }
        """, """
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
                int k, l;
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
        """);
}