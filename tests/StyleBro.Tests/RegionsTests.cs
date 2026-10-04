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
}
