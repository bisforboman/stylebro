using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ParenthesisPlacementAnalyzer, StyleBro.CodeFixes.Readability.ParenthesisPlacementCodeFixProvider>;

namespace StyleBro.Tests;

public class ParenthesisPlacementTests
{
    [Fact]
    public Task OpeningTokens_MoveUp() => VerifyFixAsync(
        """
        using System;

        class Mark : Attribute
        {
            public Mark(int a = 0)
            {
            }
        }

        class C
        {
            public C
                {|BRO1109:(|})
            {
            }

            public void Method
                {|BRO1109:(|}int a, int b)
            {
            }

            public int this
                {|BRO1109:[|}int i] => i;

            [Mark
                {|BRO1109:(|}1)]
            public void Calls()
            {
                Method
                    {|BRO1109:(|}1, 2);
                var c = new C
                    {|BRO1109:(|});
                var arr = new int[3];
                var x = arr
                    {|BRO1109:[|}0];
                Func<int, int> f = (
                    v) => v;
            }
        }
        """,
        """
        using System;

        class Mark : Attribute
        {
            public Mark(int a = 0)
            {
            }
        }

        class C
        {
            public C()
            {
            }

            public void Method(
                int a, int b)
            {
            }

            public int this[
                int i] => i;

            [Mark(
                1)]
            public void Calls()
            {
                Method(
                    1, 2);
                var c = new C();
                var arr = new int[3];
                var x = arr[
                    0];
                Func<int, int> f = (
                    v) => v;
            }
        }
        """);

    [Fact]
    public Task ClosingTokens_MoveUp() => VerifyFixAsync(
        """
        using System;

        class C
        {
            public void Method(
                int a,
                int b
                {|BRO1110:)|}
            {
            }

            public void Commented(
                int a,
                int b // last
                {|BRO1110:)|}
            {
            }

            public int this[
                int i
                {|BRO1110:]|} => i;

            public void Calls()
            {
                Method(
                    1,
                    2
                    {|BRO1110:)|};
                Method(1, Math.Max(
                    1,
                    2
                    {|BRO1110:)|});
                Func<int, int> f = (
                    v
                    {|BRO1110:)|} => v;
            }
        }
        """,
        """
        using System;

        class C
        {
            public void Method(
                int a,
                int b)
            {
            }

            public void Commented(
                int a,
                int b) // last
            {
            }

            public int this[
                int i] => i;

            public void Calls()
            {
                Method(
                    1,
                    2);
                Method(1, Math.Max(
                    1,
                    2));
                Func<int, int> f = (
                    v) => v;
            }
        }
        """);

    [Fact]
    public Task BothInOneList() => VerifyFixAsync(
        """
        class C
        {
            void M(int a, int b)
            {
                M
                    {|BRO1109:(|}1,
                    2
                    {|BRO1110:)|};
            }
        }
        """,
        """
        class C
        {
            void M(int a, int b)
            {
                M(
                    1,
                    2);
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void Empty(
                )
            {
            }

            void CommentBeforeOpen /* why */
                (int a)
            {
            }

            void CommentOnOwnLine(
                int a
                // done
                )
            {
            }

            void Directive(
                int a
        #if !NEVER
                )
        #else
                )
        #endif
            {
            }

            void M(int a, int b)
            {
                M(
                    1,
                    2 // two
                    ); var x = 1;
                System.Func<int, int> f =
                    (v) => v;
            }
        }
        """);
}
