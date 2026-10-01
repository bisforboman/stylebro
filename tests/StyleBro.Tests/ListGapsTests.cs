using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.ListGapsAnalyzer, StyleBro.CodeFixes.Readability.ListGapsCodeFixProvider>;

namespace StyleBro.Tests;

public class ListGapsTests
{
    [Fact]
    public Task ListGaps_AreFixed() => VerifyFixAsync("""
        using System;

        public class C
        {
            public void Empty(
                {|BRO1116:)|}
            {
                this.Empty(
                {|BRO1116:)|};
            }

            public void Commas(int a

                {|BRO1117:,|} int b)
            {
                this.Commas(1
                    {|BRO1117:,|} 2);
            }

            public void Blank(

                {|BRO1118:int a|},

                {|BRO1119:int b|})
            {
            }

            public void Trailing( // the parameters

                {|BRO1118:int a|})
            {
            }
        }
        """, """
        using System;

        public class C
        {
            public void Empty()
            {
                this.Empty();
            }

            public void Commas(int a,
                int b)
            {
                this.Commas(1,
                    2);
            }

            public void Blank(
                int a,
                int b)
            {
            }

            public void Trailing( // the parameters
                int a)
            {
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;

        [AttributeUsage(AttributeTargets.All)]
        public class TagAttribute : Attribute
        {
        }

        [Tag(
        )]
        public class C
        {
            public void M(
                // the a
                int a)
            {
            }

            public void N(int a, /* b */
                int b)
            {
            }
        }
        """);
}