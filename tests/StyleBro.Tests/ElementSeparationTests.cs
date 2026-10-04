using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Layout.ElementSeparationAnalyzer, StyleBro.CodeFixes.Layout.ElementSeparationCodeFixProvider>;

namespace StyleBro.Tests;

public class ElementSeparationTests
{
    [Fact]
    public Task AdjacentMembers_GetABlankLineAboveTheirCommentsAndAttributes() => VerifyFixAsync(
        """
        using System;
        using System.Text;
        {|BRO1505:|}namespace N
        {
            class C
            {
                int a;
                int b;
        {|BRO1505:|}        public event EventHandler E;
        {|BRO1505:|}        public int P { get; set; }
        {|BRO1505:|}        // A comment.
                void M() { }
        {|BRO1505:|}        /// <summary>Doc.</summary>
                void N() { }
        {|BRO1505:|}        [Obsolete]
                void O() { }
            }
        {|BRO1505:|}    class D { }
        }
        """,
        """
        using System;
        using System.Text;

        namespace N
        {
            class C
            {
                int a;
                int b;

                public event EventHandler E;

                public int P { get; set; }

                // A comment.
                void M() { }

                /// <summary>Doc.</summary>
                void N() { }

                [Obsolete]
                void O() { }
            }

            class D { }
        }
        """);

    [Fact]
    public Task Accessors_NeedABlankLineWhenEitherIsMultiLine() => VerifyFixAsync(
        """
        class C
        {
            int a;

            int P
            {
                get { return a; }
                set { a = value; }
            }

            int Q
            {
                get { return a; }
        {|BRO1505:|}        set
                {
                    a = value;
                }
            }
        }
        """,
        """
        class C
        {
            int a;

            int P
            {
                get { return a; }
                set { a = value; }
            }

            int Q
            {
                get { return a; }

                set
                {
                    a = value;
                }
            }
        }
        """);

    [Fact]
    public Task ExpressionBodiedAccessors_NeverNeedABlankLine() => VerifyNoDiagnosticsAsync("""
        class C
        {
            int a;

            int P
            {
                get => a;
                set
                {
                    a = value;
                }
            }

            int Q
            {
                get
                {
                    return a;
                }
                set => a = value;
            }
        }
        """);

    [Fact]
    public Task FieldAfterAMultiLineField_GetsABlankLine() => VerifyFixAsync(
        """
        using System;

        class C
        {
            int[] a = new[]
            {
                1,
            };
        {|BRO1505:|}    int b;
            int c;
            string d =
                "x";
        {|BRO1505:|}    [Obsolete]
            int e;
            int f;
        }
        """,
        """
        using System;

        class C
        {
            int[] a = new[]
            {
                1,
            };

            int b;
            int c;
            string d =
                "x";

            [Obsolete]
            int e;
            int f;
        }
        """);

    [Fact]
    public Task AttributeLinesDoNotMakeAFieldMultiLine() => VerifyNoDiagnosticsAsync("""
        using System;

        class C
        {
            [Obsolete]
            int a;
            int b;
            [Obsolete] [NonSerialized]
            int c;
            int d;
        }
        """);

    [Fact]
    public Task TwoMembersOnOneLine_AreSplit() => VerifyFixAsync(
        """
        class C
        {
            void M() { } {|BRO1505:|}void N() { }
        }
        """,
        """
        class C
        {
            void M() { }

            void N() { }
        }
        """);

    [Fact]
    public Task BlankLineBelowACommentOrDirective_IsEnough() => VerifyNoDiagnosticsAsync("""
        class C
        {
            void A() { }
            // A comment, then a blank line.

            void B() { }
        #if !NEVER

            void C2() { }
        #endif
        }
        """);

    [Fact]
    public Task FileScopedNamespace_IsSeparatedFromTheFirstType() => VerifyFixAsync(
        """
        namespace N;
        {|BRO1505:|}class C { }
        """,
        """
        namespace N;

        class C { }
        """);

    [Fact]
    public Task OneLineType_IsExpandedLikeBRO1509() => VerifyFixAsync(
        """
        public class Gauge { private int level; {|BRO1505:|}public int Level => this.level; {|BRO1505:|}public void Reset() { this.level = 0; } }
        """,
        """
        public class Gauge
        {
            private int level;

            public int Level => this.level;

            public void Reset() { this.level = 0; }
        }
        """);
}
