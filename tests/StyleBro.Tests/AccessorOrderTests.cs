using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Ordering.AccessorOrderAnalyzer, StyleBro.CodeFixes.Ordering.AccessorOrderCodeFixProvider>;

namespace StyleBro.Tests;

public class AccessorOrderTests
{
    [Fact]
    public Task Accessors_AreSwapped() => VerifyFixAsync("""
        using System;

        public class C
        {
            private int x;

            public int Single
            {
                {|BRO1003:set|} { this.x = value; }
                get { return this.x; }
            }

            public int Multi
            {
                // the setter
                {|BRO1003:set|}
                {
                    this.x = value;
                }

                // the getter
                get
                {
                    return this.x;
                }
            }

            public int OneLine { {|BRO1003:set|} { this.x = value; } get { return this.x; } }

            public int Auto { {|BRO1003:set|}; get; }

            public event EventHandler Changed
            {
                {|BRO1004:remove|} { }
                add { }
            }
        }
        """, """
        using System;

        public class C
        {
            private int x;

            public int Single
            {
                get { return this.x; }
                set { this.x = value; }
            }

            public int Multi
            {
                // the getter
                get
                {
                    return this.x;
                }

                // the setter
                set
                {
                    this.x = value;
                }
            }

            public int OneLine { get { return this.x; } set { this.x = value; } }

            public int Auto { get; set; }

            public event EventHandler Changed
            {
                add { }
                remove { }
            }
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        public class C
        {
            private int x;

            public int Right { get; set; }

            public int Odd
            {
                set { this.x = value; } get
                {
                    return this.x;
                }
            }

            public int OnlySet { set { this.x = value; } }

            public int OpenSameLine
            { set
                {
                    this.x = value;
                }

                get { return this.x; }
            }
        }
        """);
}