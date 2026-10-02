using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ConstraintPlacementAnalyzer, StyleBro.CodeFixes.Readability.ConstraintPlacementCodeFixProvider>;

namespace StyleBro.Tests;

public class ConstraintPlacementTests
{
    [Fact]
    public Task Constraints_MoveToTheirOwnLine() => VerifyFixAsync(
        """
        using System;

        public class Box<T> {|BRO1111:where T : class|}
        {
        }

        public class Pair<TKey, TValue> {|BRO1111:where TKey : IComparable<TKey>|} {|BRO1111:where TValue : new()|}
        {
        }

        public class Split<TA, TB>
            where TA : class {|BRO1111:where TB : class|}
        {
        }

        public interface IRepo<T> {|BRO1111:where T : class|}
        {
            TOut Map<TOut>(T item) {|BRO1111:where TOut : new()|};
        }

        public delegate void Handler<T>(T value) {|BRO1111:where T : class|};

        public static class Methods
        {
            public static void M<T>(T value) {|BRO1111:where T : class|}
            {
            }

            public static void Expression<T>(T value) {|BRO1111:where T : class|} => Console.WriteLine(value);

            public static void Local()
            {
                void Inner<T>(T value) {|BRO1111:where T : class|}
                {
                }
            }
        }
        """,
        """
        using System;

        public class Box<T>
            where T : class
        {
        }

        public class Pair<TKey, TValue>
            where TKey : IComparable<TKey>
            where TValue : new()
        {
        }

        public class Split<TA, TB>
            where TA : class
            where TB : class
        {
        }

        public interface IRepo<T>
            where T : class
        {
            TOut Map<TOut>(T item)
                where TOut : new();
        }

        public delegate void Handler<T>(T value)
            where T : class;

        public static class Methods
        {
            public static void M<T>(T value)
                where T : class
            {
            }

            public static void Expression<T>(T value)
                where T : class
                => Console.WriteLine(value);

            public static void Local()
            {
                void Inner<T>(T value)
                    where T : class
                {
                }
            }
        }
        """);

    [Fact]
    public Task Tabs_AreRead() => VerifyFixAsync(
        "class Box<T> {|BRO1111:where T : class|}\n{\n}\n",
        "class Box<T>\n\twhere T : class\n{\n}\n",
        editorConfig: "indent_style = tab");

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        public class Box<T>
            where T : class
        {
        }

        public class Commented<T> /* why */ where T : class
        {
        }
        """);
}
