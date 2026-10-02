using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.DocumentationBlankLinesAnalyzer, StyleBro.CodeFixes.Layout.DocumentationBlankLinesCodeFixProvider>;

namespace StyleBro.Tests;

public class DocumentationBlankLinesTests
{
    [Fact]
    public Task BlankLines_AroundDocumentation_AreFixed() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            /// <summary>First member, after the brace.</summary>
            private int f;
            {|BRO1513:///|} <summary>Right after a field.</summary>
            public int G { get; set; }

            /// <summary>Blank line below.</summary>
        {|BRO1511:|}
            [Obsolete]
            public void M()
            {
                do
                {
                    this.f++;
                }

                {|BRO1512:while|} (this.f < 3);
            }

            /// <summary>Two blank lines below.</summary>
        {|BRO1511:|}
            /// <remarks>A second block.</remarks>

            public void N()
            {
            }
            {|BRO1513:///|} <summary>Right after a method.</summary>
            public void O()
            {
            }

            #region R
            {|BRO1513:///|} <summary>After a region.</summary>
            public void P()
            {
            }
            #endregion
        }
        """,
        """
        using System;

        public class C
        {
            /// <summary>First member, after the brace.</summary>
            private int f;

            /// <summary>Right after a field.</summary>
            public int G { get; set; }

            /// <summary>Blank line below.</summary>
            [Obsolete]
            public void M()
            {
                do
                {
                    this.f++;
                }
                while (this.f < 3);
            }

            /// <summary>Two blank lines below.</summary>
            /// <remarks>A second block.</remarks>
            public void N()
            {
            }

            /// <summary>Right after a method.</summary>
            public void O()
            {
            }

            #region R

            /// <summary>After a region.</summary>
            public void P()
            {
            }
            #endregion
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        /// <summary>At the start of the file.</summary>
        public class C
        {
            // a plain comment
            /// <summary>Below a comment.</summary>
            public void M()
            {
            }

        #pragma warning disable CS1591
            /// <summary>After pragma.</summary>
            public void N()
            {
            }
        #pragma warning restore CS1591

        #if DEBUG
            /// <summary>After if.</summary>
            public void O()
            {
            }
        #endif
        }
        """);
}
