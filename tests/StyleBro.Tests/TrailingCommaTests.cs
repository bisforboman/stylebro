using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.TrailingCommaAnalyzer, StyleBro.CodeFixes.Maintainability.TrailingCommaCodeFixProvider>;

namespace StyleBro.Tests;

public class TrailingCommaTests
{
    private const string Omit = "stylebro_trailing_comma = omit\n";

    [Fact]
    public Task MultiLineLists_GetATrailingComma() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        enum E
        {
            A,
            {|BRO1401:B|}
        }

        record R(int A, int B);

        class P { public int X; public int Y; }

        class C
        {
            object M(int x, R r)
            {
                var array = new[]
                {
                    1,
                    {|BRO1401:2|}
                };
                var point = new P
                {
                    X = 1,
                    {|BRO1401:Y = 2|}
                };
                var list = new List<int>
                {
                    {|BRO1401:1|}
                };
                var anonymous = new
                {
                    A = 1,
                    {|BRO1401:B = 2|}
                };
                var with = r with
                {
                    {|BRO1401:A = 1|}
                };
                var arms = x switch
                {
                    1 => "a",
                    {|BRO1401:_ => "b"|}
                };
                return (array, point, list, anonymous, with, arms);
            }
        }

        namespace System.Runtime.CompilerServices { class IsExternalInit { } }
        """,
        """
        using System.Collections.Generic;

        enum E
        {
            A,
            B,
        }

        record R(int A, int B);

        class P { public int X; public int Y; }

        class C
        {
            object M(int x, R r)
            {
                var array = new[]
                {
                    1,
                    2,
                };
                var point = new P
                {
                    X = 1,
                    Y = 2,
                };
                var list = new List<int>
                {
                    1,
                };
                var anonymous = new
                {
                    A = 1,
                    B = 2,
                };
                var with = r with
                {
                    A = 1,
                };
                var arms = x switch
                {
                    1 => "a",
                    _ => "b",
                };
                return (array, point, list, anonymous, with, arms);
            }
        }

        namespace System.Runtime.CompilerServices { class IsExternalInit { } }
        """);

    [Fact]
    public Task CommaGoesBeforeATrailingComment_AndBeforeABraceOnTheSameLine() => VerifyFixAsync(
        """
        class C
        {
            int[] a = new[]
            {
                1,
                {|BRO1401:2|} // last
            };

            int[] b = new[] { 1,
                {|BRO1401:2|} };
        }
        """,
        """
        class C
        {
            int[] a = new[]
            {
                1,
                2, // last
            };

            int[] b = new[] { 1,
                2, };
        }
        """);

    [Fact]
    public Task CodeDirectlyAfterTheLastItem_GetsCommaAndSpace() => VerifyFixAsync(
        """
        class P { public string A; public string B; }

        class C
        {
            P p = new P
            {
                A = "a",
                {|BRO1401:B = "b"|}};
        }
        """,
        """
        class P { public string A; public string B; }

        class C
        {
            P p = new P
            {
                A = "a",
                B = "b", };
        }
        """);

    [Fact]
    public Task ListsWithDirectives_AreLeftAlone() => VerifyNoDiagnosticsAsync("""
        using System.Collections.Generic;

        class P { public bool Flag; }

        class C
        {
            Dictionary<int, string> map = new Dictionary<int, string>
            {
                { 1, "a" },
        #if SOMETHING
                { 2, "b" }
        #endif
            };

            P p = new P
            {
        #if !NEVER
                Flag = true
        #endif
            };
        }
        """);

    [Fact]
    public Task NestedLists_AreFixedInOnePass() => VerifyFixAsync(
        """
        class C
        {
            int[][] a = new[]
            {
                {|BRO1401:new[]
                {
                    {|BRO1401:1|}
                }|}
            };
        }
        """,
        """
        class C
        {
            int[][] a = new[]
            {
                new[]
                {
                    1,
                },
            };
        }
        """);

    [Fact]
    public Task OtherLists_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        using System.Collections.Generic;

        class C
        {
            object M(int[] items)
            {
                var oneLine = new[] { 1, 2 };
                var done = new[]
                {
                    1,
                };
                var empty = new int[]
                {
                };
                var pairs = new Dictionary<int, int>
                {
                    {
                        1,
                        2
                    },
                };
                bool list = items is
                [
                    1,
                    2
                ];
                return (oneLine, done, empty, pairs, list);
            }
        }
        """);

    // Like StyleCop master (94671b70): multi-line property patterns, also nested ones in one pass.
    [Fact]
    public Task MultiLinePropertyPatterns_GetATrailingComma() => VerifyFixAsync(
        """
        class P
        {
            public int X { get; set; }

            public P Next { get; set; }
        }

        class C
        {
            bool M(object o) => o is P { X: 1 } && o is P
            {
                X: 1,
                {|BRO1401:Next: P
                {
                    {|BRO1401:X: 2|}
                }|}
            };
        }
        """,
        """
        class P
        {
            public int X { get; set; }

            public P Next { get; set; }
        }

        class C
        {
            bool M(object o) => o is P { X: 1 } && o is P
            {
                X: 1,
                Next: P
                {
                    X: 2,
                },
            };
        }
        """);

    [Fact]
    public Task Omit_RemovesTrailingCommas_AlsoOnOneLine() => VerifyFixAsync(
        """
        enum E
        {
            A,
            B{|BRO1401:,|}
        }

        class C
        {
            object M(int x)
            {
                var array = new[] { 1, 2{|BRO1401:,|} };
                var nested = new[]
                {
                    new[] { 1{|BRO1401:,|} }{|BRO1401:,|}
                };
                var arms = x switch
                {
                    1 => "a",
                    _ => "b"{|BRO1401:,|}
                };
                var kept = new[]
                {
                    1,
                    2
                };
                return (array, nested, arms, kept);
            }
        }
        """,
        """
        enum E
        {
            A,
            B
        }

        class C
        {
            object M(int x)
            {
                var array = new[] { 1, 2 };
                var nested = new[]
                {
                    new[] { 1 }
                };
                var arms = x switch
                {
                    1 => "a",
                    _ => "b"
                };
                var kept = new[]
                {
                    1,
                    2
                };
                return (array, nested, arms, kept);
            }
        }
        """,
        editorConfig: Omit);

    [Fact]
    public Task Omit_KeepsTrailingComments_AndDropsSpaceBeforeTheComma() => VerifyFixAsync(
        """
        enum E
        {
            A{|BRO1401:,|} // a
        }

        enum F
        {
            A{|BRO1401:,|}// a
        }

        enum G
        {
            A /* a */ {|BRO1401:,|}
        }

        enum H
        {
            A {|BRO1401:,|}
        }
        """,
        """
        enum E
        {
            A // a
        }

        enum F
        {
            A // a
        }

        enum G
        {
            A /* a */
        }

        enum H
        {
            A
        }
        """,
        editorConfig: Omit);

    [Fact]
    public Task Omit_ListsWithDirectives_AreLeftAlone() => VerifyNoDiagnosticsAsync(
        """
        enum E
        {
            A,
        #if SOMETHING
            B,
        #endif
        }
        """,
        editorConfig: Omit);
}
