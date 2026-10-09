using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ConstructorInitializerLineAnalyzer, StyleBro.CodeFixes.Readability.ConstructorInitializerLineCodeFixProvider>;

namespace StyleBro.Tests;

public class ConstructorInitializerLineTests
{
    private const string BaseClass = """

        class B
        {
            public B() { }
            public B(int x) { }
        }
        """;

    [Fact]
    public Task Initializers_MoveToTheirOwnLine() => VerifyFixAsync(
        """
        class C : B
        {
            public C() {|BRO1105::|} base() { }

            public C(int x) {|BRO1105::|} this()
            {
            }

            public C(long l) {|BRO1105::|}
                base(2)
            {
            }

            public C(float f) /* why */ {|BRO1105::|} this() { }

            public C(byte b) {|BRO1105::|} this() => System.Console.WriteLine();
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C()
                : base() { }

            public C(int x)
                : this()
            {
            }

            public C(long l)
                : base(2)
            {
            }

            public C(float f) /* why */
                : this() { }

            public C(byte b)
                : this() => System.Console.WriteLine();
        }
        """ + BaseClass);

    [Fact]
    public Task MultiLineArguments_AreKept() => VerifyFixAsync(
        """
        class C : B
        {
            public C() {|BRO1105::|} base(
                3)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C()
                : base(
                3)
            {
            }
        }
        """ + BaseClass);

    [Fact]
    public Task IndentationComesFromEditorConfig() => VerifyFixAsync(
        """
        class C : B
        {
            public C() {|BRO1105::|} base() { }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C()
              : base() { }
        }
        """ + BaseClass,
        editorConfig: "indent_style = space\nindent_size = 2\n");

    [Fact]
    public Task TabIndentation_FromEditorConfig()
    {
        var source = "class C : B\n{\n\tpublic C() {|BRO1105::|} base() { }\n}\n" + BaseClass.Replace("\r\n", "\n");
        var fixedSource = "class C : B\n{\n\tpublic C()\n\t\t: base() { }\n}\n" + BaseClass.Replace("\r\n", "\n");
        return VerifyFixAsync(source, fixedSource, editorConfig: "indent_style = tab\nstylebro_constructor_initializer_placement = own_line\n");
    }

    [Fact]
    public Task AlreadyOnItsOwnLine_PrimaryConstructorsAndComments_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C : B
        {
            public C()
                : base()
            {
            }

            public C(int x) : /* keep */ base(x)
            {
            }
        }

        class D(int x) : B(x)
        {
        }
        """ + BaseClass);

    // stylebro_constructor_initializer_placement = same_line: the initializer joins the line of the parameter list's ')'
    // when the joined line (up to the initializer's end) fits max_line_length. The ')' that BRO1110 moves to the last
    // parameter moves in the same edit.
    [Fact]
    public Task SameLine_InitializersJoinTheParameterList() => VerifyFixAsync(
        """
        class C : B
        {
            public C()
                {|BRO1105::|} base() { }

            public C(string a, int b) {|BRO1105::|}
                base(b)
            {
            }

            public C(string a, string b)
                {|BRO1105::|} this(a, 1)
            {
            }

            public C(
                long a,
                long b)
                {|BRO1105::|} base(2)
            {
            }

            public C(
                byte a
            )
                {|BRO1105::|} base(a)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C() : base() { }

            public C(string a, int b) : base(b)
            {
            }

            public C(string a, string b) : this(a, 1)
            {
            }

            public C(
                long a,
                long b) : base(2)
            {
            }

            public C(
                byte a) : base(a)
            {
            }
        }
        """ + BaseClass,
        editorConfig: "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 45");

    [Fact]
    public Task SameLine_WithoutBro1110_TheParenthesisStays() => VerifyFixAsync(
        """
        class C : B
        {
            public C(
                byte a
            )
                {|BRO1105::|} base(a)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C(
                byte a
            ) : base(a)
            {
            }
        }
        """ + BaseClass,
        editorConfig: "stylebro_constructor_initializer_placement = same_line\ndotnet_diagnostic.BRO1110.severity = none");

    // BRO1110's own_line mode only reindents a ')' already on its own line, so the initializer joins it there.
    [Fact]
    public Task SameLine_WithAClosingParenthesisBro1110Reindents() => VerifyFixAsync(
        """
        class C : B
        {
            public C(
                byte a
                    )
                {|BRO1105::|} base(a)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C(
                byte a
                    ) : base(a)
            {
            }
        }
        """ + BaseClass,
        editorConfig: "stylebro_constructor_initializer_placement = same_line\nstylebro_closing_parenthesis_placement = own_line");

    // Too long (55 > 45), a comment in the gap (also one BRO1110 would carry along), arguments on several lines, already
    // joined.
    [Fact]
    public Task SameLine_NotReported() => VerifyNoDiagnosticsAsync(
        """
        class C : B
        {
            public C(string first, string second)
                : base(1)
            {
            }

            public C(int a) // why
                : base(a)
            {
            }

            public C(
                long a // why
            )
                : base(1)
            {
            }

            public C(byte a)
                : base(
                    a)
            {
            }

            public C(short a) : base(a)
            {
            }
        }
        """ + BaseClass,
        "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 45");
}
