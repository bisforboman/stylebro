using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.RegionsAnalyzer, StyleBro.CodeFixes.Readability.RegionsCodeFixProvider>;

namespace StyleBro.Tests;

public class RegionsTests
{
    [Fact]
    public Task Regions_AreRemoved() => VerifyFixAsync(
        """
        {|BRO1112:#region Types|}
        public class C
        {
            {|BRO1112:#region Fields|}

            private int x;

            #endregion

            {|BRO1112:#region Methods|}
            public int M()
            {
                {|BRO1113:#region Body|}
                var y = x;
                #endregion
                return y;
            }
            #endregion

            {|BRO1112:#region Empty|}
            #endregion
        }
        #endregion
        """,
        """
        public class C
        {
            private int x;

            public int M()
            {
                var y = x;
                return y;
            }
        }
        """);

    // Kavita: the blank lines left below the comment are found once per removed region and span removed lines.
    [Fact]
    public Task EmptyRegionsBelowAComment_TheBlankLinesGoOnce() => VerifyFixAsync(
        """
        public class C
        {
            // TODO: Implement

            {|BRO1112:#region First|}


            #endregion

            {|BRO1112:#region Second|}


            #endregion

            public int A;
        }
        """,
        """
        public class C
        {
            // TODO: Implement
            public int A;
        }
        """);

    [Fact]
    public Task RegionsAroundDirectives_KeepTheDirectives() => VerifyFixAsync(
        """
        public class C
        {
            {|BRO1112:#region Conditional|}
        #if !NEVER
            private int x;
        #endif
            #endregion
        }
        """,
        """
        public class C
        {
        #if !NEVER
            private int x;
        #endif
        }
        """);

    [Fact]
    public Task RemovingRegions_SortsTheMembers_WhenMemberOrderingIsOn() => VerifyFixAsync(
        """
        public class C
        {
            {|BRO1112:#region Methods|}

            public void M()
            {
            }

            #endregion

            {|BRO1112:#region Fields|}

            private int a;

            #endregion
        }
        """,
        """
        public class C
        {
            private int a;

            public void M()
            {
            }
        }
        """);

    [Fact]
    public Task RemovingRegions_LeavesTheOrder_WhenMemberOrderingIsOff() => VerifyFixAsync(
        """
        public class C
        {
            {|BRO1112:#region Methods|}

            public void M()
            {
            }

            #endregion

            {|BRO1112:#region Fields|}

            private int a;

            #endregion
        }
        """,
        """
        public class C
        {
            public void M()
            {
            }

            private int a;
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1001.severity = none");

    [Fact]
    public Task ACommentAboveTheRemovedLines_LosesTheBlankLineBelowIt() => VerifyFixAsync(
        """
        public class C
        {
            public int M()
            {
                {|BRO1113:#region Usage|}
                var x = 1;

                // x is 1
                #endregion

                return x;
            }
        }
        """,
        """
        public class C
        {
            public int M()
            {
                var x = 1;

                // x is 1
                return x;
            }
        }
        """,
        editorConfig: "stylebro_blank_line_below_standalone_comments = remove");

    // Trial 21: by default the comment stands on its own once the region lines go (a blank line above and below), and BRO1506
    // leaves it.
    [Fact]
    public Task ACommentAboveTheRemovedLines_KeepsTheBlankLine_WhenItStandsOnItsOwn() => VerifyFixAsync(
        """
        public class C
        {
            public int M()
            {
                {|BRO1113:#region Usage|}
                var x = 1;

                // x is 1
                #endregion

                return x;
            }
        }
        """,
        """
        public class C
        {
            public int M()
            {
                var x = 1;

                // x is 1

                return x;
            }
        }
        """);

    [Fact]
    public Task ACommentAboveTheRemovedLines_KeepsTheBlankLine_WhenBRO1506IsOff() => VerifyFixAsync(
        """
        public class C
        {
            public int M()
            {
                {|BRO1113:#region Usage|}
                var x = 1;

                // x is 1
                #endregion

                return x;
            }
        }
        """,
        """
        public class C
        {
            public int M()
            {
                var x = 1;

                // x is 1

                return x;
            }
        }
        """,
        editorConfig: "dotnet_diagnostic.BRO1506.severity = none\nstylebro_blank_line_below_standalone_comments = remove");

    [Fact]
    public Task RegionsInCodeAnIfTurnsOff_AreNotReported() => VerifyFixAsync(
        """
        {|BRO1112:#region License|}
        // header
        #endregion

        #if NEVER
        public class C
        {
            public void M()
            {
                #region Usage
                var x = 1;
                // x is 1
                #endregion

                x++;
            }
        }
        #endif
        """,
        """
        // header

        #if NEVER
        public class C
        {
            public void M()
            {
                #region Usage
                var x = 1;
                // x is 1
                #endregion

                x++;
            }
        }
        #endif
        """);

    // BRO1504's list exemption holds here too: a comment that ends up below an item gets no blank line.
    [Fact]
    public Task ACommentBetweenArgumentsBelowTheRemovedLines_GetsNoBlankLine() => VerifyFixAsync(
        """
        public class C
        {
            public int M(int a, int b)
            {
                return M(
                    1,
                    {|BRO1113:#region Second|}
                    // the second
                    2);
                #endregion
            }
        }
        """,
        """
        public class C
        {
            public int M(int a, int b)
            {
                return M(
                    1,
                    // the second
                    2);
            }
        }
        """);
}
