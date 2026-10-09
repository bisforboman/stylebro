using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ParamsArrayAnalyzer, StyleBro.CodeFixes.Readability.ParamsArrayCodeFixProvider>;

namespace StyleBro.Tests;

public class ParamsArrayTests
{
    [Fact]
    public Task ArraysForParams_BecomeTheElements() => Verify.VerifyFixAsync(
        """
        public class Joiner
        {
            public Joiner(params string[] parts)
            {
            }

            public static int Sum(int first, params int[] rest) => first + rest.Length;

            public static string Join(params object[] values) => string.Concat(values);

            public void M()
            {
                var a = Sum(1, {|BRO1151:new[] { 2, 3 }|});
                var b = Sum(1, {|BRO1151:new int[] { 2, 3, }|});
                var c = Sum(1, {|BRO1151:new int[] { }|});
                var d = Sum(0, {|BRO1151:[3, 4]|});
                var e = new Joiner({|BRO1151:new[] { "x", "y" }|});
                var f = Join({|BRO1151:new object[] { "one", 2 }|});
                var g = Join({|BRO1151:new object[] { 1 }|});
                var h = Sum(Sum(0, {|BRO1151:new[] { 1 }|}), {|BRO1151:new[] { Sum(1, {|BRO1151:new[] { 2 }|}) }|});
                var i = Sum(0, {|BRO1151:new[] { 1, /* two */ 2 }|});
                var j = Sum(
                    0,
                    {|BRO1151:new[] { 1 }|});
            }
        }
        """,
        """
        public class Joiner
        {
            public Joiner(params string[] parts)
            {
            }

            public static int Sum(int first, params int[] rest) => first + rest.Length;

            public static string Join(params object[] values) => string.Concat(values);

            public void M()
            {
                var a = Sum(1, 2, 3);
                var b = Sum(1, 2, 3);
                var c = Sum(1);
                var d = Sum(0, 3, 4);
                var e = new Joiner("x", "y");
                var f = Join("one", 2);
                var g = Join(1);
                var h = Sum(Sum(0, 1), Sum(1, 2));
                var i = Sum(0, 1, /* two */ 2);
                var j = Sum(
                    0,
                    1);
            }
        }
        """);

    [Fact]
    public Task OtherArrays_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public static int Sum(params int[] values) => values.Length;

            public static int Sum(int a, int b) => a + b;

            public static int Count(params object[] values) => values.Length;

            public static int Take(int[] values) => values.Length;

            public static int Pair(int first, params int[] rest) => first + rest.Length;

            public static int Generic<T>(params T[] values) => values.Length;

            public void M(object[] objects, string[] strings)
            {
                var a = Sum(new[] { 1, 2 });
                var b = Count(new object[] { null });
                var c = Count(new object[] { objects });
                var d = Count(new string[] { "a", "b" });
                var e = Take(new[] { 1, 2 });
                var f = Sum(new int[2]);
                var g = Generic(new object[] { "a", "b" });
                var h = Sum(values: new[] { 1, 2, 3 });
                var j = Sum(new[] /* three */ { 1, 2, 3 });
                var k = Sum(new[]
                {
                    1,
                    2,
                    3,
                });
                var l = Count([.. strings, "c"]);
                var n = Count(objects, new object[] { 1, 2 });
                var m = Pair(1,
                    new[] { 2, 3 });
            }
        }
        """);
}
