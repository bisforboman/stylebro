using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.AccessorLayoutAnalyzer, StyleBro.CodeFixes.Layout.AccessorLayoutCodeFixProvider>;

namespace StyleBro.Tests;

public class AccessorLayoutTests
{
    [Fact]
    public Task OneStatementBodies_AreCollapsed() => VerifyFixAsync(
        """
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
        """,
        """
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
    public Task LongerBodies_ExpandTheSingleLineOnes() => VerifyFixAsync(
        """
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
        """,
        """
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
    public Task AttributeLines_DoNotMakeAnAccessorMultiLine() => VerifyFixAsync(
        """
        using System.Diagnostics;

        public class C
        {
            private int x;

            public int OneLineEach
            {
                [DebuggerStepThrough]
                get { return this.x; }
                set { this.x = value; }
            }

            public int Mixed
            {
                [DebuggerStepThrough]
                {|BRO1510:get|} { return this.x; }

                set
                {
                    this.x = value;
                    this.x++;
                }
            }
        }
        """,
        """
        using System.Diagnostics;

        public class C
        {
            private int x;

            public int OneLineEach
            {
                [DebuggerStepThrough]
                get { return this.x; }
                set { this.x = value; }
            }

            public int Mixed
            {
                [DebuggerStepThrough]
                get
                {
                    return this.x;
                }

                set
                {
                    this.x = value;
                    this.x++;
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

            public int ExpressionBodies
            {
                get => this.x;
                set => this.x = value;
            }

            public int ExpressionAndOneLineBlock
            {
                get => this.x;
                set { this.x = value; }
            }

            public int MultiLineExpressionAndMultiLineBlock
            {
                get =>
                    this.x;
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

        // An accessor without a body ('field' needs C# 13: an error here) leaves the list alone.
        public class AutoGet
        {
            public int Value
            {
                {|CS0501:get|};
                set
                {
                    System.Console.WriteLine(value);
                }
            }
        }
        """);

    // Issue 338: an expression-bodied accessor counts too. The short way first: a one-statement block next to it is put
    // on one line, and the expression body stays.
    [Fact]
    public Task ExpressionAccessor_NextToAOneStatementBlock_BlockIsCollapsed() => VerifyFixAsync(
        """
        public class C
        {
            private int x;

            public int A
            {
                {|BRO1510:get|} => this.x;
                set
                {
                    this.x = value;
                }
            }

            public int B
            {
                {|BRO1510:get|}
                {
                    return this.x;
                }

                set => this.x = value;
            }
        }
        """,
        """
        public class C
        {
            private int x;

            public int A
            {
                get => this.x;
                set { this.x = value; }
            }

            public int B
            {
                get { return this.x; }

                set => this.x = value;
            }
        }
        """);

    // The block can't go on one line: the expression-bodied accessor becomes a block, with BRO1505's blank line.
    [Fact]
    public Task ExpressionAccessor_NextToALongerBlock_BecomesABlock() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            private int x;
            private int y;

            public int Get
            {
                {|BRO1510:get|} => this.x;
                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public int Set
            {
                {|BRO1510:get|}
                {
                    this.y++;
                    return this.x;
                }

                private set => this.x = value;
            }

            public int Init
            {
                {|BRO1510:get|}
                {
                    this.y++;
                    return this.x;
                }
                init => this.x = value;
            }

            public int Throws
            {
                {|BRO1510:get|} => throw new NotSupportedException();
                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public int NotRef
            {
                {|BRO1510:get|} => this.y; // by reference
                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public event EventHandler Changed
            {
                {|BRO1510:add|} => this.x++;
                remove
                {
                    this.x--;
                    this.y--;
                }
            }

            public int this[int i]
            {
                {|BRO1510:get|} => this.x + i;
                set
                {
                    this.x = value;
                    this.y = i;
                }
            }
        }
        """,
        """
        using System;

        public class C
        {
            private int x;
            private int y;

            public int Get
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

            public int Set
            {
                get
                {
                    this.y++;
                    return this.x;
                }

                private set
                {
                    this.x = value;
                }
            }

            public int Init
            {
                get
                {
                    this.y++;
                    return this.x;
                }

                init
                {
                    this.x = value;
                }
            }

            public int Throws
            {
                get
                {
                    throw new NotSupportedException();
                }

                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public int NotRef
            {
                get
                {
                    return this.y;
                } // by reference

                set
                {
                    this.x = value;
                    this.y = value;
                }
            }

            public event EventHandler Changed
            {
                add
                {
                    this.x++;
                }

                remove
                {
                    this.x--;
                    this.y--;
                }
            }

            public int this[int i]
            {
                get
                {
                    return this.x + i;
                }

                set
                {
                    this.x = value;
                    this.y = i;
                }
            }
        }
        """);

    // An expression body over several lines is a multi-line accessor and stays as written: the one-line accessor next to
    // it is expanded, a block or an expression body.
    [Fact]
    public Task MultiLineExpressionAccessor_ExpandsTheSingleLineOnes() => VerifyFixAsync(
        """
        public class C
        {
            private int x;

            public int Block
            {
                {|BRO1510:get|} =>
                    this.x;
                set { this.x = value; }
            }

            public int Expression
            {
                {|BRO1510:get|} =>
                    this.x;
                set => this.x = value;
            }
        }
        """,
        """
        public class C
        {
            private int x;

            public int Block
            {
                get =>
                    this.x;
                set
                {
                    this.x = value;
                }
            }

            public int Expression
            {
                get =>
                    this.x;
                set
                {
                    this.x = value;
                }
            }
        }
        """);

    [Fact]
    public Task ExpressionAccessor_BraceOnTheAccessorsLine_WhenConfigured() => VerifyFixAsync(
        """
        public class C
        {
            private int x;
            private int y;

            public int Get
            {
                {|BRO1510:get|} => this.x;
                set {
                    this.x = value;
                    this.y = value;
                }
            }
        }
        """,
        """
        public class C
        {
            private int x;
            private int y;

            public int Get
            {
                get {
                    return this.x;
                }

                set {
                    this.x = value;
                    this.y = value;
                }
            }
        }
        """,
        editorConfig: "csharp_new_line_before_open_brace = types, methods, properties\n");

    private const string LongSetter = """
        public class C
        {
            private int x;
            private int y;

            public int Get
            {
                MARK => this.x;
                set
                {
                    this.x = value;
                    this.y = value;
                }
            }
        }
        """;

    // The SDK's IDE0027 would turn the new block back into an expression body: with its option asking for expression
    // bodies, or the rule on and the option unset (its default is 'true'), the list is left alone.
    [Theory]
    [InlineData("csharp_style_expression_bodied_accessors = true\n")]
    [InlineData("csharp_style_expression_bodied_accessors = when_on_single_line:suggestion\n")]
    [InlineData("dotnet_diagnostic.IDE0027.severity = warning\n")]
    public Task ExpressionAccessor_NotReported_WhenExpressionBodiesArePreferred(string editorConfig) =>
        VerifyNoDiagnosticsAsync(LongSetter.Replace("MARK", "get"), editorConfig);

    [Theory]
    [InlineData("csharp_style_expression_bodied_accessors = false\n")]
    [InlineData("csharp_style_expression_bodied_accessors = false\ndotnet_diagnostic.IDE0027.severity = warning\n")]
    [InlineData("dotnet_diagnostic.IDE0027.severity = none\n")]
    public Task ExpressionAccessor_Reported_WhenBlockBodiesAreFine(string editorConfig) => VerifyFixAsync(
        LongSetter.Replace("MARK", "{|BRO1510:get|}"),
        """
        public class C
        {
            private int x;
            private int y;

            public int Get
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
        }
        """,
        editorConfig);

    // A comment between the keyword and the expression would have to move: left alone.
    [Fact]
    public Task ExpressionAccessor_WithACommentInside_NotReported() => VerifyNoDiagnosticsAsync(
        LongSetter.Replace("MARK => this.x;", "get => /* cached */ this.x;") + "\n"
        + LongSetter.Replace("class C", "class D").Replace("MARK => this.x;", "get /* cached */ => this.x;") + "\n"
        + LongSetter.Replace("class C", "class E").Replace("MARK => this.x;", "get => this.x /* cached */;"));
}
