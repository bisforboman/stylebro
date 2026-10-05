using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ElseAfterJumpAnalyzer, StyleBro.CodeFixes.Readability.ElseAfterJumpCodeFixProvider>;

namespace StyleBro.Tests;

public class ElseAfterJumpTests
{
    private const string On = "dotnet_diagnostic.BRO1143.severity = warning\n";

    [Fact]
    public Task OffByDefault() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
        }
        """);

    [Fact]
    public Task Block_MovesOut_WithBro1519sBlankLine() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            public int M(bool x, int y)
            {
                if (x)
                {
                    Console.WriteLine();
                    return 1;
                }
                {|BRO1143:else|}
                {
                    y++;
                    Console.WriteLine(
                        y);

                    return y;
                }
            }
        }
        """,
        """
        using System;

        public class C
        {
            public int M(bool x, int y)
            {
                if (x)
                {
                    Console.WriteLine();
                    return 1;
                }

                y++;
                Console.WriteLine(
                    y);

                return y;
            }
        }
        """,
        On);

    [Fact]
    public Task WithoutBro1519_NoBlankLine() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                {
                    return 1;
                }
                {|BRO1143:else|}
                {
                    return 2;
                }
            }
        }
        """,
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                {
                    return 1;
                }
                return 2;
            }
        }
        """,
        On + "dotnet_diagnostic.BRO1519.severity = none\n");

    [Fact]
    public Task KAndR_AndEveryJump() => VerifyFixAsync(
        """
        using System;
        using System.Collections.Generic;

        public class C
        {
            public IEnumerable<int> M(int[] items)
            {
                foreach (var i in items) {
                    if (i == 0) {
                        continue;
                    } {|BRO1143:else|} {
                        Console.WriteLine(i);
                    }

                    if (i == 1) {
                        break;
                    } {|BRO1143:else|} {
                        Console.WriteLine(i);
                    }

                    if (i == 2) {
                        goto end;
                    } {|BRO1143:else|} {
                        Console.WriteLine(i);
                    }

                    if (i == 3) {
                        throw new InvalidOperationException();
                    } {|BRO1143:else|} {
                        yield return i;
                    }
                }

                end:
                Console.WriteLine();
                if (items.Length == 0) {
                    yield break;
                } {|BRO1143:else|} {
                    yield return 1;
                }
            }
        }
        """,
        """
        using System;
        using System.Collections.Generic;

        public class C
        {
            public IEnumerable<int> M(int[] items)
            {
                foreach (var i in items) {
                    if (i == 0) {
                        continue;
                    }

                    Console.WriteLine(i);

                    if (i == 1) {
                        break;
                    }

                    Console.WriteLine(i);

                    if (i == 2) {
                        goto end;
                    }

                    Console.WriteLine(i);

                    if (i == 3) {
                        throw new InvalidOperationException();
                    }

                    yield return i;
                }

                end:
                Console.WriteLine();
                if (items.Length == 0) {
                    yield break;
                }

                yield return 1;
            }
        }
        """,
        On);

    [Fact]
    public Task WithoutBraces_AndOnOneLine() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool x, bool y)
            {
                if (x)
                    return 1;
                {|BRO1143:else|}
                    System.Console.WriteLine();

                if (y) { return 2; } {|BRO1143:else|} { System.Console.WriteLine(); }

                if (!x) return 3; {|BRO1143:else|} return 4;
            }
        }
        """,
        """
        public class C
        {
            public int M(bool x, bool y)
            {
                if (x)
                    return 1;
                System.Console.WriteLine();

                if (y) { return 2; }
                System.Console.WriteLine();

                if (!x) return 3;
                return 4;
            }
        }
        """,
        On);

    [Fact]
    public Task InconsistentBraces_LeftToBro1516_WhenItIsOn() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                    return 1;
                else
                {
                    return 2;
                }
            }
        }
        """,
        On);

    [Fact]
    public Task InconsistentBraces_WhenBro1516IsOff() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                    return 1;
                {|BRO1143:else|}
                {
                    return 2;
                }
            }
        }
        """,
        """
        public class C
        {
            public int M(bool x)
            {
                if (x)
                    return 1;
                return 2;
            }
        }
        """,
        On + "dotnet_diagnostic.BRO1516.severity = none\n");

    [Fact]
    public Task Nested_AllMoveOutInOnePass() => VerifyFixAsync(
        """
        public class C
        {
            public int M(bool x, bool y, bool z)
            {
                if (x)
                {
                    return 1;
                }
                {|BRO1143:else|}
                {
                    if (y)
                    {
                        return 2;
                    }
                    {|BRO1143:else|}
                    {
                        System.Console.WriteLine();
                        if (z)
                        {
                            return 3;
                        }
                        {|BRO1143:else|}
                        {
                            return 4;
                        }
                    }
                }
            }
        }
        """,
        """
        public class C
        {
            public int M(bool x, bool y, bool z)
            {
                if (x)
                {
                    return 1;
                }

                if (y)
                {
                    return 2;
                }

                System.Console.WriteLine();
                if (z)
                {
                    return 3;
                }

                return 4;
            }
        }
        """,
        On);

    [Fact]
    public Task Tabs() => VerifyFixAsync(
        "public class C\n{\n\tpublic int M(bool x)\n\t{\n\t\tif (x)\n\t\t{\n\t\t\treturn 1;\n\t\t}\n\t\t{|BRO1143:else|}\n\t\t{\n\t\t\tx = !x;\n\t\t\treturn 2;\n\t\t}\n\t}\n}\n",
        "public class C\n{\n\tpublic int M(bool x)\n\t{\n\t\tif (x)\n\t\t{\n\t\t\treturn 1;\n\t\t}\n\n\t\tx = !x;\n\t\treturn 2;\n\t}\n}\n",
        On + "indent_style = tab\n");

    [Fact]
    public Task Skipped() => VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.IO;

        public class C
        {
            public int M(bool x, int y)
            {
                // The 'if' branch doesn't end in a jump.
                if (x)
                {
                    return 1;
                    Console.WriteLine();
                }
                else
                {
                    return 2;
                }

                if (x)
                {
                    Console.WriteLine();
                }
                else
                {
                    return 2;
                }

                // 'else if' chains.
                if (x)
                {
                    return 1;
                }
                else if (y > 0) return 2;

                // Not a statement of a block.
                if (x)
                    if (y > 0)
                        return 1;
                    else
                        return 2;

                // Empty 'else'.
                if (x)
                {
                    return 1;
                }
                else
                {
                }

                // Comments.
                if (x)
                {
                    return 1;
                }
                else // why
                {
                    return 2;
                }

                if (x)
                {
                    return 1;
                }
                // why
                else
                {
                    return 2;
                }

                if (x)
                {
                    return 1;
                }
                else
                {
                    return 2;
                    // done
                }

                // Directives.
                if (x)
                {
                    return 1;
                }
                else
                {
                    y++;
                    #if DEBUG
                    y++;
                    #endif
                    return y;
                }

                // The body starts on the 'else' line and goes on below.
                if (x)
                {
                    return 1;
                }
                else { Console.WriteLine();
                    return 2; }

                // Indented less than one level deeper than the 'if'.
                if (x)
                {
                    return 1;
                }
                else
                {
                  return 2;
                }

                // Strings and comments spanning lines.
                if (x)
                {
                    return 1;
                }
                else
                {
                    Console.WriteLine(@"a
                        b");
                    return 2;
                }

                if (x)
                {
                    return 1;
                }
                else
                {
                    y++;
                    /* a
                       b */
                    return 2;
                }

                // 'using' declarations would be disposed later.
                if (x)
                {
                    return 1;
                }
                else
                {
                    using var stream = new MemoryStream();
                    return 2;
                }

                // Names the enclosing block uses too.
                if (x)
                {
                    return 1;
                }
                else
                {
                    var z = 2;
                    return z;
                }

                {
                    var z = 3;
                    return z;
                }
            }

            public int N(bool x)
            {
                if (x)
                {
                    return 1;
                }
                else
                {
                    Func<int, int> f = q => q;
                    return f(2);
                }

                Func<int, int> g = q => q;
                return g(1);
            }

            // Syntax errors.
            public int P(bool x, int y)
            {
                if (x)
                {
                    return 1;
                }
                else
                {
                    y = {|CS1525:;|}
                    return 2;
                }
            }

            // 'yield return' isn't a jump.
            public System.Collections.Generic.IEnumerable<int> Q(bool x)
            {
                if (x)
                {
                    yield return 1;
                }
                else
                {
                    yield return 2;
                }
            }

            // The 'if' doesn't start its line.
            public int O(bool x)
            {
                var y = 1; if (x) { return 1; } else { return y; }
            }
        }
        """,
        On);
}
