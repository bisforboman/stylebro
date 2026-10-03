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
    public Task CamelCase_RenamesAndQualifiesHiddenReferences() => VerifyFixAsync(
        """
        using System;

        class C
        {
            private int {|BRO1303:_count|};
            private readonly string {|BRO1303:Name|} = "c";
            private static int {|BRO1303:Instances|};
            private static readonly int Max = 10;
            private const int Min = 0;
            protected int {|BRO1303:_shared|};
            public int Visible;
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
        """,
        """
        using System;

        class C
        {
            private int count;
            private readonly string name = "c";
            private static int instances;
            private static readonly int Max = 10;
            private const int Min = 0;
            protected int shared;
            public int Visible;
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
    public Task UnderscoreStyle_IsRead() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:count|};
            private int {|BRO1303:Total|};
            private int _done;

            public int Sum() => count + Total + _done;
        }
        """,
        """
        class C
        {
            private int _count;
            private int _total;
            private int _done;

            public int Sum() => _count + _total + _done;
        }
        """,
        editorConfig: Underscore);

    [Fact]
    public Task NamingRuleForPrivateFields_PicksTheStyle() => VerifyFixAsync(
        """
        public class C
        {
            private int {|BRO1303:count|};
            private static int s_total;

            public int Next() => ++count + s_total;
        }
        """,
        """
        public class C
        {
            private int _count;
            private static int s_total;

            public int Next() => ++_count + s_total;
        }
        """,
        editorConfig: """
            dotnet_naming_rule.private_fields.symbols = private_fields
            dotnet_naming_rule.private_fields.style = underscore_camel
            dotnet_naming_rule.private_fields.severity = warning
            dotnet_naming_rule.private_fields.priority = 2
            dotnet_naming_symbols.private_fields.applicable_kinds = field
            dotnet_naming_symbols.private_fields.applicable_accessibilities = private
            dotnet_naming_style.underscore_camel.required_prefix = _
            dotnet_naming_style.underscore_camel.capitalization = camel_case
            dotnet_naming_rule.static_fields.symbols = static_fields
            dotnet_naming_rule.static_fields.style = s_camel
            dotnet_naming_rule.static_fields.severity = warning
            dotnet_naming_rule.static_fields.priority = 1
            dotnet_naming_symbols.static_fields.applicable_kinds = field
            dotnet_naming_symbols.static_fields.applicable_accessibilities = private
            dotnet_naming_symbols.static_fields.required_modifiers = static
            dotnet_naming_style.s_camel.required_prefix = s_
            dotnet_naming_style.s_camel.capitalization = camel_case
            """);

    [Fact]
    public Task ProtectedField_IsRenamed_WhenItsNameIsOnlyAWordInAnotherTypesString() => VerifyFixAsync(
        """
        public class C
        {
            protected int {|BRO1303:Total|};

            public int Next() => ++Total;
        }

        public class Report
        {
            public string Title => "Total of the month";
        }
        """,
        """
        public class C
        {
            protected int total;

            public int Next() => ++total;
        }

        public class Report
        {
            public string Title => "Total of the month";
        }
        """);

    [Fact]
    public Task NamingRuleWithRequiredModifiers_DoesNotPickThePrivateFieldStyle() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private int count;

            public int Next() => ++count;
        }
        """,
        """
        dotnet_naming_rule.readonly_fields.symbols = readonly_fields
        dotnet_naming_rule.readonly_fields.style = underscore_camel
        dotnet_naming_rule.readonly_fields.severity = warning
        dotnet_naming_symbols.readonly_fields.applicable_kinds = field
        dotnet_naming_symbols.readonly_fields.applicable_accessibilities = private
        dotnet_naming_symbols.readonly_fields.required_modifiers = readonly
        dotnet_naming_style.underscore_camel.required_prefix = _
        dotnet_naming_style.underscore_camel.capitalization = camel_case
        """);

    [Fact]
    public Task StyleBroKey_WinsOverANamingRule() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private int count;

            public int Next() => ++count;
        }
        """,
        """
        stylebro_private_field_naming = camelCase
        dotnet_naming_rule.private_fields.symbols = private_fields
        dotnet_naming_rule.private_fields.style = underscore_camel
        dotnet_naming_symbols.private_fields.applicable_kinds = field
        dotnet_naming_symbols.private_fields.applicable_accessibilities = private
        dotnet_naming_style.underscore_camel.required_prefix = _
        dotnet_naming_style.underscore_camel.capitalization = camel_case
        """);

    [Fact]
    public Task ProtectedFields_FollowStyleCop() => VerifyFixAsync(
        """
        public class C
        {
            protected int {|BRO1303:Count|};
            protected int {|BRO1303:_total|};
            protected readonly int limit = 1;
            protected readonly int {|BRO1303:_size|} = 2;
            private protected int {|BRO1303:Level|};
            protected int fine;
            protected readonly int Ready = 3;
        }
        """,
        """
        public class C
        {
            protected int count;
            protected int total;
            protected readonly int limit = 1;
            protected readonly int size = 2;
            private protected int level;
            protected int fine;
            protected readonly int Ready = 3;
        }
        """);

    [Fact]
    public Task ProtectedFields_KeepTheirUnderscore_InTheUnderscoreStyle() => VerifyFixAsync(
        """
        public class C
        {
            protected int {|BRO1303:_Count|};
            protected int {|BRO1303:Total|};
            protected readonly int _logger = 1;
            protected readonly int limit = 2;
            protected int fine;
            protected int _alsoFine;
        }
        """,
        """
        public class C
        {
            protected int _count;
            protected int total;
            protected readonly int _logger = 1;
            protected readonly int limit = 2;
            protected int fine;
            protected int _alsoFine;
        }
        """,
        editorConfig: Underscore);

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
    public Task StructsAndObjectInitializers() => VerifyFixAsync(
        """
        struct S
        {
            private int {|BRO1303:_value|};

            public S Copy(int value) => new S { _value = value };
        }
        """,
        """
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

        public class Dto
        {
            public int total;

            public const string Example = "{\"total\": 1}";
        }

        class JsonObjectAttribute : Attribute
        {
        }

        [JsonObject]
        class FieldsSerialized
        {
            private int _stored;

            public int Get() => _stored;
        }

        [System.Diagnostics.DebuggerDisplay("{_shown}")]
        class Displayed
        {
            private int _shown;

            public int Get() => _shown;
        }

        class Paired
        {
            private int _items;

            public bool ShouldSerialize_items() => _items > 0;
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

    [Fact]
    public Task PascalCaseFields_AreRenamed() => VerifyFixAsync(
        """
        public class C
        {
            public const int {|BRO1306:maxCount|} = 10;
            private const int {|BRO1306:_minCount|} = 0;
            public static readonly string {|BRO1306:defaultName|} = "c";
            private static readonly object {|BRO1306:syncRoot|} = new object();
            public int {|BRO1306:total|};
            internal int {|BRO1306:shared|};
            protected internal readonly int {|BRO1306:limit|} = 1;
            protected int kept;

            public enum Kind
            {
                small,
            }

            public int Sum(int maxCount) => C.maxCount + maxCount + _minCount + total + shared + limit + kept;

            public object Lock() => syncRoot;

            public string Name() => defaultName;
        }

        public class User
        {
            public int Read(C c) => c.total + C.maxCount;
        }
        """,
        """
        public class C
        {
            public const int MaxCount = 10;
            private const int MinCount = 0;
            public static readonly string DefaultName = "c";
            private static readonly object SyncRoot = new object();
            public int Total;
            internal int Shared;
            protected internal readonly int Limit = 1;
            protected int kept;

            public enum Kind
            {
                small,
            }

            public int Sum(int maxCount) => C.MaxCount + maxCount + MinCount + Total + Shared + Limit + kept;

            public object Lock() => SyncRoot;

            public string Name() => DefaultName;
        }

        public class User
        {
            public int Read(C c) => c.Total + C.MaxCount;
        }
        """);

    [Fact]
    public Task PublicFieldsNamedInStrings_KeepTheName() => VerifyNotFixedAsync(
        [
            """
            public class Dto
            {
                public int {|BRO1306:total|};
            }
            """,
            """
            class Tests
            {
                const string Json = "{\"total\": 3}";
            }
            """,
        ]);

    [Fact]
    public Task HidingInDerivedTypes_KeepsTheName() => VerifyNotFixedAsync(
        [
            """
            public class Base
            {
                public static readonly int {|BRO1306:limit|} = 1;
            }
            """,
            """
            public class Derived : Base
            {
                public int Limit => 2;

                public int Get() => limit;
            }
            """,
        ]);

    [Fact]
    public Task PrefixesAndUnderscores_GetTheCompleteName() => VerifyFixAsync(
        """
        using System;

        public class C
        {
            private int {|BRO1307:m_member|};
            private static int {|BRO1307:s_count|};
            [ThreadStatic]
            private static int t_depth;
            private int {|BRO1307:m_with_more|};
            private int {|BRO1307:m_Upper|};
            private int {|BRO1308:with_underscore|};
            private int {|BRO1308:x_other|};
            private int {|BRO1308:RETRY_COUNT|};
            private int {|BRO1308:trailing_|};
            private int {|BRO1308:two__underscores|};
            private const int {|BRO1308:MAX_VALUE|} = 1;
            private static readonly int {|BRO1308:Default_Value|} = 1;
            public int {|BRO1308:Public_Field|};
            protected int {|BRO1308:protected_field|};
            private int m_;
            private static int s_static;
            private static readonly object Int32_0 = 0;

            public int Use() => m_member + s_count + t_depth + m_with_more + m_Upper + with_underscore + x_other
                + RETRY_COUNT + trailing_ + two__underscores + MAX_VALUE + Default_Value + Public_Field + protected_field + m_
                + s_static;
        }
        """,
        """
        using System;

        public class C
        {
            private int member;
            private static int count;
            [ThreadStatic]
            private static int t_depth;
            private int withMore;
            private int upper;
            private int withUnderscore;
            private int xOther;
            private int retryCount;
            private int trailing;
            private int twoUnderscores;
            private const int MaxValue = 1;
            private static readonly int DefaultValue = 1;
            public int PublicField;
            protected int protectedField;
            private int m_;
            private static int s_static;
            private static readonly object Int32_0 = 0;

            public int Use() => member + count + t_depth + withMore + upper + withUnderscore + xOther
                + retryCount + trailing + twoUnderscores + MaxValue + DefaultValue + PublicField + protectedField + m_
                + s_static;
        }
        """);

    [Fact]
    public Task PrefixesAndUnderscores_FollowTheUnderscoreStyle() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1307:m_member|};
            private int {|BRO1308:with_underscore|};
            private const int {|BRO1308:MAX_VALUE|} = 1;

            public int Use() => m_member + with_underscore + MAX_VALUE;
        }
        """,
        """
        class C
        {
            private int _member;
            private int _withUnderscore;
            private const int MaxValue = 1;

            public int Use() => _member + _withUnderscore + MaxValue;
        }
        """,
        editorConfig: Underscore);
}
