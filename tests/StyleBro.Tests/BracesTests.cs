using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.BracesAnalyzer, StyleBro.CodeFixes.Layout.BracesCodeFixProvider>;

namespace StyleBro.Tests;

public class BracesTests
{
    [Fact]
    public Task ChildStatements_GetBraces() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            private readonly object gate = new object();

            public int M(bool a, List<int> items)
            {
                if (a)
                    {|BRO1514:return 1;|}

                foreach (var item in items)
                    {|BRO1514:a = !a;|} // keeps the comment

                while (a)
                    {|BRO1514:a = false;|}

                for (var i = 0; i < 3; i++)
                    {|BRO1514:a = true;|}

                lock (gate)
                    {|BRO1514:a = false;|}

                do
                    {|BRO1514:a = !a;|}
                while (a);

                return 0;
            }
        }
        """,
        """
        using System.Collections.Generic;

        public class C
        {
            private readonly object gate = new object();

            public int M(bool a, List<int> items)
            {
                if (a)
                {
                    return 1;
                }

                foreach (var item in items)
                {
                    a = !a; // keeps the comment
                }

                while (a)
                {
                    a = false;
                }

                for (var i = 0; i < 3; i++)
                {
                    a = true;
                }

                lock (gate)
                {
                    a = false;
                }

                do
                {
                    a = !a;
                }
                while (a);

                return 0;
            }
        }
        """);

    [Fact]
    public Task SameLine_And_ElseChains() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a)
            {
                if (a == 1) {|BRO1514:return 1;|}
                else if (a == 2) {|BRO1514:return 2;|} else {|BRO1514:return 3;|}
            }

            public void D(bool a)
            {
                do {|BRO1514:a = !a;|} while (a);
            }
        }
        """,
        """
        public class C
        {
            public int M(int a)
            {
                if (a == 1)
                {
                    return 1;
                }
                else if (a == 2)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }

            public void D(bool a)
            {
                do
                {
                    a = !a;
                } while (a);
            }
        }
        """);

    [Fact]
    public Task MultiLine_Consistent_AndNested_InOnePass() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a, int b)
            {
                if (a > 0)
                    {|BRO1515:if (b > 0)
                        {|BRO1515:return Add(a,
                            b);|}|}

                if (a == 1)
                {
                    return 1;
                }
                else
                    {|BRO1516:return 2;|}
            }

            private static int Add(int x, int y) => x + y;
        }
        """,
        """
        public class C
        {
            public int M(int a, int b)
            {
                if (a > 0)
                {
                    if (b > 0)
                    {
                        return Add(a,
                            b);
                    }
                }

                if (a == 1)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }

            private static int Add(int x, int y) => x + y;
        }
        """);

    [Fact]
    public Task BraceOnTheSameLine_WhenControlBlocksAreLeftOut() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool a)
            {
                if (a)
                    {|BRO1514:return 1;|}
                else
                    {|BRO1514:return 2;|}
            }
        }
        """,
        """
        public class C
        {
            public int M(bool a)
            {
                if (a) {
                    return 1;
                } else {
                    return 2;
                }
            }
        }
        """,
        editorConfig: "csharp_new_line_before_open_brace = types, methods\ncsharp_new_line_before_else = false");

    [Fact]
    public Task NestedElseChain_IsReindentedOnce() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a)
            {
                foreach (var x in new[] { 1 })
                    {|BRO1515:if (a == x)
                        {|BRO1514:a++;|}
                    else
                        {|BRO1514:a--;|}|}

                return a;
            }
        }
        """,
        """
        public class C
        {
            public int M(int a)
            {
                foreach (var x in new[] { 1 })
                {
                    if (a == x)
                    {
                        a++;
                    }
                    else
                    {
                        a--;
                    }
                }

                return a;
            }
        }
        """);

    [Fact]
    public Task ARuleThatIsOff_LeavesItsStatementsToTheNext() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a)
            {
                if (a == 1)
                    {|BRO1514:return Add(a,
                        1);|}

                if (a == 2)
                {
                    return 2;
                }
                else
                    {|BRO1514:return 3;|}
            }

            private static int Add(int x, int y) => x + y;
        }
        """,
        """
        public class C
        {
            public int M(int a)
            {
                if (a == 1)
                {
                    return Add(a,
                        1);
                }

                if (a == 2)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }

            private static int Add(int x, int y) => x + y;
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1515.severity = none\ndotnet_diagnostic.BRO1516.severity = none");

    [Fact]
    public Task PreferBracesWhenMultiline_ReportsOnlyMultiLineAndInconsistent() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a, int b)
            {
                if (a > 0)
                    a++;

                for (var i = 0;
                    i < b;
                    i++)
                    {|BRO1515:a++;|}

                if (a == 1)
                {
                    return 1;
                }
                else
                    {|BRO1516:return 2;|}
            }
        }
        """,
        """
        public class C
        {
            public int M(int a, int b)
            {
                if (a > 0)
                    a++;

                for (var i = 0;
                    i < b;
                    i++)
                {
                    a++;
                }

                if (a == 1)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
        }
        """,
        editorConfig: "csharp_prefer_braces = when_multiline:warning");

    [Fact]
    public Task PreferBracesFalse_ReportsNothing() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(int a)
            {
                if (a > 0)
                    return Add(a,
                        1);

                if (a == 1)
                {
                    return 1;
                }
                else
                    return 2;
            }

            private static int Add(int x, int y) => x + y;
        }
        """,
        "csharp_prefer_braces = false");

    [Fact]
    public Task ConsecutiveUsings_GetBraces_WhenNotAllowed() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            public void M(IDisposable x, IDisposable y)
            {
                using (x)
                    {|BRO1515:using (y)
                    {
                    }|}
            }
        }
        """,
        """
        using System;

        public class C
        {
            public void M(IDisposable x, IDisposable y)
            {
                using (x)
                {
                    using (y)
                    {
                    }
                }
            }
        }
        """,
        editorConfig: "stylebro_allow_consecutive_usings = false");

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        public class C
        {
            public void M(bool a, IDisposable x, IDisposable y)
            {
                if (a)
                {
                    a = false;
                }

                using (x)
                using (y)
                {
                }

                // A comment where the brace would go.
                if (a) // why
                    a = false;

                // A multi-line string inside: its lines would be reindented.
                if (a)
                    Console.WriteLine(@"one
                    two");

                // A directive elsewhere in the if statement.
                if (a)
                    a = false;
        #if true
                else
                    a = true;
        #endif

                // Other code after it on the line.
                if (a) a = false; a = true;

        #if DEBUG
                if (a)
                    a = false;
        #endif
            }
        }
        """);

    [Fact]
    public Task ElseChain_GetsAllItsBracesAtOnce_ExceptWhereTheyCantGo() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a)
            {
                if (a == 1) // why
                    return 1;
                else if (a == 2)
                    {|BRO1514:return 2;|}
                else
                    {|BRO1515:return Add(a,
                        3);|}
            }

            private static int Add(int x, int y) => x + y;
        }
        """,
        """
        public class C
        {
            public int M(int a)
            {
                if (a == 1) // why
                    return 1;
                else if (a == 2)
                {
                    return 2;
                }
                else
                {
                    return Add(a,
                        3);
                }
            }

            private static int Add(int x, int y) => x + y;
        }
        """);
}
