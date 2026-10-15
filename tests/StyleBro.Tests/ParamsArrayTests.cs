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

    // A single element the call would take as the array itself: the normal form binds, and the one-element array became
    // that value (trial 14: 'Take(new Item[] { default! })' became 'Take(default!)', a null array). The compiler decides,
    // so every shape is covered, however it's wrapped.
    [Fact]
    public Task ElementsThatBindAsTheArray_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        #nullable enable
        public class Item
        {
        }

        public static class Bag
        {
            public static int Take(params Item[] items) => items.Length;

            public static int TakeAll(params object[] values) => values.Length;

            public static int M(object[] objects, object o)
            {
                return Take(new Item[] { default! })
                    + Take(new Item[] { (default)! })
                    + Take(new Item[] { ((default!)) })
                    + Take(new Item[] { (null)! })
                    + TakeAll(new object[] { (object[])null! })
                    + TakeAll(new object[] { (null as object[])! })
                    + TakeAll([objects!])
                    + TakeAll(new object[] { objects })
                    + TakeAll(new object[] { ((objects)) })
                    + TakeAll(new object[] { (object[])o });
            }
        }
        """);

    // The same wrappers around an element that binds as an element: still reported.
    [Fact]
    public Task WrappedElements_AreReported() => Verify.VerifyFixAsync(
        """
        #nullable enable
        public static class Bag
        {
            public static int TakeAll(params object[] values) => values.Length;

            public static int M(object o, string s) => TakeAll({|BRO1151:new object[] { o! }|}) + TakeAll({|BRO1151:new object[] { ((s)) }|});
        }
        """,
        """
        #nullable enable
        public static class Bag
        {
            public static int TakeAll(params object[] values) => values.Length;

            public static int M(object o, string s) => TakeAll(o!) + TakeAll(((s)));
        }
        """);

    // Trial 23: in a netstandard2.0;net8.0;net10.0 project, net8.0's fix expanded string.Join/Task.WhenAll/string.Format
    // calls in the shared file, which net10.0 then bound to .NET 9's 'params ReadOnlySpan' overloads; the same for a
    // method of the project's own with such an overload behind '#if'.
    [Theory]
    [InlineData(null, true, true)]
    [InlineData("net10.0", true, true)]
    [InlineData("netstandard2.0,net8.0", true, false)]
    [InlineData("net9.0,net10.0", true, false)]
    [InlineData("net8.0,net10.0", false, false)]
    [InlineData("netstandard2.0,net8.0,net10.0", false, false)]
    public async Task MultiTargeted_CallsAnotherFrameworkMayBindElsewhere_AreNotReported(string? frameworks, bool library, bool own)
    {
        static string Arg(bool reported, bool fixedCode, string array, string elements) =>
            !reported ? array : fixedCode ? elements : "{|BRO1151:" + array + "|}";
        string Code(bool fixedCode) => $$"""
            public static class Joiner
            {
                public static string Csv(string a, string b) => string.Join(",", {{Arg(library, fixedCode, "new[] { a, b }", "a, b")}});

                public static string Own(string a) => Fmt({{Arg(own, fixedCode, "new[] { a, a }", "a, a")}});

                private static string Fmt(params string[] parts) => string.Concat(parts);
            #if NET9_0_OR_GREATER
                private static string Fmt(params System.ReadOnlySpan<string> parts) => string.Concat(parts);
            #endif
            }
            """;
        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<StyleBro.Analyzers.Readability.ParamsArrayAnalyzer, StyleBro.CodeFixes.Readability.ParamsArrayCodeFixProvider, Microsoft.CodeAnalysis.Testing.DefaultVerifier>
        {
            TestCode = Code(false),
            FixedCode = Code(true),
        };
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", "is_global = true\n" + (frameworks is null ? string.Empty : $"build_property.StyleBroTargetFrameworks = {frameworks}\n")));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n\n[*]\ndotnet_diagnostic.BRO1151.severity = warning\n"));
        await test.RunAsync();
    }
}
