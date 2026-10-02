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
        return VerifyFixAsync(source, fixedSource, editorConfig: "indent_style = tab\n");
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
}
