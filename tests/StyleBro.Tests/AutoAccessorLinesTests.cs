using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.AutoAccessorLinesAnalyzer, StyleBro.CodeFixes.Layout.AutoAccessorLinesCodeFixProvider>;

namespace StyleBro.Tests;

public class AutoAccessorLinesTests
{
    [Fact]
    public Task MultiLineAutoAccessors_GoOnOneLine() => Verify.VerifyFixAsync(
        """
        public abstract class C
        {
            public int Simple
            {|BRO1527:{|}
                get;
                set;
            }

            public int Modifiers {|BRO1527:{|}
                get;  private set;
            }

            public int Initialized
            {|BRO1527:{|}
                get;
                protected set;
            } = 5;

            public abstract int this[int i]
            {|BRO1527:{|}
                get;
                set;
            }
        }

        public interface I
        {
            string Name
            {|BRO1527:{|}
                get;
            }
        }
        """,
        """
        public abstract class C
        {
            public int Simple { get; set; }

            public int Modifiers { get; private set; }

            public int Initialized { get; protected set; } = 5;

            public abstract int this[int i] { get; set; }
        }

        public interface I
        {
            string Name { get; }
        }
        """);

    [Fact]
    public Task Skipped_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;

        public class C
        {
            private int x;

            public int OneLine { get; set; }

            public int Body
            {
                get { return this.x; }
                set { this.x = value; }
            }

            public int Expression
            {
                get => this.x;
                set => this.x = value;
            }

            public int Attributed
            {
                [Obsolete]
                get;
                set;
            }

            public int AttributedInline
            {
                [Obsolete] get;
                set;
            }

            public int Commented
            {
                // the getter
                get;
                set;
            }

            public int Inline
            {
                get; /* set */
                set;
            }

            public int Directive
            {
                get;
        #if DEBUG
                set;
        #endif
            }

            public int InitializerBelow
            {
                get;
                set;
            }
            = 5;

            public int Arrow => this.x;

            public int SplitAccessor
            {
                get;
                private
                    set;
            }

            public int InsideAccessor
            {
                get;
                private /* only here */ set;
            }
        }
        """);

    [Fact]
    public Task NoAccessors_NotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int {|CS0548:Empty|}
            {
            }
        }
        """);

    [Fact]
    public Task PreserveSingleLineBlocksFalse_NotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int Simple
            {
                get;
                set;
            }
        }
        """,
        "csharp_preserve_single_line_blocks = false\n");

    [Fact]
    public Task BlankLinesNextToTheProperty_AddedWhenBro1505WantsThem() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public int A { get; set; }
            public int B
            {|BRO1527:{|}
                get;
                set;
            }
            public int D { get; set; }
        }
        """,
        """
        public class C
        {
            public int A { get; set; }

            public int B { get; set; }

            public int D { get; set; }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = warning\n");

    [Fact]
    public Task BlankLineAfterTheBrace_AddedWhenBro1519WantsIt() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public int A { get; set; }
            public int B
            {|BRO1527:{|}
                get;
                set;
            }
            public int D { get; set; }
        }
        """,
        """
        public class C
        {
            public int A { get; set; }
            public int B { get; set; }

            public int D { get; set; }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none\ndotnet_diagnostic.BRO1519.severity = warning\n");

    [Fact]
    public Task NoBlankLines_WhenTheirRulesAreOff() => Verify.VerifyFixAsync(
        """
        public class C
        {
            public int A { get; set; }
            public int B
            {|BRO1527:{|}
                get;
                set;
            }
            public int D { get; set; }
        }
        """,
        """
        public class C
        {
            public int A { get; set; }
            public int B { get; set; }
            public int D { get; set; }
        }
        """,
        "dotnet_diagnostic.BRO1505.severity = none\ndotnet_diagnostic.BRO1519.severity = none\n");
}
