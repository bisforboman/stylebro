using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.BlankLineRunsAnalyzer, StyleBro.CodeFixes.Layout.BlankLineRunsCodeFixProvider>;

namespace StyleBro.Tests;

public class BlankLineRunsTests
{
    [Fact]
    public Task BlankLines_AreFixed() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool a)
            {
                var x = 1;
        {|BRO1517:

        |}        if (a)
                {
                    x++;

                {|BRO1518:}|}{|BRO1519:
        |}        foreach (var i in new[] { 1 })
                {
                    x += i;
                }{|BRO1519:
        |}        return x;
            }
        }
        """,
        """
        public class C
        {
            public int M(bool a)
            {
                var x = 1;

                if (a)
                {
                    x++;
                }

                foreach (var i in new[] { 1 })
                {
                    x += i;
                }

                return x;
            }
        }
        """);

    [Fact]
    public Task SeveralRunsBeforeOneToken_AreAllFixedInOneRun() => VerifyFixAsync(
        """
        public class C
        {
            public int A;

            #region First
        {|BRO1517:


        |}    #endregion

            #region Second
        {|BRO1517:


        |}    #endregion

            public int B;
        }
        """,
        """
        public class C
        {
            public int A;

            #region First

            #endregion

            #region Second

            #endregion

            public int B;
        }
        """);

    // Fonts: below a comment and above another, BRO1506 doesn't report the blank lines, so they're BRO1517's.
    [Fact]
    public Task BlankLinesBetweenTwoComments_AreFixed() => VerifyFixAsync(
        """
        public class C
        {
            public void M()
            {
                // The number of glyphs.
        {|BRO1517:

        |}        // The format.
                M();
            }
        }
        """,
        """
        public class C
        {
            public void M()
            {
                // The number of glyphs.

                // The format.
                M();
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        public class C
        {
            public int Value { get; set; }

            public int M(bool a, int[] items)
            {
                try
                {
                    a = !a;
                }
                catch (Exception)
                {
                }
                finally
                {
                }

                if (a)
                {
                    a = false;
                }
                else
                {
                    a = true;
                }

                do
                {
                    a = !a;
                }
                while (a);

                var c = new C
                {
                    Value = 1,
                };
                Action f = () =>
                {
                };
                var s = $"{c.Value}";
                if (a)
                {
        #if DEBUG
                    a = false;
        #endif

                }

                return items.Length + s.Length;
            }
        }
        """);

    // recyclarr (CSharpier layout): a '}' that ends a pattern or expression, followed by a token that continues it, isn't
    // the end of a block. BRO1519 added a blank line before '?', CSharpier removed it again.
    [Fact]
    public Task ContinuationAfterCloseBrace_NotReported() => VerifyNoDiagnosticsAsync(
        """
        using System.Linq;

        public class C
        {
            public int Value { get; set; }

            public int[] Items
            {
                get;
                set;
            }
            = new int[0];

            public object M(object o, C c, int[] items)
            {
                var a = o
                    is C
                    {
                        Value: 1,
                    }
                    ? 1
                    : 2;
                var b = new C
                {
                    Value = 1,
                }
                ?? c;
                var d = new
                {
                    Value = 2,
                }
                with
                {
                    Value = 3,
                };
                var e = c.Value switch
                {
                    1 => 1,
                    _ => 2,
                }
                + a;
                var f = new[]
                {
                    1,
                }
                as object;
                var g = o switch
                {
                    C
                    {
                        Value: 1,
                    }
                    when b != null => 1,
                    C
                    {
                        Value: 2,
                    }
                    x => x.Value,
                    _ => 0,
                };
                return new C
                {
                    Value = 1,
                }
                ?.Value;
            }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none");

    // Lua-CSharp (trial 26): the next accessor starting with an attribute list got a blank line that the same accessor
    // without the attribute didn't, and CSharpier removed it again.
    [Fact]
    public Task NextAccessorWithAttributeOrModifier_NotReported() => VerifyNoDiagnosticsAsync(
        """
        using System.Runtime.CompilerServices;

        public class C
        {
            private int f;

            public int WithAttribute
            {
                get
                {
                    var x = f;
                    return x;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => f = value;
            }

            public int WithModifier
            {
                get
                {
                    var x = f;
                    return x;
                }
                private set => f = value;
            }

            public event System.Action E
            {
                add
                {
                    f++;
                }
                [MethodImpl(MethodImplOptions.NoInlining)]
                remove => f--;
            }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none");

    [Fact]
    public Task OtherRulesPlaces_AreLeftToThem() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {

            public void M(bool a)
            {
                if (a)
                {
                    a = false;
                }
                // BRO1504 asks for the blank line above this comment.
                a = true;

                // BRO1506: no blank line below a comment.


                a = false;

                // BRO1506 also covers the blank line before '}' below a comment.

            }
            public void N()
            {
            }
        }
        """,
        "dotnet_diagnostic.BRO1503.severity = warning\ndotnet_diagnostic.BRO1504.severity = warning\ndotnet_diagnostic.BRO1505.severity = warning\ndotnet_diagnostic.BRO1506.severity = warning\nstylebro_blank_line_below_standalone_comments = remove");

    // Trial 21: BRO1506 leaves a standalone comment (blank lines above and below) by default, so its extra blank lines are
    // BRO1517's.
    [Fact]
    public Task BlankLinesBelowAStandaloneComment_AreBro1517s() => VerifyFixAsync(
        """
        public class C
        {
            public void M()
            {
                M();

                // A note.
        {|BRO1517:

        |}        M();
            }
        }
        """,
        """
        public class C
        {
            public void M()
            {
                M();

                // A note.

                M();
            }
        }
        """,
        "dotnet_diagnostic.BRO1506.severity = warning");

    // Trial 64, ArchUnitNET: CSharpier removes every blank line above a 'checked'/'unchecked' statement, so with CSharpier
    // laying the code out BRO1519 leaves that gap alone (the two formatters undid each other); without it, it's reported.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task CheckedStatementBelowABlock_IsLeftToCSharpier(bool csharpier) => VerifyFixAsync(
        $$"""
        public class C
        {
            public int M(bool b, object o)
            {
                if (b)
                {
                    return 0;
                }{{(csharpier ? string.Empty : "{|BRO1519:")}}
        {{(csharpier ? string.Empty : "|}")}}        unchecked
                {
                    return o.GetHashCode() * 397;
                }
            }

            public int N(int a)
            {
                for (var i = 0; i < a; i++)
                {
                    a--;
                }{{(csharpier ? string.Empty : "{|BRO1519:")}}
        {{(csharpier ? string.Empty : "|}")}}        checked
                {
                    return a * 3;
                }
            }
        }
        """,
        $$"""
        public class C
        {
            public int M(bool b, object o)
            {
                if (b)
                {
                    return 0;
                }
        {{(csharpier ? string.Empty : "\n")}}        unchecked
                {
                    return o.GetHashCode() * 397;
                }
            }

            public int N(int a)
            {
                for (var i = 0; i < a; i++)
                {
                    a--;
                }
        {{(csharpier ? string.Empty : "\n")}}        checked
                {
                    return a * 3;
                }
            }
        }
        """,
        csharpier ? "stylebro_layout_by = csharpier\n" : null);
}
