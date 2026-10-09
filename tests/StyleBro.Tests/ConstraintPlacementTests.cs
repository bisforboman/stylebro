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
        editorConfig: "indent_style = tab\nstylebro_constraint_placement = own_line");

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

    // stylebro_constraint_placement = same_line: every clause joins the declaration's line (one space), all or nothing,
    // when the joined line (up to the last clause) fits max_line_length. The '=>' of an expression body stays; the ')'
    // that BRO1110 moves to the last parameter moves in the same edit.
    [Fact]
    public Task SameLine_ConstraintsJoinTheDeclaration() => VerifyFixAsync(
        """
        using System;

        public class Box<T>
            {|BRO1111:where T : class|}
        {
        }

        public class Pair<TKey, TValue> where TKey : class
            {|BRO1111:where TValue : new()|}
        {
        }

        public static class Methods
        {
            public static void M<T>(
                T value)
                {|BRO1111:where T : class|}
            {
            }

            public static void N<T>(
                T value
            )
                {|BRO1111:where T : class|}
            {
            }

            public static void Expression<T>(T value)
                {|BRO1111:where T : class|}
                => Console.WriteLine(value);
        }
        """,
        """
        using System;

        public class Box<T> where T : class
        {
        }

        public class Pair<TKey, TValue> where TKey : class where TValue : new()
        {
        }

        public static class Methods
        {
            public static void M<T>(
                T value) where T : class
            {
            }

            public static void N<T>(
                T value) where T : class
            {
            }

            public static void Expression<T>(T value) where T : class
                => Console.WriteLine(value);
        }
        """,
        editorConfig: "stylebro_constraint_placement = same_line\nmax_line_length = 71");

    [Fact]
    public Task SameLine_WithoutBro1110_TheParenthesisStays() => VerifyFixAsync(
        """
        public static class Methods
        {
            public static void M<T>(
                T value
            )
                {|BRO1111:where T : class|}
            {
            }
        }
        """,
        """
        public static class Methods
        {
            public static void M<T>(
                T value
            ) where T : class
            {
            }
        }
        """,
        editorConfig: "stylebro_constraint_placement = same_line\ndotnet_diagnostic.BRO1110.severity = none");

    // Too long together (73 > 71; also when the first clause is there already), a comment in a gap (also one BRO1110's
    // ')' would carry along), a clause on several lines, already joined.
    [Fact]
    public Task SameLine_NotReported() => VerifyNoDiagnosticsAsync(
        """
        public class Box<TItem, TOther>
            where TItem : class
            where TOther : struct
        {
        }

        public class Box2<TItem, TOther> where TItem : class
            where TOther : struct
        {
        }

        public class Commented<T> // why
            where T : class
        {
        }

        public static class Methods
        {
            public static void M<T>(
                T value // why
            )
                where T : class
            {
            }
        }

        public class Split<T>
            where T :
                class
        {
        }

        public class Joined<T> where T : class
        {
        }
        """,
        "stylebro_constraint_placement = same_line\nmax_line_length = 71");

    // The line is measured as the other fixes leave it: BRO1108 splits the parameters ('        T dd) where T : class',
    // 29), BRO1109 moves '(' up ('        T abcdefg) where T : class', 34). As the text stands, each joined line would be
    // too long (35 > 34).
    [Fact]
    public Task SameLine_MeasuredAsTheOtherFixesLeaveTheLine() => VerifyFixAsync(
        """
        public static class C
        {
            public static void M<T>(int a, int b,
                T cc, T dd)
                {|BRO1111:where T : class|}
            {
            }

            public static void N<T>
                (T abcdefg)
                {|BRO1111:where T : class|}
            {
            }
        }
        """,
        """
        public static class C
        {
            public static void M<T>(int a, int b,
                T cc, T dd) where T : class
            {
            }

            public static void N<T>
                (T abcdefg) where T : class
            {
            }
        }
        """,
        editorConfig: "stylebro_constraint_placement = same_line\nmax_line_length = 34");

    // BRO1404 adds 'internal ' (37 > 36) and 'private ' (42 > 36), BRO1007 another part's 'public ' (41 > 36).
    [Fact]
    public Task SameLine_NotReported_WhenAnAddedModifierMakesItTooLong() => VerifyNoDiagnosticsAsync(
        """
        class Box<T>
            where T : class
        {
            void M<U>(U v)
                where U : class
            {
            }
        }

        partial class P<T>
            where T : class
        {
        }

        public partial class P<T>
        {
        }
        """,
        "stylebro_constraint_placement = same_line\nmax_line_length = 36");

    [Fact]
    public Task SameLine_WithoutBro1404AndBro1007_TheModifiersAreNotCounted() => VerifyFixAsync(
        """
        class Box<T>
            {|BRO1111:where T : class|}
        {
        }

        partial class P<T>
            {|BRO1111:where T : class|}
        {
        }

        public partial class P<T>
        {
        }
        """,
        """
        class Box<T> where T : class
        {
        }

        partial class P<T> where T : class
        {
        }

        public partial class P<T>
        {
        }
        """,
        editorConfig: "stylebro_constraint_placement = same_line\nmax_line_length = 36\ndotnet_diagnostic.BRO1404.severity = none\ndotnet_diagnostic.BRO1007.severity = none");
}
