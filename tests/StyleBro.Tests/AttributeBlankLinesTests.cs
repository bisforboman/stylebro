using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.AttributeBlankLinesAnalyzer, StyleBro.CodeFixes.Layout.AttributeBlankLinesCodeFixProvider>;

namespace StyleBro.Tests;

public class AttributeBlankLinesTests
{
    [Fact]
    public Task BlankLinesAfterAttributes_AreRemoved() => VerifyFixAsync(
        """
        using System;

        [Serializable]
        {|BRO1525:|}
        public class C
        {
            [Obsolete]
        {|BRO1525:|}

            [CLSCompliant(false)]
        {|BRO1525:|}
            public void M()
            {
            }

            public int P
            {
                [Obsolete]
        {|BRO1525:|}
                get => 1;
            }

            public void N(
                [CLSCompliant(false)]
        {|BRO1525:|}
                int x)
            {
            }
        }
        """,
        """
        using System;

        [Serializable]
        public class C
        {
            [Obsolete]
            [CLSCompliant(false)]
            public void M()
            {
            }

            public int P
            {
                [Obsolete]
                get => 1;
            }

            public void N(
                [CLSCompliant(false)]
                int x)
            {
            }
        }
        """);

    [Fact]
    public Task KeepsTheLineEndings() => VerifyFixAsync(
        "public class C\r\n{\r\n    [System.Obsolete]\r\n{|BRO1525:|}    \r\n    public void M()\r\n    {\r\n    }\r\n}\r\n",
        "public class C\r\n{\r\n    [System.Obsolete]\r\n    public void M()\r\n    {\r\n    }\r\n}\r\n");

    [Fact]
    public Task Skipped() => VerifyNoDiagnosticsAsync(
        """
        using System;

        [assembly: CLSCompliant(false)]

        public class C
        {
            [Obsolete]

            // A comment between.
            public void M()
            {
            }

            [CLSCompliant(false)]

        #if DEBUG
            public void N()
            {
            }
        #endif

            [Obsolete] public void O()
            {
            }
        }
        """);

    [Fact]
    public Task SyntaxErrors_AreSkipped() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            [System.Obsolete]

        {|CS1519:}|}
        """);
}
