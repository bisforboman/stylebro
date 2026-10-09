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

    // The line is measured as the other fixes leave it: BRO1108 splits the parameters ('        long d) : base(a)', 25),
    // BRO1109 moves '(' up ('        long a) : base(1)', 25). As the text stands, each joined line would be too long (33,
    // 26 > 25). The last one fits either way: BRO1109 and BRO1108 both rewrite the gap after its '('.
    [Fact]
    public Task SameLine_MeasuredAsTheOtherFixesLeaveTheLine() => VerifyFixAsync(
        """
        class C : B
        {
            public C(int a, long b,
                long c, long d)
                {|BRO1105::|} base(a)
            {
            }

            public C
                (long a)
                {|BRO1105::|} base(1)
            {
            }

            public C
                ( int a, int b,
                long c)
                {|BRO1105::|} base(a)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C(int a, long b,
                long c, long d) : base(a)
            {
            }

            public C
                (long a) : base(1)
            {
            }

            public C
                ( int a, int b,
                long c) : base(a)
            {
            }
        }
        """ + BaseClass,
        editorConfig: "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 25");

    // BRO1110's own_line mode moves ')' down, also when the join takes it along: '    ) : base(a)' (15) fits, as the text
    // stands '        int a, int b) : base(a)' (31) wouldn't. The space before ')' keeps BRO1110's edit after the join's.
    [Fact]
    public Task SameLine_MeasuredAfterBro1110MovesTheParenthesisDown() => VerifyFixAsync(
        """
        class C : B
        {
            public C(
                int a, int b )
                {|BRO1105::|} base(a)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            public C(
                int a, int b) : base(a)
            {
            }
        }
        """ + BaseClass,
        editorConfig: "stylebro_constructor_initializer_placement = same_line\nstylebro_closing_parenthesis_placement = own_line\nmax_line_length = 15");

    // The same lines with BRO1108, BRO1109 and BRO1110 off: their edits aren't counted, so none fits.
    [Fact]
    public Task SameLine_NotReported_WhenTheRulesThatShortenTheLineAreOff() => VerifyNoDiagnosticsAsync(
        """
        class C : B
        {
            public C(int a, long b,
                long c, long d)
                : base(a)
            {
            }

            public C
                (long a)
                : base(1)
            {
            }

            public C(
                int a, int b)
                : base(a)
            {
            }
        }
        """ + BaseClass,
        "stylebro_constructor_initializer_placement = same_line\nstylebro_closing_parenthesis_placement = own_line\nmax_line_length = 25\n"
            + "dotnet_diagnostic.BRO1108.severity = none\ndotnet_diagnostic.BRO1109.severity = none\ndotnet_diagnostic.BRO1110.severity = none");

    // BRO1404 adds 'private ' ('    private C(int a) : base(a)', 30 > 25); with BRO1404 off, '    C(int a) : base(a)' fits.
    [Fact]
    public Task SameLine_NotReported_WhenAnAddedModifierMakesItTooLong() => VerifyNoDiagnosticsAsync(
        """
        class C : B
        {
            C(int a)
                : base(a)
            {
            }
        }
        """ + BaseClass,
        "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 25");

    [Fact]
    public Task SameLine_WithoutBro1404_TheModifierIsNotCounted() => VerifyFixAsync(
        """
        class C : B
        {
            C(int a)
                {|BRO1105::|} base(a)
            {
            }
        }
        """ + BaseClass,
        """
        class C : B
        {
            C(int a) : base(a)
            {
            }
        }
        """ + BaseClass,
        editorConfig: "stylebro_constructor_initializer_placement = same_line\nmax_line_length = 25\ndotnet_diagnostic.BRO1404.severity = none");
}
