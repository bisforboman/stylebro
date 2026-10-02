using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.TupleElementNameAnalyzer, StyleBro.CodeFixes.Readability.TupleElementNameCodeFixProvider>;

namespace StyleBro.Tests;

public class TupleElementNameTests
{
    [Fact]
    public Task ItemN_BecomesTheName() => Verify.VerifyFixAsync("""
        class C
        {
            (int Count, string Name) Get() => (1, "a");

            void M((int a, int b)? maybe)
            {
                var t = Get();
                var x = t.{|BRO1124:Item1|} + t.{|BRO1124:Item2|}.Length;
                t.{|BRO1124:Item1|} = 2;
                var y = maybe?{|BRO1124:.Item2|};
                var z = Get().{|BRO1124:Item2|};
            }
        }
        """, """
        class C
        {
            (int Count, string Name) Get() => (1, "a");

            void M((int a, int b)? maybe)
            {
                var t = Get();
                var x = t.Count + t.Name.Length;
                t.Count = 2;
                var y = maybe?.b;
                var z = Get().Name;
            }
        }
        """);

    [Fact]
    public Task UnnamedElementsNameofAndNames_AreNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        class C
        {
            void M((int, int b) t, System.Tuple<int, int> old)
            {
                var a = t.Item1;
                var b = t.b;
                var c = nameof(t.Item2);
                var d = old.Item1;
            }
        }
        """);

    [Fact]
    public Task EighthElement_UsesItsName() => Verify.VerifyFixAsync("""
        class C
        {
            int M((int a, int b, int c, int d, int e, int f, int g, int h) t) => t.{|BRO1124:Item8|};
        }
        """, """
        class C
        {
            int M((int a, int b, int c, int d, int e, int f, int g, int h) t) => t.h;
        }
        """);
}
