using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Ordering.MemberOrderingAnalyzer, StyleBro.CodeFixes.Ordering.MemberOrderingCodeFixProvider>;

namespace StyleBro.Tests;

public class MemberOrderingTests
{
    [Fact]
    public Task SortedType_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        using System;

        public class Sorted
        {
            public const int Max = 10;
            private static readonly int s_shared = 1;
            private readonly int _value;
            private int _counter;

            public Sorted(int value) { _value = value; }

            ~Sorted() { }

            public event EventHandler Changed;

            public int Value => _value;

            public int this[int i] => i;

            public static implicit operator int(Sorted s) => s._value;

            public static Sorted operator +(Sorted a, Sorted b) => a;

            public void Run() { _counter++; Changed?.Invoke(this, EventArgs.Empty); }

            private class Nested { }
        }
        """);

    [Fact]
    public Task FieldAfterMethod_IsMovedUp() => VerifyFixAsync("""
        class C
        {
            public void M() { }

            private int {|BRO1001:_x|};
        }
        """, """
        class C
        {
            private int _x;

            public void M() { }
        }
        """);

    [Fact]
    public Task PublicAfterPrivate_IsMovedUp() => VerifyFixAsync("""
        class C
        {
            private void A() { }

            public void {|BRO1001:B|}() { }
        }
        """, """
        class C
        {
            public void B() { }

            private void A() { }
        }
        """);

    [Fact]
    public Task StaticAfterInstance_IsMovedUp() => VerifyFixAsync("""
        class C
        {
            private int _a;
            private static int {|BRO1001:s_b|};
        }
        """, """
        class C
        {
            private static int s_b;
            private int _a;
        }
        """);

    [Fact]
    public Task ConstantAfterField_IsMovedUp() => VerifyFixAsync("""
        class C
        {
            private static int s_a;
            private const int {|BRO1001:B|} = 1;
        }
        """, """
        class C
        {
            private const int B = 1;
            private static int s_a;
        }
        """);

    [Fact]
    public Task CommentsAndDocs_MoveWithTheirMember() => VerifyFixAsync("""
        class C
        {
            /// <summary>Does M.</summary>
            public void M() { }

            // The value.
            [System.Obsolete]
            private int {|BRO1001:_x|};
        }
        """, """
        class C
        {
            // The value.
            [System.Obsolete]
            private int _x;

            /// <summary>Does M.</summary>
            public void M() { }
        }
        """);

    [Fact]
    public Task NestedTypes_AreFixedInOnePass() => VerifyFixAsync("""
        class Outer
        {
            class Inner
            {
                void M() { }

                int {|BRO1001:_y|};
            }

            int {|BRO1001:_x|};
        }
        """, """
        class Outer
        {
            int _x;

            class Inner
            {
                int _y;

                void M() { }
            }
        }
        """);

    [Fact]
    public Task InterfaceMembers_AreImplicitlyPublic() => VerifyFixAsync("""
        interface I
        {
            void M();

            int {|BRO1001:P|} { get; }
        }
        """, """
        interface I
        {
            int P { get; }

            void M();
        }
        """);

    [Fact]
    public Task Regions_AreLeftAlone() => VerifyNoDiagnosticsAsync("""
        class C
        {
            #region Methods
            public void M() { }
            #endregion

            private int _x;
        }
        """);

    [Fact]
    public Task CustomKindOrder_FromEditorConfig() => VerifyFixAsync("""
        class C
        {
            private int _x;

            private void {|BRO1001:M|}() { }
        }
        """, """
        class C
        {
            private void M() { }

            private int _x;
        }
        """, editorConfig: "stylebro_member_order = method, field\n");

    [Fact]
    public Task ConversionsAndOperators_ComeBeforeMethods() => VerifyFixAsync("""
        class C
        {
            public void M() { }

            public static C operator {|BRO1001:+|}(C a, C b) => a;

            public static implicit operator int(C c) => 0;
        }
        """, """
        class C
        {
            public static implicit operator int(C c) => 0;

            public static C operator +(C a, C b) => a;

            public void M() { }
        }
        """);

    [Fact]
    public Task InitializerReadingConstant_DoesNotBlockSorting() => VerifyFixAsync("""
        class C
        {
            private const string Default = "x";

            private string Name { get; set; } = Default;
            private readonly object {|BRO1001:_lock|} = new();
        }
        """, """
        class C
        {
            private const string Default = "x";

            private readonly object _lock = new();
            private string Name { get; set; } = Default;
        }
        """);

    [Fact]
    public Task StaticInitializerReadingAnotherField_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        public static class C
        {
            public static int A = 5;
            public static readonly int B = A + 1;
        }
        """);

    [Fact]
    public Task StaticInitializerReadingThroughMethod_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        class C
        {
            public static int P { get; } = Compute();

            private static int s_a = 5;

            private static int Compute() => s_a;
        }
        """);

    [Fact]
    public Task StaticInitializerCreatingTheType_IsLeftAlone() => VerifyNoDiagnosticsAsync("""
        using System.Collections.Generic;

        class C
        {
            private static readonly C s_instance = new();
            public static readonly List<int> Items = new();

            private C() { Items.Add(1); }
        }
        """);

    [Fact]
    public Task InstanceInitializersSharingStaticState_AreLeftAlone() => VerifyNoDiagnosticsAsync("""
        class C
        {
            private static int s_next;
            private int _a = s_next++;
            public int B = s_next++;
        }
        """);

    [Fact]
    public Task IndependentInitializers_AreStillSorted() => VerifyFixAsync("""
        class C
        {
            private static int s_a = 1;
            public static readonly int {|BRO1001:B|} = 2;
        }
        """, """
        class C
        {
            public static readonly int B = 2;
            private static int s_a = 1;
        }
        """);

    [Fact]
    public Task TypeofNameofAndForeignMembers_DoNotBlockSorting() => VerifyFixAsync("""
        class C
        {
            private static int s_count = 0;
            private static readonly int {|BRO1001:s_length|} = "".Length;
            public static readonly string Name = nameof(s_count) + typeof(C).Name;
            public static readonly int Length = 3;
        }
        """, """
        class C
        {
            public static readonly string Name = nameof(s_count) + typeof(C).Name;
            public static readonly int Length = 3;
            private static readonly int s_length = "".Length;
            private static int s_count = 0;
        }
        """);

    [Fact]
    public Task CommentedMemberMovedBelowCode_GetsABlankLineAbove() => VerifyFixAsync("""
        class C
        {
            // normal deserialization
            public string Name { get; set; }
            public {|BRO1001:C|}()
            {
            }
            public int _count;
        }
        """, """
        class C
        {
            public int _count;
            public C()
            {
            }

            // normal deserialization
            public string Name { get; set; }
        }
        """);

    [Fact]
    public Task StaticConstructor_ComesBeforeInstanceConstructors() => VerifyFixAsync("""
        class C
        {
            public C() { }

            static {|BRO1001:C|}() { }

            internal C(int x) { }

            private C(long x) { }
        }
        """, """
        class C
        {
            static C() { }

            public C() { }

            internal C(int x) { }

            private C(long x) { }
        }
        """);

    [Fact]
    public Task StaticConstructorFirst_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            static C() { }

            public C() { }

            private C(int x) { }
        }
        """);

    [Fact]
    public Task StaticFirstDisabled_NoDiagnostic() => VerifyNoDiagnosticsAsync("""
        class C
        {
            private int _a;
            private static int s_b;
        }
        """, editorConfig: "stylebro_member_static_first = false\n");
}
