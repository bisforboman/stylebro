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

    // Trial 60, EasyCaching: BRO1513's blank line goes below the region line, where it would count for BRO1505; the fix
    // adds BRO1505's above it too, which BRO1505's fix would have added had it run first (FixOrderTests).
    [Fact]
    public Task BlankLineBeforeDocumentation_BelowARegionRightAfterAMember_GetsBRO1505sToo() => VerifyFixAsync(
        """
        public class C
        {
            public void M()
            {
            }
            #region R
            {|BRO1513:///|} <summary>After a region.</summary>
            public void N()
            {
            }
            #endregion
        }
        """,
        """
        public class C
        {
            public void M()
            {
            }

            #region R

            /// <summary>After a region.</summary>
            public void N()
            {
            }
            #endregion
        }
        """);

    // With BRO1505 off, only BRO1513's own blank line.
    [Fact]
    public Task BlankLineBeforeDocumentation_WithBRO1505Off_AddsOnlyItsOwn() => VerifyFixAsync(
        """
        public class C
        {
            public void M()
            {
            }
            #region R
            {|BRO1513:///|} <summary>After a region.</summary>
            public void N()
            {
            }
            #endregion
        }
        """,
        """
        public class C
        {
            public void M()
            {
            }
            #region R

            /// <summary>After a region.</summary>
            public void N()
            {
            }
            #endregion
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none");

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
