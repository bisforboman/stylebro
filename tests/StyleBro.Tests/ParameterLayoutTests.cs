using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ParameterLayoutAnalyzer, StyleBro.CodeFixes.Readability.ParameterLayoutCodeFixProvider>;

namespace StyleBro.Tests;

public class ParameterLayoutTests
{
    [Fact]
    public Task FirstItem_MovesToTheNextLine() => VerifyFixAsync(
        """
        using System;

        class C
        {
            public C({|BRO1107:int a|},
                int b)
            {
            }

            public int this[{|BRO1107:int a|},
                int b] => a;

            [Obsolete({|BRO1107:"message"|},
                true)]
            void M()
            {
                Math.Max({|BRO1107:1|},
                    2);
                var array = new int[2, 2];
                var x = array[{|BRO1107:0|},
                    1];
                Func<int, int, int> f = ({|BRO1107:p|},
                    q) => p + q;
            }
        }
        """,
        """
        using System;

        class C
        {
            public C(
                int a,
                int b)
            {
            }

            public int this[
                int a,
                int b] => a;

            [Obsolete(
                "message",
                true)]
            void M()
            {
                Math.Max(
                    1,
                    2);
                var array = new int[2, 2];
                var x = array[
                    0,
                    1];
                Func<int, int, int> f = (
                    p,
                    q) => p + q;
            }
        }
        """);

    [Fact]
    public Task MixedLayout_PutsEachItemOnItsOwnLine() => VerifyFixAsync(
        """
        class C
        {
            void M(int a, int b,
                {|BRO1108:int c|})
            {
            }

            void N()
            {
                M(
                    1, 2,
                    {|BRO1108:3|});
                M(
                    1,
                    2, {|BRO1108:3|});
                M(1, 2,
                    {|BRO1108:3|});
            }
        }
        """,
        """
        class C
        {
            void M(
                int a,
                int b,
                int c)
            {
            }

            void N()
            {
                M(
                    1,
                    2,
                    3);
                M(
                    1,
                    2,
                    3);
                M(
                    1,
                    2,
                    3);
            }
        }
        """);

    [Fact]
    public Task MultiLineItems_FollowStyleCop() => VerifyFixAsync(
        """
        class C
        {
            int M(int a, int b, int c, int d) => a;

            void N()
            {
                M(1, 2, 3, M(
                    1, 2, 3, 4));
                M(1, M(
                    1, 2, 3, 4), {|BRO1108:3|}, 4);
                M(
                    M(1, 2, 3, 4
                        ), {|BRO1108:2|},
                    3,
                    4);
            }
        }
        """,
        """
        class C
        {
            int M(int a, int b, int c, int d) => a;

            void N()
            {
                M(1, 2, 3, M(
                    1, 2, 3, 4));
                M(
                    1,
                    M(
                        1, 2, 3, 4),
                    3,
                    4);
                M(
                    M(1, 2, 3, 4
                        ),
                    2,
                    3,
                    4);
            }
        }
        """);

    [Fact]
    public Task MovedLambda_TakesItsBodyAlong() => VerifyFixAsync(
        """
        using System;
        using System.Threading;
        using System.Threading.Tasks;

        class C
        {
            void M(CancellationToken token)
            {
                Func<Task, Task<string>> f = task =>
                    Task.Factory.StartNew<string>({|BRO1107:() =>
                    {
                        throw new InvalidOperationException();
                    }|}, {|BRO1108:token|});
            }
        }
        """,
        """
        using System;
        using System.Threading;
        using System.Threading.Tasks;

        class C
        {
            void M(CancellationToken token)
            {
                Func<Task, Task<string>> f = task =>
                    Task.Factory.StartNew<string>(
                        () =>
                        {
                            throw new InvalidOperationException();
                        },
                        token);
            }
        }
        """);

    [Fact]
    public Task MovedItem_WithAMultiLineString_KeepsItsLines() => VerifyFixAsync(
        """"
        class C
        {
            void M(string a, int b)
            {
                M({|BRO1107:"""
                    text
                    """|}, {|BRO1108:1|});
            }
        }
        """",
        """"
        class C
        {
            void M(string a, int b)
            {
                M(
                    """
                    text
                    """,
                    1);
            }
        }
        """");

    [Fact]
    public Task FixAll_FixesNestedLists() => VerifyFixAsync(
        """
        class C
        {
            int M(int a, int b) => a;

            void N()
            {
                M({|BRO1107:M({|BRO1107:1|},
                    2)|},
                    3);
            }
        }
        """,
        """
        class C
        {
            int M(int a, int b) => a;

            void N()
            {
                M(
                    M(
                        1,
                        2),
                    3);
            }
        }
        """);

    [Fact]
    public Task TabsAndIndentSize_AreRead() => VerifyFixAsync(
        "class C\n{\n\tvoid M(int a, int b,\n\t\t{|BRO1108:int c|})\n\t{\n\t}\n}\n",
        "class C\n{\n\tvoid M(\n\t\tint a,\n\t\tint b,\n\t\tint c)\n\t{\n\t}\n}\n",
        editorConfig: "indent_style = tab");

    [Fact]
    public Task GoodLayouts_AreNotReported() => VerifyNoDiagnosticsAsync("""
        using System.Linq;

        class C
        {
            void M(int a, int b, int c)
            {
            }

            void N(
                int a,
                int b,
                int c)
            {
            }

            void O(
                int a, int b, int c)
            {
            }

            void P()
            {
                M(1, 2, 3);
                var s = string.Join(", ", new[]
                {
                    "a",
                });
                var q = new[] { 1 }.Select(x =>
                    x + 1);
            }
        }
        """);

    [Fact]
    public Task CommentsAndDirectives_AreSkipped() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(int a, // first
                int b)
            {
            }

            void N(int a, int b,
                /* third */ int c)
            {
            }

            void O(int a, int b,
        #if !NEVER
                int c)
        #else
                int d)
        #endif
            {
            }
        }
        """);

    [Fact]
    public Task ItemsAtTheStartOfALine_KeepTheirIndentation() => VerifyFixAsync(
        """
        class C
        {
            void M(int a, int b,
                    {|BRO1108:int c|})
            {
            }
        }
        """,
        """
        class C
        {
            void M(
                int a,
                int b,
                    int c)
            {
            }
        }
        """);

    [Fact]
    public Task SyntaxErrors_AreSkipped() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void M(int a, int b, int c)
            {
                M(1,
                    2, {|CS1525:)|};
            }
        }
        """);

    // BRO1116 joins 'N(' / ')': judged as joined already, the items are all on one line.
    [Fact]
    public Task EmptyParenthesesSplitOverLines_CountAsJoined() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            int M(int a, int b) => a;

            int N() => 1;

            int P() => M(
                N(
                ), M(5, 6));
        }
        """);

    [Fact]
    public Task EmptyParenthesesSplitOverLines_CountWhenBro1116IsOff() => VerifyFixAsync(
        """
        class C
        {
            int M(int a, int b) => a;

            int N() => 1;

            int P() => M(
                N(
                ), {|BRO1108:M(5, 6)|});
        }
        """,
        """
        class C
        {
            int M(int a, int b) => a;

            int N() => 1;

            int P() => M(
                N(
                ),
                M(5, 6));
        }
        """,
        "dotnet_diagnostic.BRO1116.severity = none");
}
