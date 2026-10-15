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

    // eShop's minimal APIs: the lambda after the moved first argument starts with an attribute list on its own line, and
    // keeps its own lines where they are.
    [Fact]
    public Task AttributedLambda_KeepsItsLines() => VerifyFixAsync(
        """
        using System;

        class ProducesAttribute : Attribute
        {
        }

        class C
        {
            static C Map(string pattern, Delegate handler) => new C();

            C Named(string name) => this;

            void M()
            {
                Map({|BRO1107:"/catalogtypes"|},
                    [Produces]
                    async (int context) => await System.Threading.Tasks.Task.FromResult(context))
                    .Named("ListItemTypes");
                Map({|BRO1107:"/catalogbrands"|},
                    [Produces]
                    (int context) =>
                    {
                        return context;
                    });
            }
        }
        """,
        """
        using System;

        class ProducesAttribute : Attribute
        {
        }

        class C
        {
            static C Map(string pattern, Delegate handler) => new C();

            C Named(string name) => this;

            void M()
            {
                Map(
                    "/catalogtypes",
                    [Produces]
                    async (int context) => await System.Threading.Tasks.Task.FromResult(context))
                    .Named("ListItemTypes");
                Map(
                    "/catalogbrands",
                    [Produces]
                    (int context) =>
                    {
                        return context;
                    });
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

    [Fact]
    public Task SameLine_KeepsTheFirstItem_AndCopiesTheListsIndentation() => VerifyFixAsync(
        """
        class C
        {
            public C(int a,
                int b)
            {
            }

            public int Sum(int a,
                           int b, {|BRO1108:int c|}) => a;

            public void Indented(int a, int b,
                {|BRO1108:int c|})
            {
            }

            public void NoneStartsALine(int a, int b, int c, int d)
            {
                NoneStartsALine(a, b, (a
                    + b), {|BRO1108:d|});
                Indented(
                    a, b,
                    {|BRO1108:c|});
            }
        }
        """,
        """
        class C
        {
            public C(int a,
                int b)
            {
            }

            public int Sum(int a,
                           int b,
                           int c) => a;

            public void Indented(int a,
                int b,
                int c)
            {
            }

            public void NoneStartsALine(int a, int b, int c, int d)
            {
                NoneStartsALine(a,
                    b,
                    (a
                        + b),
                    d);
                Indented(
                    a,
                    b,
                    c);
            }
        }
        """,
        editorConfig: "stylebro_split_list_first_item = same_line");

    // realworlddotnet: init turned BRO1107 off (the code mixes both forms), and BRO1108 still moved the first item.
    [Fact]
    public Task WithBro1107Off_TheFirstItemStaysOnTheParenthesisLine() => VerifyFixAsync(
        """
        class C
        {
            public int UpdateBySlugAsync(string slug,
                int request, {|BRO1108:int cancellationToken|}) => 0;

            public int Aligned(string slug, int request,
                               {|BRO1108:int cancellationToken|}) => 0;
        }
        """,
        """
        class C
        {
            public int UpdateBySlugAsync(string slug,
                int request,
                int cancellationToken) => 0;

            public int Aligned(string slug,
                               int request,
                               int cancellationToken) => 0;
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1107.severity = none");

    // metadata-extractor: init turns BRO1108 off for grouped items; BRO1107 then moves only the first item, the groups stay.
    [Fact]
    public Task WithBro1108Off_GroupedItemsStayGrouped() => VerifyFixAsync(
        """
        class C
        {
            string M(int tag, int index, string a, string b) => a;

            string N() => M({|BRO1107:1|},
                2, "a", "b");
        }
        """,
        """
        class C
        {
            string M(int tag, int index, string a, string b) => a;

            string N() => M(
                1,
                2, "a", "b");
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1108.severity = none");

    // spreadcheetah: 'Assert.Equal(@"Address line 2 (verbatim' + 'string: "", \)", name.Name)' was split around the literal.
    [Fact]
    public Task ListsWithAMultiLineString_AreLeftAlone() => VerifyNoDiagnosticsAsync(
        """"
        class C
        {
            public static void Equal(string expected, string actual)
            {
            }

            public void M(string name)
            {
                Equal(@"Address line 2 (verbatim
        string: "", \)", name);
                Equal("""
                    Age (
                        raw
                    )
                    """, name);
                Equal($@"Line {name}
        next", name);
            }
        }
        """");

    // CommunityToolkit (owner's choice 2026-10-15): 'Register(c =>' / '{ ... }' / '}, Kind);' stays with the option on; a list
    // of another shape, or a first argument that isn't a lambda, anonymous method or anonymous object, is still reported.
    [Fact]
    public Task LeadingMultiLineLambda_StaysWithTheOption() => VerifyFixAsync(
        """
        using System;

        class C
        {
            static void Register(Action<int> action, string kind, int order) { }

            static void Show(object value, string kind) { }

            void M()
            {
                Register(x =>
                {
                    Console.WriteLine(x);
                }, "Field", 1);
                Register(delegate (int x)
                {
                    Console.WriteLine(x);
                }, "Method", 2);
                Show(new
                {
                    Name = "a",
                }, "Value");
                Register({|BRO1107:x =>
                {
                    Console.WriteLine(x);
                }|}, {|BRO1108:"Field"|},
                    3);
                Show({|BRO1107:Value()|},
                    "Value");
                Show(
                    new
                    {
                        Name = "b",
                    }, {|BRO1108:"Value"|});
            }

            static object Value() => 1;
        }
        """,
        """
        using System;

        class C
        {
            static void Register(Action<int> action, string kind, int order) { }

            static void Show(object value, string kind) { }

            void M()
            {
                Register(x =>
                {
                    Console.WriteLine(x);
                }, "Field", 1);
                Register(delegate (int x)
                {
                    Console.WriteLine(x);
                }, "Method", 2);
                Show(new
                {
                    Name = "a",
                }, "Value");
                Register(
                    x =>
                    {
                        Console.WriteLine(x);
                    },
                    "Field",
                    3);
                Show(
                    Value(),
                    "Value");
                Show(
                    new
                    {
                        Name = "b",
                    },
                    "Value");
            }

            static object Value() => 1;
        }
        """,
        editorConfig: "stylebro_allow_leading_multiline_lambda = true");

    // Without the option (StyleCop's SA1116/SA1117): the leading lambda gets the first item's line and the others theirs.
    [Fact]
    public Task LeadingMultiLineLambda_IsReportedByDefault() => VerifyFixAsync(
        """
        using System;

        class C
        {
            static void Register(Action<int> action, string kind) { }

            void M()
            {
                Register({|BRO1107:x =>
                {
                    Console.WriteLine(x);
                }|}, {|BRO1108:"Field"|});
            }
        }
        """,
        """
        using System;

        class C
        {
            static void Register(Action<int> action, string kind) { }

            void M()
            {
                Register(
                    x =>
                    {
                        Console.WriteLine(x);
                    },
                    "Field");
            }
        }
        """);
}
