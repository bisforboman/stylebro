using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Documentation.SummaryLayoutAnalyzer, StyleBro.CodeFixes.Documentation.SummaryLayoutCodeFixProvider>;

namespace StyleBro.Tests;

public class SummaryLayoutTests
{
    private const string SingleLine = "stylebro_summary_layout = single_line_when_fits\n";

    [Fact]
    public Task MultiLine_PutsTheTagsOnTheirOwnLines() => VerifyFixAsync(
        """
        /// {|BRO1616:<summary>|}A shape.</summary>
        public class Shape
        {
            /// {|BRO1616:<summary>|}  Gets the <see cref="Shape"/>'s name.  </summary>
            /// <returns>The name.</returns>
            public string Name() => "x";

            /// {|BRO1616:<summary>|}Draws the shape
            /// on the screen.</summary>
            public void Draw()
            {
            }

            /// {|BRO1616:<summary>|}
            ///   Erases the shape.</summary>
            /// <remarks>Slowly.</remarks>
            public void Erase()
            {
            }

            /// {|BRO1616:<summary>|}Hides the shape.
            ///   </summary>
            public void Hide()
            {
            }
        }
        """,
        """
        /// <summary>
        /// A shape.
        /// </summary>
        public class Shape
        {
            /// <summary>
            /// Gets the <see cref="Shape"/>'s name.
            /// </summary>
            /// <returns>The name.</returns>
            public string Name() => "x";

            /// <summary>
            /// Draws the shape
            /// on the screen.
            /// </summary>
            public void Draw()
            {
            }

            /// <summary>
            ///   Erases the shape.
            /// </summary>
            /// <remarks>Slowly.</remarks>
            public void Erase()
            {
            }

            /// <summary>
            /// Hides the shape.
            ///   </summary>
            public void Hide()
            {
            }
        }
        """);

    [Fact]
    public Task SingleLineWhenFits_JoinsOneLineOfText() => VerifyFixAsync(
        """
        /// {|BRO1616:<summary>|}
        /// A shape.
        /// </summary>
        public class Shape
        {
            /// {|BRO1616:<summary>|}
            ///   Gets the <see cref="Shape"/>'s <c>name</c>.
            ///
            /// </summary>
            public string Name() => "x";

            /// {|BRO1616:<summary>|}Draws the shape.
            /// </summary>
            public void Draw()
            {
            }

            /// {|BRO1616:<summary>|}Moves the shape
            /// to the left.</summary>
            public void Move()
            {
            }

            /// <summary>Erases the shape.</summary>
            public void Erase()
            {
            }
        }
        """,
        """
        /// <summary>A shape.</summary>
        public class Shape
        {
            /// <summary>Gets the <see cref="Shape"/>'s <c>name</c>.</summary>
            public string Name() => "x";

            /// <summary>Draws the shape.</summary>
            public void Draw()
            {
            }

            /// <summary>
            /// Moves the shape
            /// to the left.
            /// </summary>
            public void Move()
            {
            }

            /// <summary>Erases the shape.</summary>
            public void Erase()
            {
            }
        }
        """,
        SingleLine);

    [Fact]
    public Task SingleLineWhenFits_KeepsLongTextAndBlocksOnTheirOwnLines() => VerifyNoDiagnosticsAsync(
        """
        public class Shape
        {
            /// <summary>
            /// Draws the shape on the screen, slowly.
            /// </summary>
            public void Draw()
            {
            }

            /// <summary>
            /// <para>Erases the shape.</para>
            /// </summary>
            public void Erase()
            {
            }

            /// <summary>
            /// Moves the shape.
            /// To the left.
            /// </summary>
            public void Move()
            {
            }

            /// <summary>Gets the shape's name, which is also too long to fit within the configured line length.</summary>
            public string Name() => "x";
        }
        """,
        SingleLine + "max_line_length = 50\n");

    [Fact]
    public Task SingleLineWhenFits_SplitsWhenTheLineIsTooLong() => VerifyFixAsync(
        """
        public class Shape
        {
            /// {|BRO1616:<summary>|}Draws the shape on the screen, slowly.
            /// </summary>
            public void Draw()
            {
            }
        }
        """,
        """
        public class Shape
        {
            /// <summary>
            /// Draws the shape on the screen, slowly.
            /// </summary>
            public void Draw()
            {
            }
        }
        """,
        SingleLine + "max_line_length = 50\n");

    [Fact]
    public Task SingleLineWhenFits_SplitsAMixedSummaryWithABlock() => VerifyFixAsync(
        """
        public class Shape
        {
            /// {|BRO1616:<summary>|}<para>Draws the shape.</para>
            /// </summary>
            public void Draw()
            {
            }
        }
        """,
        """
        public class Shape
        {
            /// <summary>
            /// <para>Draws the shape.</para>
            /// </summary>
            public void Draw()
            {
            }
        }
        """,
        SingleLine);

    [Fact]
    public Task SummariesThatCantBeLaidOut_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        public class Shape
        {
            /// <summary></summary>
            public void Empty()
            {
            }

            /// <summary>
            /// </summary>
            public void Blank()
            {
            }

            /// <summary/>
            public void Short()
            {
            }

            /// <summary>Draws.</summary> <remarks>Slowly.</remarks>
            public void Draw()
            {
            }

            /// <remarks>Erases.</remarks> <summary>Erases the shape.</summary>
            public void Erase()
            {
            }

            /** <summary>Moves.</summary> */
            public void Move()
            {
            }

            /// <remarks>Only remarks.</remarks>
            public void Hide()
            {
            }
        }
        """);

    [Fact]
    public Task SummariesWithoutTheirEndTag_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        public class Shape
        {
            /// <summary>Draws.
            public void Draw()
            {
            }

            /// <summary>Erases.</remarks>
            public void Erase()
            {
            }
        }
        """);

    [Fact]
    public Task KeepsCrLfAndTheIndentation() => VerifyFixAsync(
        "class C\r\n{\r\n\t/// {|BRO1616:<summary>|}Gets it.</summary>\r\n\tint P => 1;\r\n}\r\n",
        "class C\r\n{\r\n\t/// <summary>\r\n\t/// Gets it.\r\n\t/// </summary>\r\n\tint P => 1;\r\n}\r\n");
}
