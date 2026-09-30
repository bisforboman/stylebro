using StyleBro.Analyzers.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.FieldNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class FieldNamingTests
{
    private const string Underscore = "stylebro_private_field_naming = _camelCase";

    [Theory]
    [InlineData("Count", false, "count")]
    [InlineData("_count", false, "count")]
    [InlineData("count", false, null)]
    [InlineData("Count", true, "_count")]
    [InlineData("count", true, "_count")]
    [InlineData("__count", true, "_count")]
    [InlineData("_Count", true, "_count")]
    [InlineData("_count", true, null)]
    [InlineData("_", true, null)]
    [InlineData("m_count", false, null)]
    [InlineData("s_count", true, null)]
    [InlineData("M_Count", false, null)]
    public void NewName(string name, bool underscore, string? expected) =>
        Assert.Equal(expected, FieldNames.GetNewName(name, underscore ? FieldStyle.UnderscoreCamelCase : FieldStyle.CamelCase));

    [Fact]
    public Task CamelCase_RenamesAndQualifiesHiddenReferences() => VerifyFixAsync("""
        using System;

        class C
        {
            private int {|BRO1303:_count|};
            private readonly string {|BRO1303:Name|} = "c";
            private static int {|BRO1303:Instances|};
            private static readonly int Max = 10;
            private const int Min = 0;
            protected int _shared;
            public int visible;
            private event EventHandler Changed;

            /// <summary>Sets <see cref="_count"/>.</summary>
            public C(int count, string name)
            {
                _count = count;
                Instances++;
                Console.WriteLine(Name + name + nameof(_count));
            }

            public int Count => _count;

            public static int Total(int instances) => Instances + instances;

            public int Add(int value) => new Func<int, int>(count => count + _count)(value) + Max + Min;
        }
        """, """
        using System;

        class C
        {
            private int count;
            private readonly string name = "c";
            private static int instances;
            private static readonly int Max = 10;
            private const int Min = 0;
            protected int _shared;
            public int visible;
            private event EventHandler Changed;

            /// <summary>Sets <see cref="count"/>.</summary>
            public C(int count, string name)
            {
                this.count = count;
                instances++;
                Console.WriteLine(this.name + name + nameof(this.count));
            }

            public int Count => count;

            public static int Total(int instances) => C.instances + instances;

            public int Add(int value) => new Func<int, int>(count => count + this.count)(value) + Max + Min;
        }
        """);

    [Fact]
    public Task UnderscoreStyle_IsRead() => VerifyFixAsync("""
        class C
        {
            private int {|BRO1303:count|};
            private int {|BRO1303:Total|};
            private int _done;

            public int Sum() => count + Total + _done;
        }
        """, """
        class C
        {
            private int _count;
            private int _total;
            private int _done;

            public int Sum() => _count + _total + _done;
        }
        """, editorConfig: Underscore);

    [Fact]
    public Task PartialTypes_AreRenamedInEveryPart() => VerifyFixAsync(
        [
            """
            partial class C
            {
                private int {|BRO1303:_count|};
            }
            """,
            """
            partial class C
            {
                public int Get() => _count;
            }
            """,
        ],
        [
            """
            partial class C
            {
                private int count;
            }
            """,
            """
            partial class C
            {
                public int Get() => count;
            }
            """,
        ]);

    [Fact]
    public Task StructsAndObjectInitializers() => VerifyFixAsync("""
        struct S
        {
            private int {|BRO1303:_value|};

            public S Copy(int value) => new S { _value = value };
        }
        """, """
        struct S
        {
            private int value;

            public S Copy(int value) => new S { value = value };
        }
        """);

    [Fact]
    public Task UnsafeRenames_AreSkipped() => VerifyNoDiagnosticsAsync("""
        using System;
        using System.Reflection;

        class Base
        {
            protected int total;
        }

        class C : Base
        {
            private int _total;
            private int _count;
            private int Count;
            private int _reflected;
            private int _anonymous;
            private int _conditional;
            [NonSerialized]
            private int _attributed;

            public object M()
            {
                var field = typeof(C).GetField("_reflected", BindingFlags.NonPublic | BindingFlags.Instance);
        #if NEVER
                _conditional++;
        #endif
                return new { _anonymous, _attributed, field, Sum = _total + _count + Count };
            }
        }

        [Serializable]
        class Serialized
        {
            private int _value;

            public int Get() => _value;
        }
        """);

    [Fact]
    public Task ReflectionInOtherFiles_KeepsTheName() => VerifyNotFixedAsync(
        [
            """
            class C
            {
                private int {|BRO1303:_blockedUntil|};

                public int Get() => _blockedUntil;
            }
            """,
            """
            class Tests
            {
                object Read(C c) => typeof(C).GetField("_blockedUntil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(c);
            }
            """,
        ]);
}