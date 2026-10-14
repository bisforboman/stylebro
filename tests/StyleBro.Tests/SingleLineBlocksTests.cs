using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.SingleLineBlocksAnalyzer, StyleBro.CodeFixes.Layout.SingleLineBlocksCodeFixProvider>;

namespace StyleBro.Tests;

public class SingleLineBlocksTests
{
    [Fact]
    public Task StatementBlocks_AreExpanded() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            public void M(bool b)
            {
                if (b) {|BRO1508:{|} return; }
                if (b) {|BRO1508:{|} }
                if (b) {|BRO1508:{|} Console.WriteLine(); Console.WriteLine(); }
                if (b) {|BRO1508:{|} return; } else {|BRO1508:{|} Console.WriteLine(); }
                while (b) {|BRO1508:{|} b = false; }
                do {|BRO1508:{|} b = false; } while (b);
                try {|BRO1508:{|} b = true; } catch {|BRO1508:{|} b = false; } finally {|BRO1508:{|} b = true; }
                {|BRO1508:{|} b = true; }
                if (b) {|BRO1508:{|} return; } // trailing
            }
        }
        """,
        """
        using System;

        public class C
        {
            public void M(bool b)
            {
                if (b)
                {
                    return;
                }

                if (b)
                {
                }

                if (b)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                }

                if (b)
                {
                    return;
                }
                else
                {
                    Console.WriteLine();
                }

                while (b)
                {
                    b = false;
                }

                do
                {
                    b = false;
                } while (b);
                try
                {
                    b = true;
                }
                catch
                {
                    b = false;
                }
                finally
                {
                    b = true;
                }

                {
                    b = true;
                }

                if (b)
                {
                    return;
                } // trailing
            }
        }
        """);

    [Fact]
    public Task StatementsWithoutBraces_GetThemInTheSameEdit() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool x) {|BRO1509:{|} if (x) return; }

            public int N(bool x) {|BRO1509:{|} if (x) M(x); else M(!x); return 1; }

            public void O(bool x)
            {
                if (x) {|BRO1508:{|} while (!x) x = true; }
            }

            public void P(bool x) {|BRO1509:{|} if (x) {|BRO1508:{|} if (!x) M(x); else M(!x); } }
        }
        """,
        """
        public class C
        {
            public void M(bool x)
            {
                if (x)
                {
                    return;
                }
            }

            public int N(bool x)
            {
                if (x)
                {
                    M(x);
                }
                else
                {
                    M(!x);
                }

                return 1;
            }

            public void O(bool x)
            {
                if (x)
                {
                    while (!x)
                    {
                        x = true;
                    }
                }
            }

            public void P(bool x)
            {
                if (x)
                {
                    if (!x)
                    {
                        M(x);
                    }
                    else
                    {
                        M(!x);
                    }
                }
            }
        }
        """);

    [Fact]
    public Task StatementsWithoutBraces_KeepThem_WhenTheBraceRulesAreOff() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool x) {|BRO1509:{|} if (x) return; }
        }
        """,
        """
        public class C
        {
            public void M(bool x)
            {
                if (x) return;
            }
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1514.severity = none\ndotnet_diagnostic.BRO1515.severity = none\ndotnet_diagnostic.BRO1516.severity = none");

    [Fact]
    public Task InnerBlock_ExpandsTheEnclosingBody_AndALocalFunctionGetsABlankLineAfterIt() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool x) {|BRO1509:{|} if (x) {|BRO1508:{|} M(x); } }

            public void N(bool x) {|BRO1509:{|} void L() {|BRO1509:{|} M(x); } L(); }
        }
        """,
        """
        public class C
        {
            public void M(bool x)
            {
                if (x)
                {
                    M(x);
                }
            }

            public void N(bool x)
            {
                void L()
                {
                    M(x);
                }

                L();
            }
        }
        """);

    [Fact]
    public Task LocalFunction_GetsNoBlankLineAfterIt_WhenBRO1519IsOff() => VerifyFixAsync(
        """
        public class C
        {
            public void N(bool x) {|BRO1509:{|} void L() {|BRO1509:{|} N(x); } L(); }
        }
        """,
        """
        public class C
        {
            public void N(bool x)
            {
                void L()
                {
                    N(x);
                }
                L();
            }
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1519.severity = none\n");

    [Fact]
    public Task InnerBlock_LeavesTheEnclosingBody_WhenBRO1509IsOff() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool x) { if (x) {|BRO1508:{|} M(x); } }
        }
        """,
        """
        public class C
        {
            public void M(bool x) { if (x)
                {
                    M(x);
                } }
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1509.severity = none\n");

    [Fact]
    public Task NestedBlocks_SingleFixAndFixAllAgree() => VerifyFixAsync(
        """
        public class C
        {
            public void M(bool b)
            {
                if (b) {|BRO1508:{|} if (b) {|BRO1508:{|} return; } else if (!b) {|BRO1508:{|} b = true; } }
            }
        }
        """,
        """
        public class C
        {
            public void M(bool b)
            {
                if (b)
                {
                    if (b)
                    {
                        return;
                    }
                    else if (!b)
                    {
                        b = true;
                    }
                }
            }
        }
        """);

    [Fact]
    public Task Elements_AreExpanded() => VerifyFixAsync(
        """
        using System;

        namespace N {|BRO1509:{|} public class Empty {|BRO1509:{|} } }

        public class OneLine {|BRO1509:{|} private int a; private int b; public void M() {|BRO1509:{|} } }

        public enum E {|BRO1509:{|} A, B }

        public class Members
        {
            private int x;

            public Members() {|BRO1509:{|} }

            public int Get {|BRO1509:{|} get { return this.x; } }

            public int this[int i] {|BRO1509:{|} get { return i; } }

            public event EventHandler Ev {|BRO1509:{|} add { } remove { } }

            public static Members operator +(Members a, Members b) {|BRO1509:{|} return a; }

            public void Local()
            {
                void L() {|BRO1509:{|} Console.WriteLine(); }
            }
        }
        """,
        """
        using System;

        namespace N
        {
            public class Empty
            {
            }
        }

        public class OneLine
        {
            private int a;
            private int b;

            public void M()
            {
            }
        }

        public enum E
        {
            A,
            B,
        }

        public class Members
        {
            private int x;

            public Members()
            {
            }

            public int Get
            {
                get { return this.x; }
            }

            public int this[int i]
            {
                get { return i; }
            }

            public event EventHandler Ev
            {
                add { }
                remove { }
            }

            public static Members operator +(Members a, Members b)
            {
                return a;
            }

            public void Local()
            {
                void L()
                {
                    Console.WriteLine();
                }
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        public class C
        {
            private int x;

            public int Auto { get; set; }

            public int PrivateSet { get; private set; }

            public int Expression { get => this.x; set => this.x = value; }

            public int Accessors
            {
                get { return this.x; }
                set { this.x = value; }
            }

            public void Run(Action action) => action();

            public void M(bool b)
            {
                Action a = () => { Console.WriteLine(); };
                Action d = delegate { Console.WriteLine(); };
                this.Run(
                    () => { Console.WriteLine(); });
                if (b) { /* comment */ return; }
                switch (b) { case true: { b = false; break; } default: break; }
                if (b)
                {
                    return;
                }
            }
        }
        """);

    [Fact]
    public Task EditorConfig_IndentationAndBracesOnTheSameLine() => VerifyFixAsync(
        """
        public class C
        {
        	public void M(bool b)
        	{
        		if (b) {|BRO1508:{|} return; } else {|BRO1508:{|} b = true; }
        	}
        }
        """,
        """
        public class C
        {
        	public void M(bool b)
        	{
        		if (b) {
        			return;
        		} else {
        			b = true;
        		}
        	}
        }
        """,
        editorConfig: "indent_style = tab\ncsharp_new_line_before_open_brace = types, methods\ncsharp_new_line_before_else = false\n");

    // Trial 19: with csharp_preserve_single_line_statements = false the SDK's whitespace pass splits 'if (x) y;' once it's
    // no longer in a single-line block, but it ran before the fix: the expansion does it, so one run converges. Nested
    // embedded statements go one level deeper each (trial 20), comments in a gap are left alone, and a statement that
    // becomes multi-line gets BRO1515's braces (trial 21).
    [Fact]
    public Task EmbeddedStatements_GetTheirOwnLines_WhenSingleLineStatementsArentPreserved() => VerifyFixAsync(
        """
        public class C
        {
            public C(int x, int[] a) {|BRO1509:{|} if (x > 0) x++; }
            public void M(int x, int[] a) {|BRO1509:{|} if (x > 0) x++; else if (x < 0) x--; else x = 1; while (x > 0) x--; foreach (var v in a) x += v; do x--; while (x > 0); lock (a) x++; }
            public void N(int x) {|BRO1509:{|} if (x > 0) if (x > 1) x++; if (x > 0) /* c */ x++; for (; x < 3; x++) x--; }
        }
        """,
        """
        public class C
        {
            public C(int x, int[] a)
            {
                if (x > 0)
                    x++;
            }
            public void M(int x, int[] a)
            {
                if (x > 0)
                    x++;
                else if (x < 0)
                    x--;
                else
                    x = 1;
                while (x > 0)
                    x--;
                foreach (var v in a)
                    x += v;
                do
                    x--;
                while (x > 0);
                lock (a)
                    x++;
            }
            public void N(int x)
            {
                if (x > 0)
                {
                    if (x > 1)
                        x++;
                }

                if (x > 0) /* c */ x++;
                for (; x < 3; x++)
                    x--;
            }
        }
        """,
        editorConfig: "csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = when_multiline\n");

    // A braced 'if' with an unbraced 'else' (and other embedded blocks): the formatter puts the block's '{' on its own line
    // (csharp_new_line_before_open_brace), 'else' after the '}' too, and the else's statement on its own line; the block
    // itself stays on one line (BRO1508 is off here).
    [Fact]
    public Task EmbeddedBlocks_StartTheirOwnLine_WhenSingleLineStatementsArentPreserved() => VerifyFixAsync(
        """
        public class C
        {
            public void M(int x, int[] a) {|BRO1509:{|} if (x > 0) { x++; } else x--; if (x > 0) { x++; } else if (x < 0) x--; else { x = 1; } while (x > 0) { x--; } do { x--; } while (x > 0); }
        }
        """,
        """
        public class C
        {
            public void M(int x, int[] a)
            {
                if (x > 0)
                { x++; }
                else
                    x--;
                if (x > 0)
                { x++; }
                else if (x < 0)
                    x--;
                else
                { x = 1; }

                while (x > 0)
                { x--; }

                do
                { x--; } while (x > 0);
            }
        }
        """,
        editorConfig: "csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = false\ndotnet_diagnostic.BRO1508.severity = none\n");

    // Trial 20: a one-line switch in the expanded block is laid out like the formatter does, with BRO1526's blank line; one
    // with a comment or a nested statement is left alone.
    [Fact]
    public Task Switch_GetsItsLines_WhenSingleLineStatementsArentPreserved() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int x) {|BRO1509:{|} switch (x) { case 1: case 2: x++; break; default: return 30; } return x; }
            public void E(int x) {|BRO1509:{|} switch (x) { } }
            public void N(int x) {|BRO1509:{|} switch (x) { case 1: /* c */ break; } switch (x) { case 1: if (x > 0) x++; break; } switch (x) { case 1: { } break; } }
        }
        """,
        """
        public class C
        {
            public int M(int x)
            {
                switch (x)
                {
                    case 1:
                    case 2:
                        x++;
                        break;

                    default:
                        return 30;
                }

                return x;
            }
            public void E(int x)
            {
                switch (x)
                {
                }
            }
            public void N(int x)
            {
                switch (x) { case 1: /* c */ break; }

                switch (x) { case 1: if (x > 0) x++; break; }

                switch (x) { case 1: { } break; }
            }
        }
        """,
        editorConfig: "csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = false\n");

    // With K&R braces for control blocks and 'else' kept after '}', only the else's statement moves (as the formatter does).
    [Fact]
    public Task EmbeddedBlocks_FollowTheBraceAndElseSettings() => VerifyFixAsync(
        "public class C\n{\n    public void M(int x) {|BRO1509:{|} if (x > 0) { x++; } else x--; }\n}\n",
        "public class C\n{\n    public void M(int x)\n    {\n        if (x > 0) { x++; } else\n            x--;\n    }\n}\n",
        editorConfig: "csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = false\ncsharp_new_line_before_open_brace = methods\ncsharp_new_line_before_else = false\ndotnet_diagnostic.BRO1508.severity = none\n");

    // With BRO1508 on, the block is expanded as well.
    [Fact]
    public Task EmbeddedBlocks_AreExpanded_WhenBRO1508IsOn() => VerifyFixAsync(
        "public class C\n{\n    public void M(int x) {|BRO1509:{|} if (x > 0) {|BRO1508:{|} x++; } else x--; }\n}\n",
        "public class C\n{\n    public void M(int x)\n    {\n        if (x > 0)\n        {\n            x++;\n        }\n        else\n            x--;\n    }\n}\n",
        editorConfig: "csharp_preserve_single_line_statements = false\ncsharp_prefer_braces = false\n");

    // Where BRO1514-BRO1516 add braces, the line break is theirs.
    [Fact]
    public Task EmbeddedStatements_GetBraces_WhenTheBraceRulesWantThem() => VerifyFixAsync(
        "public class C\n{\n    public void M(int x) {|BRO1509:{|} if (x > 0) x++; else x--; }\n}\n",
        "public class C\n{\n    public void M(int x)\n    {\n        if (x > 0)\n        {\n            x++;\n        }\n        else\n        {\n            x--;\n        }\n    }\n}\n",
        editorConfig: "csharp_preserve_single_line_statements = false\n");

    // The SDK's default keeps them, and so does the expansion.
    [Fact]
    public Task EmbeddedStatements_StayOnTheirLine_ByDefault() => VerifyFixAsync(
        "public class C\n{\n    public void M(int x) {|BRO1509:{|} if (x > 0) x++; }\n}\n",
        "public class C\n{\n    public void M(int x)\n    {\n        if (x > 0) x++;\n    }\n}\n",
        editorConfig: "csharp_prefer_braces = when_multiline\n");

    [Fact]
    public Task LineEndings_AreKept() => VerifyFixAsync(
        "public class C\r\n{\r\n    public void M(bool b) {|BRO1509:{|} if (b) {|BRO1508:{|} return; } }\r\n}\r\n",
        "public class C\r\n{\r\n    public void M(bool b)\r\n    {\r\n        if (b)\r\n        {\r\n            return;\r\n        }\r\n    }\r\n}\r\n");

    [Fact]
    public Task Enum_GetsNoTrailingComma_WhenBRO1401IsOff() => VerifyFixAsync(
        """
        public enum E {|BRO1509:{|} A, B }
        """,
        """
        public enum E
        {
            A,
            B
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1401.severity = none\n");

    // The expansion adds BRO1505's blank lines, so with stylebro_allow_adjacent_single_line_members none between
    // single-line members without a block body.
    [Fact]
    public Task Expansion_AdjacentSingleLineMembersAllowed() => VerifyFixAsync(
        """
        public interface I {|BRO1509:{|} void A(); int B => 1; void C() {|BRO1509:{|} } }
        """,
        """
        public interface I
        {
            void A();
            int B => 1;

            void C()
            {
            }
        }
        """,
        editorConfig: "stylebro_allow_adjacent_single_line_members = true");

    [Fact]
    public Task Enum_GetsNoTrailingComma_WhenTrailingCommasAreOmitted() => VerifyFixAsync(
        """
        public enum E {|BRO1509:{|} A, B }
        """,
        """
        public enum E
        {
            A,
            B
        }
        """,
        editorConfig: "stylebro_trailing_comma = omit\n");

    [Fact]
    public Task AllowEmpty_EmptyBracesStay_OthersAreExpanded() => VerifyFixAsync(
        """
        public class Empty { }

        public enum None { }

        public class C {|BRO1509:{|} public C() { } }

        public class D
        {
            public void M(bool b) {|BRO1509:{|} try { } catch { } if (b) {|BRO1508:{|} X(); } }

            public void X() { }
        }
        """,
        """
        public class Empty { }

        public enum None { }

        public class C
        {
            public C() { }
        }

        public class D
        {
            public void M(bool b)
            {
                try { } catch { }

                if (b)
                {
                    X();
                }
            }

            public void X() { }
        }
        """,
        editorConfig: "stylebro_allow_empty_single_line_blocks = true\n");
}
