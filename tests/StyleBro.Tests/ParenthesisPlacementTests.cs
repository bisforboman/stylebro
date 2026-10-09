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

    [Fact]
    public Task OwnLine_MovesTheClosingTokenOfASplitList() => VerifyFixAsync(
        """
        using System;

        class Mark : Attribute
        {
            public Mark(int a = 0, int b = 0)
            {
            }
        }

        class C
        {
            [Mark(
                1,
                2{|BRO1110:)|}]
            public int Method(
                int a,
                int b{|BRO1110:)|} => a;

            public int One(int a) => a;

            public int this[
                int i,
                int j{|BRO1110:]|} => i;

            public void Calls(int[,] grid)
            {
                Method(1, 2);
                Method(1,
                    2{|BRO1110:)|};
                Method(
                    Method(
                        1,
                        2{|BRO1110:)|},
                    3{|BRO1110:)|};
                One(Method(
                    1,
                    2{|BRO1110:)|});
                var x = grid[
                    0,
                    1{|BRO1110:]|};
                Method(
                    1,
                    2
                );
                Method(1, 2
                    {|BRO1110:)|};
                Action a = () =>
                {
                };
                Method(
                    1, 2{|BRO1110:)|}; // two
                Run(x =>
                    x.Length > 1
                    && x.Contains('a'){|BRO1110:)|};
                Run(x =>
                    x.StartsWith(
                        "a"{|BRO1110:)|}{|BRO1110:)|};
                Run(x => (x.Length
                    + 1) > 2{|BRO1110:)|};
                Run(x => (x.Length
                    + 1 > 2){|BRO1110:)|};
                Run(x =>
                {
                    return true;
                });
                Run(x =>
                {
                    return true;
                }
                {|BRO1110:)|};
                Run(x => x.StartsWith(
                    "a"{|BRO1110:)|});
                Run(x => x.Length is 1
                    or 2{|BRO1110:)|};
                Run(
                    x =>
                    {
                        return true;
                    }{|BRO1110:)|};
            }

            public bool Run(Func<string, bool> check) => check("a");

            public void Next
                (int a,
                int b{|BRO1110:)|}
            {
            }
        }
        """,
        """
        using System;

        class Mark : Attribute
        {
            public Mark(int a = 0, int b = 0)
            {
            }
        }

        class C
        {
            [Mark(
                1,
                2
            )]
            public int Method(
                int a,
                int b
            ) => a;

            public int One(int a) => a;

            public int this[
                int i,
                int j
            ] => i;

            public void Calls(int[,] grid)
            {
                Method(1, 2);
                Method(1,
                    2
                );
                Method(
                    Method(
                        1,
                        2
                    ),
                    3
                );
                One(Method(
                    1,
                    2
                ));
                var x = grid[
                    0,
                    1
                ];
                Method(
                    1,
                    2
                );
                Method(1, 2);
                Action a = () =>
                {
                };
                Method(
                    1, 2
                ); // two
                Run(x =>
                    x.Length > 1
                    && x.Contains('a')
                );
                Run(x =>
                    x.StartsWith(
                        "a"
                    )
                );
                Run(x => (x.Length
                    + 1) > 2
                );
                Run(x => (x.Length
                    + 1 > 2)
                );
                Run(x =>
                {
                    return true;
                });
                Run(x =>
                {
                    return true;
                });
                Run(x => x.StartsWith(
                    "a"
                ));
                Run(x => x.Length is 1
                    or 2
                );
                Run(
                    x =>
                    {
                        return true;
                    }
                );
            }

            public bool Run(Func<string, bool> check) => check("a");

            public void Next
                (int a,
                int b
                )
            {
            }
        }
        """,
        editorConfig: "stylebro_closing_parenthesis_placement = own_line\ndotnet_diagnostic.BRO1109.severity = none");

    [Fact]
    public Task OwnLine_WithBro1109_IndentsLikeTheNameLine() => VerifyFixAsync(
        """
        class C
        {
            public void Method
                {|BRO1109:(|}int a, int b{|BRO1110:)|}
            {
            }
        }
        """,
        """
        class C
        {
            public void Method(
                int a, int b
            )
            {
            }
        }
        """,
        editorConfig: "stylebro_closing_parenthesis_placement = own_line");

    [Fact]
    public Task OwnLine_ReindentsAClosingTokenOnItsOwnLine() => VerifyFixAsync(
        """
        class C
        {
            public int M(int a, int b) => a;

            public void Method
                {|BRO1109:(|}int a,
                int b
                {|BRO1110:)|}
            {
                M(
                    1,
                    2
                        {|BRO1110:)|};
                M(
                    1,
                    2 // two
        {|BRO1110:)|};
                M(
                    1,
                    2
                );
            }
        }
        """,
        """
        class C
        {
            public int M(int a, int b) => a;

            public void Method(
                int a,
                int b
            )
            {
                M(
                    1,
                    2
                );
                M(
                    1,
                    2 // two
                );
                M(
                    1,
                    2
                );
            }
        }
        """,
        editorConfig: "stylebro_closing_parenthesis_placement = own_line");

    // The indentation of the opening token's line as the other fixes leave it: BRO1105 moves ': base(' down, an inner list's
    // ')' moved down or reindented starts the line, one moved up (not split) or an empty '()' joined to its name by
    // BRO1109 takes the line up.
    [Fact]
    public Task OwnLine_IndentsLikeTheOpeningLineAfterTheOtherFixes() => VerifyFixAsync(
        """
        class B
        {
            public B(int a)
            {
            }
        }

        class C : B
        {
            public C(int a) : base(
                a
            {|BRO1110:)|}
            {
            }

            public int M(int a, int b) => a;

            public C Get() => this;

            public void Run()
            {
                M(M(
                    1, 2{|BRO1110:)|}, M(
                    3, 4
                        {|BRO1110:)|}{|BRO1110:)|};
                M(M(
                    1, 2
                        {|BRO1110:)|}, M(
                    3, 4
                        {|BRO1110:)|}{|BRO1110:)|};
                var x = M(1, 2
                    {|BRO1110:)|} + M(
                    3,
                    4
                        {|BRO1110:)|};
                var y = Get
                    {|BRO1109:(|}).M(
                    1,
                    2
                    {|BRO1110:)|};
                var z = M
                    {|BRO1109:(|}1, M(
                        2,
                        3
                    ){|BRO1110:)|};
            }
        }
        """,
        """
        class B
        {
            public B(int a)
            {
            }
        }

        class C : B
        {
            public C(int a) : base(
                a
                )
            {
            }

            public int M(int a, int b) => a;

            public C Get() => this;

            public void Run()
            {
                M(M(
                    1, 2
                ), M(
                    3, 4
                )
                );
                M(M(
                    1, 2
                ), M(
                    3, 4
                )
                );
                var x = M(1, 2) + M(
                    3,
                    4
                );
                var y = Get().M(
                    1,
                    2
                );
                var z = M(
                    1, M(
                        2,
                        3
                    )
                );
            }
        }
        """,
        editorConfig: "stylebro_closing_parenthesis_placement = own_line");

    // BRO1105's same_line mode leaves ': base(' on the constructor's line.
    [Fact]
    public Task OwnLine_ConstructorInitializerOnTheConstructorsLine() => VerifyNoDiagnosticsAsync(
        """
        class B
        {
            public B(int a)
            {
            }
        }

        class C : B
        {
            public C(int a) : base(
                a
            )
            {
            }
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line\nstylebro_constructor_initializer_placement = same_line");

    [Fact]
    public Task OwnLine_SkipsCommentsAndDirectives() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            void M(int a, int b)
            {
                M(
                    1,
                    2 /* two */);
                System.Action<System.Action> run = null;
                run(() =>
                    { M(1, 2); });
                M(
                    1,
        #if DEBUG
                    2);
        #else
                    3);
        #endif
                M(
                    1,
                    2 // two
                );
                M(
                    1,
                    2
                        /* two */ );
            }
        }
        """,
        "stylebro_closing_parenthesis_placement = own_line");
}
