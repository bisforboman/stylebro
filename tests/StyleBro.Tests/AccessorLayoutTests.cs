using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.AccessorLayoutAnalyzer, StyleBro.CodeFixes.Layout.AccessorLayoutCodeFixProvider>;

namespace StyleBro.Tests;

public class AccessorLayoutTests
{
    [Fact]
    public Task OneStatementBodies_AreCollapsed() => VerifyFixAsync("""
        public class C
        {
            private int x;

            public int Mixed
            {
                {|BRO1510:get|} { return this.x; }

                set
                {
                    this.x = value;
                }
            }

            public int Compact
            {
                {|BRO1510:get|} { return this.x;
                }
                set { this.x = value; }
            }

            public event System.EventHandler Changed
            {
                {|BRO1510:add|} { }

                remove
                {
                }
            }
        }
        """, """
        public class C
        {
            private int x;

            public int Mixed
            {
                get { return this.x; }

                set { this.x = value; }
            }

            public int Compact
            {
                get { return this.x; }

                set { this.x = value; }
            }

            public event System.EventHandler Changed
            {
                add { }

                remove { }
            }
        }
        """);

    [Fact]
    public Task LongerBodies_ExpandTheSingleLineOnes() => VerifyFixAsync("""
        public class C
        {
            private int x;
            private int y;

            public int TwoStatements
            {
                {|BRO1510:get|} { return this.x; }
                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public int WithComment
            {
                {|BRO1510:get|} { return this.x; }

                set
                {
                    // keep it
                    this.x = value;
                }
            }
        }
        """, """
        public class C
        {
            private int x;
            private int y;

            public int TwoStatements
            {
                get
                {
                    return this.x;
                }

                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public int WithComment
            {
                get
                {
                    return this.x;
                }

                set
                {
                    // keep it
                    this.x = value;
                }
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        public class C
        {
            private int x;

            public int AllSingle
            {
                get { return this.x; }
                set { this.x = value; }
            }

            public int AllMulti
            {
                get
                {
                    return this.x;
                }

                set
                {
                    this.x = value;
                }
            }

            public int ExpressionGet
            {
                get => this.x;
                set
                {
                    this.x = value;
                }
            }

            public int OneLine { get { return this.x; } set { this.x = value; } }

            public int CommentBetween
            {
                get { return this.x; } /* a */ set
                {
                    this.x = value;
                }
            }
        }
        """);
}
