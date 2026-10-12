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

    // Sonar's S3878 accepts arrays for 'params object[]', 'params Array[]' and jagged 'params' arrays.
    [Fact]
    public Task ExceptObjectAndJagged_LeavesWhatSonarAccepts() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public static int Sum(int first, params int[] rest) => first;

            public static int Objects(params object[] values) => 0;

            public static int Arrays(params System.Array[] values) => 0;

            public static int Jagged(params int[][] values) => 0;

            public void M(int[] other)
            {
                Sum(1, {|BRO1151:new[] { 2, 3 }|});
                Sum(1, {|BRO1151:[2, 3]|});
                Objects(new object[] { 1, "a" });
                Objects([1, "a"]);
                Arrays(new System.Array[] { other, other });
                Jagged(new[] { other, other });
            }
        }
        """,
        """
        public class C
        {
            public static int Sum(int first, params int[] rest) => first;

            public static int Objects(params object[] values) => 0;

            public static int Arrays(params System.Array[] values) => 0;

            public static int Jagged(params int[][] values) => 0;

            public void M(int[] other)
            {
                Sum(1, 2, 3);
                Sum(1, 2, 3);
                Objects(new object[] { 1, "a" });
                Objects([1, "a"]);
                Arrays(new System.Array[] { other, other });
                Jagged(new[] { other, other });
            }
        }
        """,
        "stylebro_params_arrays = except_object_and_jagged\n");

    [Fact]
    public Task ObjectArraysAndJaggedArrays_AreReportedByDefault() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public static int Objects(params object[] values) => 0;

            public static int Jagged(params int[][] values) => 0;

            public void M(int[] other)
            {
                Objects({|BRO1151:new object[] { 1, "a" }|});
                Jagged({|BRO1151:new[] { other, other }|});
            }
        }
        """,
        """
        public class C
        {
            public static int Objects(params object[] values) => 0;

            public static int Jagged(params int[][] values) => 0;

            public void M(int[] other)
            {
                Objects(1, "a");
                Jagged(other, other);
            }
        }
        """);

    // Out of the array, a target-typed element converts to whatever the call wants there: a single 'new()' to the array
    // type itself (eShop: CS8752), the others possibly to another overload. None of these is reported.
    [Fact]
    public Task TargetTypedElements_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Collections.Generic;

        public class Parameter
        {
            public string Name { get; set; } = string.Empty;
        }

        public static class Routes
        {
            public static string Match(this string route, params Parameter[] parameters) => route;

            public static int Run(params Func<int>[] actions) => actions.Length;

            public static int Run(params Action[] actions) => actions.Length;

            public static int Lists(params List<int>[] lists) => lists.Length;

            public static int Values(params int?[] values) => values.Length;

            public static int Numbers(params long[] values) => values.Length;

            public static int Parameters(params Parameter?[] values) => values.Length;

            public static int Get() => 1;

            public static void M(string route, bool flag, int x)
            {
                route.Match([new() { Name = "api-version" }]);
                route.Match([new() { Name = "a" }, new() { Name = "b" }]);
                route.Match(new Parameter[] { new() });
                Run(new Func<int>[] { () => 1 });
                Run(new Func<int>[] { delegate { return 1; } });
                Run(new Func<int>[] { Get });
                Lists(new List<int>[] { [1, 2] });
                Values(new int?[] { default });
                Values(new int?[] { null, 1 });
                Numbers(new long[] { x switch { 1 => 1, _ => 2 } });
                Parameters(new Parameter?[] { flag ? new() : null });
            }
        }
        """);
}
