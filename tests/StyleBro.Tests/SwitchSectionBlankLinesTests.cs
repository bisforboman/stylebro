using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.SwitchSectionBlankLinesAnalyzer, StyleBro.CodeFixes.Layout.SwitchSectionBlankLinesCodeFixProvider>;

namespace StyleBro.Tests;

public class SwitchSectionBlankLinesTests
{
    [Fact]
    public Task Include_AddsTheBlankLine() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int x)
            {
                switch (x)
                {
                    case 1:
                        return 1;
                    {|BRO1526:case|} 2:
                    case 3:
                        x++;
                        break;
                    {|BRO1526:default|}:
                        return 0;
                }

                return x;
            }
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
                        return 1;

                    case 2:
                    case 3:
                        x++;
                        break;

                    default:
                        return 0;
                }

                return x;
            }
        }
        """);

    [Fact]
    public Task Include_KeepsTheLineEndings() => VerifyFixAsync(
        "class C\r\n{\r\n    void M(int x)\r\n    {\r\n        switch (x)\r\n        {\r\n            case 1:\r\n                break;\r\n            {|BRO1526:default|}:\r\n                break;\r\n        }\r\n    }\r\n}\r\n",
        "class C\r\n{\r\n    void M(int x)\r\n    {\r\n        switch (x)\r\n        {\r\n            case 1:\r\n                break;\r\n\r\n            default:\r\n                break;\r\n        }\r\n    }\r\n}\r\n");

    [Fact]
    public Task Omit_RemovesTheBlankLines() => VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                        break;

                    {|BRO1526:case|} 2:
                        break;
                    case 3:
                        break;


                    {|BRO1526:default|}:
                        break;
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    default:
                        break;
                }
            }
        }
        """,
        editorConfig: "stylebro_blank_line_between_switch_sections = omit\n");

    [Fact]
    public Task OmitAfterBlock_RemovesTheBlankLineOnlyAfterABlock_WhenBro1519IsOff() => VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                    {
                        break;
                    }

                    {|BRO1526:case|} 2:
                        break;
                    {|BRO1526:default|}:
                        break;
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                    {
                        break;
                    }
                    case 2:
                        break;

                    default:
                        break;
                }
            }
        }
        """,
        editorConfig: "stylebro_blank_line_between_switch_sections = omit_after_block\ndotnet_diagnostic.BRO1519.severity = none\n");

    [Fact]
    public Task AfterAMultiLineBlock_IsLeftToBro1519_WhenItIsOn() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                    {
                        break;
                    }
                    case 2:
                        if (x > 0)
                        {
                            return;
                        }
                        else
                        {
                            return;
                        }

                    default:
                        break;
                }
            }
        }
        """,
        editorConfig: "stylebro_blank_line_between_switch_sections = omit\n");

    [Fact]
    public Task Include_ASingleLineBlock_IsNotBro1519s() => VerifyFixAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1: { break; }
                    {|BRO1526:default|}:
                        break;
                }
            }
        }
        """,
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1: { break; }

                    default:
                        break;
                }
            }
        }
        """);

    [Fact]
    public Task Skipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(int x)
            {
                switch (x)
                {
                    case 1: return 1; case 2: return 2;

                    case 3:
                        return 3; // trailing comments are fine
                    // a comment between sections
                    case 4:
                        return 4;
        #if DEBUG
                    case 5:
                        return 5;
        #endif
                    default:
                        return 0;
                }
            }

            public int N(int x) => x switch
            {
                1 => 1,
                _ => 0,
            };
        }
        """);

    [Fact]
    public Task SyntaxErrors_Skipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                        x = {|CS1525:;|}
                        break;
                    case 2:
                        break;
                }
            }
        }
        """);

    [Fact]
    public Task SeveralBlankLines_AreBro1517s() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public void M(int x)
            {
                switch (x)
                {
                    case 1:
                        break;


                    default:
                        break;
                }
            }
        }
        """);
}
