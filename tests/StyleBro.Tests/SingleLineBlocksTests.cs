using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.SingleLineBlocksAnalyzer, StyleBro.CodeFixes.Layout.SingleLineBlocksCodeFixProvider>;

namespace StyleBro.Tests;

public class SingleLineBlocksTests
{
    [Fact]
    public Task StatementBlocks_AreExpanded() => VerifyFixAsync("""
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
        """, """
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
    public Task NestedBlocks_SingleFixAndFixAllAgree() => VerifyFixAsync("""
        public class C
        {
            public void M(bool b)
            {
                if (b) {|BRO1508:{|} if (b) {|BRO1508:{|} return; } else if (!b) {|BRO1508:{|} b = true; } }
            }
        }
        """, """
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
    public Task Elements_AreExpanded() => VerifyFixAsync("""
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
        """, """
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
            A, B,
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
    public Task EditorConfig_IndentationAndBracesOnTheSameLine() => VerifyFixAsync("""
        public class C
        {
        	public void M(bool b)
        	{
        		if (b) {|BRO1508:{|} return; } else {|BRO1508:{|} b = true; }
        	}
        }
        """, """
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
        """, editorConfig: "indent_style = tab\ncsharp_new_line_before_open_brace = types, methods\ncsharp_new_line_before_else = false\n");

    [Fact]
    public Task LineEndings_AreKept() => VerifyFixAsync(
        "public class C\r\n{\r\n    public void M(bool b) {|BRO1509:{|} if (b) {|BRO1508:{|} return; } }\r\n}\r\n",
        "public class C\r\n{\r\n    public void M(bool b)\r\n    {\r\n        if (b)\r\n        {\r\n            return;\r\n        }\r\n    }\r\n}\r\n");

    [Fact]
    public Task Enum_GetsNoTrailingComma_WhenBRO1401IsOff() => VerifyFixAsync("""
        public enum E {|BRO1509:{|} A, B }
        """, """
        public enum E
        {
            A, B
        }
        """, editorConfig: "dotnet_diagnostic.BRO1401.severity = none\n");
}
