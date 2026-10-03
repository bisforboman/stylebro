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
            }
            public void N()
            {
            }
        }
        """,
        "dotnet_diagnostic.BRO1503.severity = warning\ndotnet_diagnostic.BRO1504.severity = warning\ndotnet_diagnostic.BRO1505.severity = warning\ndotnet_diagnostic.BRO1506.severity = warning");
}
