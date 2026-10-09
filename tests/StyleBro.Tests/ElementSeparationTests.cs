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

    // Like StyleCop's unreleased master (2aeb4e3d): two single-line properties may sit together (attribute lines don't
    // count); a property spanning several lines needs a blank line on both sides.
    [Fact]
    public Task SingleLineProperties_MaySitTogether() => VerifyFixAsync(
        """
        using System;

        class C
        {
            public int A { get; set; }
            public int B { get; set; }
            [Obsolete]
            public int C1 => 1;
        {|BRO1505:|}    public int D
            {
                get => 1;
            }
        {|BRO1505:|}    public int E { get; set; }
            public int F { get; } = 2;
        {|BRO1505:|}    public void M() { }
        }
        """,
        """
        using System;

        class C
        {
            public int A { get; set; }
            public int B { get; set; }
            [Obsolete]
            public int C1 => 1;

            public int D
            {
                get => 1;
            }

            public int E { get; set; }
            public int F { get; } = 2;

            public void M() { }
        }
        """);

    // Ocelot: a documented property is set apart like any other member (BRO1601's '<inheritdoc/>' made the result depend
    // on the fix order otherwise).
    [Fact]
    public Task DocumentedProperties_AreSetApart() => VerifyFixAsync(
        """
        class C
        {
            public int A { get; set; }

            /// <summary>Gets B.</summary>
            public int B { get; set; }
        {|BRO1505:|}    public int C1 { get; set; }
        {|BRO1505:|}    /// <inheritdoc/>
            public override string ToString() => string.Empty;
        }
        """,
        """
        class C
        {
            public int A { get; set; }

            /// <summary>Gets B.</summary>
            public int B { get; set; }

            public int C1 { get; set; }

            /// <inheritdoc/>
            public override string ToString() => string.Empty;
        }
        """);

    // An auto-property that BRO1527 puts on one line counts as one line while BRO1527 is on (so the result doesn't depend
    // on which fix runs first), and as several lines while it's off.
    [Fact]
    public Task MultiLineAutoProperty_CountsAsOneLineWhileBro1527IsOn() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            public int A { get; set; }
            public int B
            {
                get;
                set;
            }
            public int D { get; set; }
        }
        """);

    [Fact]
    public Task MultiLineAutoProperty_CountsAsSeveralLinesWhileBro1527IsOff() => VerifyFixAsync(
        """
        class C
        {
            public int A { get; set; }
        {|BRO1505:|}    public int B
            {
                get;
                set;
            }
        {|BRO1505:|}    public int D { get; set; }
        }
        """,
        """
        class C
        {
            public int A { get; set; }

            public int B
            {
                get;
                set;
            }

            public int D { get; set; }
        }
        """,
        "dotnet_diagnostic.BRO1527.severity = none\n");

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

    // stylebro_allow_adjacent_single_line_members: single-line members without a block body may sit together (StyleCop
    // #2441). Still separated: a block body (BRO1509 expands it), a 'where' clause (BRO1111 may split it), a member that
    // spans several lines, accessors with block bodies, constructors and fields next to them.
    [Fact]
    public Task AdjacentSingleLineMembers_WhenAllowed() => VerifyFixAsync(
        """
        using System;

        public abstract class C
        {
            private int count;
        {|BRO1505:|}    public abstract void A();
            public int B() => 1;
            public int this[int i] => i;
            public event EventHandler? Changed;
        {|BRO1505:|}    public event EventHandler? Closed { add { } remove { } }
        {|BRO1505:|}    public event EventHandler? Opened { add => Changed += value; remove => Changed -= value; }
            public static C operator +(C c, int i) => c;
            [Obsolete]
            public int P { get; set; }
        {|BRO1505:|}    public void D() { }
        {|BRO1505:|}    public abstract void E<T>() where T : class;
        {|BRO1505:|}    public abstract void F();
        {|BRO1505:|}    public int Q { get { return 1; } }
        {|BRO1505:|}    public event EventHandler?
                Multi;
        {|BRO1505:|}    public abstract void H();
        {|BRO1505:|}    public int G() =>
                2;
        {|BRO1505:|}    public C() { }
        }
        """,
        """
        using System;

        public abstract class C
        {
            private int count;

            public abstract void A();
            public int B() => 1;
            public int this[int i] => i;
            public event EventHandler? Changed;

            public event EventHandler? Closed { add { } remove { } }

            public event EventHandler? Opened { add => Changed += value; remove => Changed -= value; }
            public static C operator +(C c, int i) => c;
            [Obsolete]
            public int P { get; set; }

            public void D() { }

            public abstract void E<T>() where T : class;

            public abstract void F();

            public int Q { get { return 1; } }

            public event EventHandler?
                Multi;

            public abstract void H();

            public int G() =>
                2;

            public C() { }
        }
        """,
        editorConfig: "stylebro_allow_adjacent_single_line_members = true");
}
