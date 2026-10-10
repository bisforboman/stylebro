using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Naming;
using StyleBro.CodeFixes.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.FieldNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class FieldNamingTests
{
    private const string Underscore = "stylebro_private_field_naming = _camelCase";

    private const string CamelConstants = """
        dotnet_naming_rule.private_constants.symbols = private_constants
        dotnet_naming_rule.private_constants.style = camel
        dotnet_naming_rule.private_constants.severity = warning
        dotnet_naming_symbols.private_constants.applicable_kinds = field
        dotnet_naming_symbols.private_constants.applicable_accessibilities = private
        dotnet_naming_symbols.private_constants.required_modifiers = const
        dotnet_naming_rule.private_statics.symbols = private_statics
        dotnet_naming_rule.private_statics.style = underscore_camel
        dotnet_naming_rule.private_statics.severity = warning
        dotnet_naming_symbols.private_statics.applicable_kinds = field
        dotnet_naming_symbols.private_statics.applicable_accessibilities = private
        dotnet_naming_symbols.private_statics.required_modifiers = static, readonly
        dotnet_naming_style.camel.capitalization = camel_case
        dotnet_naming_style.underscore_camel.required_prefix = _
        dotnet_naming_style.underscore_camel.capitalization = camel_case
        """;

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
    [InlineData("_field", false, null)]
    [InlineData("Field", false, null)]
    [InlineData("Field", true, "_field")]
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
                Console.WriteLine(Name + name + _count);
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
                Console.WriteLine(this.name + name + this.count);
            }

            public int Count => count;

            public static int Total(int instances) => C.instances + instances;

            public int Add(int value) => new Func<int, int>(count => count + this.count)(value) + Max + Min;
        }
        """);

    [Fact]
    public Task UnderscoreStyle_IsRead_ForStaticFieldsToo() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:count|};
            private int {|BRO1303:Total|};
            private int _done;
            private static int {|BRO1303:instances|};
            private static readonly int Limit = 1;
            private const int Max = 2;

            public int Sum() => count + Total + _done + instances + Limit + Max;
        }
        """,
        """
        class C
        {
            private int _count;
            private int _total;
            private int _done;
            private static int _instances;
            private static readonly int Limit = 1;
            private const int Max = 2;

            public int Sum() => _count + _total + _done + _instances + Limit + Max;
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

    [Fact]
    public Task NamingRules_CanAskForCamelCasePrivateConstantsAndStaticReadonlyFields() => VerifyFixAsync(
        """
        public class C
        {
            public const int MaxCount = 3;
            private const int {|BRO1306:Limit|} = 2;
            private const int {|BRO1308:MIN_SIZE|} = 1;
            private const int fine = 4;
            internal static readonly string Shared = "";
            private static readonly string {|BRO1306:Cache|} = "";
            private static readonly string {|BRO1306:empty|} = "";
            private static readonly string _ready = "";
            private int count;

            public int Sum() => MaxCount + Limit + MIN_SIZE + fine + Shared.Length + Cache.Length + empty.Length + _ready.Length + count;
        }
        """,
        """
        public class C
        {
            public const int MaxCount = 3;
            private const int limit = 2;
            private const int minSize = 1;
            private const int fine = 4;
            internal static readonly string Shared = "";
            private static readonly string _cache = "";
            private static readonly string _empty = "";
            private static readonly string _ready = "";
            private int count;

            public int Sum() => MaxCount + limit + minSize + fine + Shared.Length + _cache.Length + _empty.Length + _ready.Length + count;
        }
        """,
        CamelConstants);

    [Fact]
    public Task StaticStyleKey_PascalCase_WinsOverTheNamingRules() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private const int Limit = 2;
            private static readonly string Cache = "";

            public int Sum() => Limit + Cache.Length;
        }
        """,
        CamelConstants + "\nstylebro_private_static_field_naming = PascalCase");

    [Fact]
    public Task StaticStyleKey_CamelCase_RenamesWithoutANamingRule() => VerifyFixAsync(
        """
        public class C
        {
            private const int {|BRO1306:Limit|} = 2;
            private static readonly string {|BRO1306:Cache|} = "";

            public int Sum() => Limit + Cache.Length;
        }
        """,
        """
        public class C
        {
            private const int limit = 2;
            private static readonly string cache = "";

            public int Sum() => limit + cache.Length;
        }
        """,
        "stylebro_private_static_field_naming = camelCase");

    [Fact]
    public Task NamingRuleForStatics_WithAnotherStyle_KeepsPascalCase() => VerifyFixAsync(
        """
        public class C
        {
            private const int {|BRO1306:limit|} = 2;
            private static readonly string {|BRO1306:cache|} = "";
            private static readonly string s_shared = "";

            public int Sum() => limit + cache.Length + s_shared.Length;
        }
        """,
        """
        public class C
        {
            private const int Limit = 2;
            private static readonly string Cache = "";
            private static readonly string s_shared = "";

            public int Sum() => Limit + Cache.Length + s_shared.Length;
        }
        """,
        """
        dotnet_naming_rule.private_fields.symbols = private_fields
        dotnet_naming_rule.private_fields.style = camel
        dotnet_naming_rule.private_fields.severity = warning
        dotnet_naming_symbols.private_fields.applicable_kinds = field
        dotnet_naming_symbols.private_fields.applicable_accessibilities = private
        dotnet_naming_rule.statics.symbols = statics
        dotnet_naming_rule.statics.style = s_camel
        dotnet_naming_rule.statics.severity = warning
        dotnet_naming_symbols.statics.applicable_kinds = field
        dotnet_naming_symbols.statics.applicable_accessibilities = private
        dotnet_naming_symbols.statics.required_modifiers = static
        dotnet_naming_rule.constants.symbols = constants
        dotnet_naming_rule.constants.style = pascal
        dotnet_naming_rule.constants.severity = warning
        dotnet_naming_symbols.constants.applicable_kinds = field
        dotnet_naming_symbols.constants.required_modifiers = const
        dotnet_naming_rule.camel_constants.symbols = constants
        dotnet_naming_rule.camel_constants.style = camel
        dotnet_naming_rule.camel_constants.severity = none
        dotnet_naming_style.camel.capitalization = camel_case
        dotnet_naming_style.pascal.capitalization = pascal_case
        dotnet_naming_style.s_camel.required_prefix = s_
        dotnet_naming_style.s_camel.capitalization = camel_case
        """);

    [Fact]
    public Task NamingRuleWithMoreModifiers_WinsWithoutPriority() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private static readonly string Cache = "";

            public int Get() => Cache.Length;
        }
        """,
        """
        dotnet_naming_rule.a_statics.symbols = statics
        dotnet_naming_rule.a_statics.style = camel
        dotnet_naming_rule.b_static_readonly.symbols = static_readonly
        dotnet_naming_rule.b_static_readonly.style = pascal
        dotnet_naming_symbols.statics.applicable_kinds = field
        dotnet_naming_symbols.statics.required_modifiers = static
        dotnet_naming_symbols.static_readonly.applicable_kinds = field
        dotnet_naming_symbols.static_readonly.required_modifiers = static, readonly
        dotnet_naming_style.camel.capitalization = camel_case
        dotnet_naming_style.pascal.capitalization = pascal_case
        """);

    [Fact]
    public Task NamingRulePriority_PicksTheConstantStyle() => VerifyFixAsync(
        """
        public class C
        {
            private const int {|BRO1306:Limit|} = 2;

            public int Get() => Limit;
        }
        """,
        """
        public class C
        {
            private const int limit = 2;

            public int Get() => limit;
        }
        """,
        """
        dotnet_naming_rule.a_pascal.symbols = constants
        dotnet_naming_rule.a_pascal.style = pascal
        dotnet_naming_rule.a_pascal.priority = 2
        dotnet_naming_rule.b_camel.symbols = constants
        dotnet_naming_rule.b_camel.style = camel
        dotnet_naming_rule.b_camel.priority = 1
        dotnet_naming_symbols.constants.applicable_kinds = field
        dotnet_naming_symbols.constants.applicable_accessibilities = private
        dotnet_naming_symbols.constants.required_modifiers = const
        dotnet_naming_style.camel.capitalization = camel_case
        dotnet_naming_style.pascal.capitalization = pascal_case
        """);

    [Fact]
    public Task NothingIsRenamedToField_ACSharp14KeywordInAccessors() => VerifyNoDiagnosticsAsync(
        """
        public class A
        {
            private int _field;

            public int Value { get => _field; set => _field = value; }
        }

        public class B
        {
            private int Field;
            private int m_field;
            private int field_;
            protected int _field;

            public int Value => Field + m_field + field_ + _field;
        }

        public class C
        {
            protected readonly int _field;
            private int m_Field;

            public int Value => _field + m_Field;
        }
        """);

    [Fact]
    public Task UnderscoreStyle_StillGivesUnderscoreField() => VerifyFixAsync(
        """
        public class C
        {
            private int {|BRO1303:Field|};

            public int Value => Field;
        }
        """,
        """
        public class C
        {
            private int _field;

            public int Value => _field;
        }
        """,
        Underscore);

    [Fact]
    public Task FieldsOfATypeWithAGeneratedPart_KeepTheirNames() => VerifyNoDiagnosticsAsync(
        ("/0/List.razor.cs", """
            public partial class List
            {
                private int _count;

                public int Get() => _count;
            }
            """),
        ("/0/List.razor.g.cs", """
            public partial class List
            {
                public int Render() => _count;
            }
            """));

    [Fact]
    public Task AReferenceInGeneratedCode_KeepsTheName() => VerifyNotFixedAsync(
        ("/0/Test0.cs", """
            public class Dto
            {
                public int {|BRO1306:total|};
            }
            """),
        ("/0/Page.g.cs", """
            public class Page
            {
                public int Read(Dto dto) => dto.total;
            }
            """));

    [Fact]
    public Task AReferenceInSourceGeneratorOutput_KeepsTheName() => new GeneratorTest<ReferenceGenerator>("""
        public class Dto
        {
            public int {|BRO1306:total|};
        }
        """).RunAsync();

    [Fact]
    public Task TheNameInSourceGeneratorStrings_KeepsTheName() => new GeneratorTest<AccessorGenerator>("""
        public class Dto
        {
            private int {|BRO1303:_intValue|};

            public int Get() => _intValue;
        }
        """).RunAsync();

    /// <summary>Like Mapperly, which reaches private fields by name: 'UnsafeAccessor(UnsafeAccessorKind.Field, Name = "intValue")'.</summary>
    private sealed class AccessorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            context.RegisterPostInitializationOutput(c => c.AddSource("Accessor.cs", """
                static class Accessor
                {
                    public const string Name = "_intValue";
                }
                """));
    }

    /// <summary>Generated code that uses a field (hint name without '.g.cs' and no header: only the document kind tells).</summary>
    private sealed class ReferenceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            context.RegisterPostInitializationOutput(c => c.AddSource("Reader.cs", """
                static class Reader
                {
                    public static int Read(Dto dto) => dto.total;
                }
                """));
    }

    /// <summary>
    /// The diagnostic in the source is reported, and the fix leaves it (the generator's output can't be renamed). The
    /// generator is an analyzer reference of the project, as in a real workspace, so its output is source-generated documents.
    /// </summary>
    private sealed class GeneratorTest<TGenerator> : CSharpCodeFixTest<FieldNamingAnalyzer, CamelCaseNamingCodeFixProvider, DefaultVerifier>
        where TGenerator : IIncrementalGenerator, new()
    {
        public GeneratorTest(string source)
        {
            TestState.Sources.Add(source);
            TestState.AnalyzerConfigFiles.Add(PublicApi.RenameEverything);
            FixedState.Sources.Add(source);
            NumberOfIncrementalIterations = 0;
            NumberOfFixAllIterations = 0;
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck;
            SolutionTransforms.Add((solution, projectId) => solution.AddAnalyzerReference(projectId, new GeneratorReference(new TGenerator())));
        }
    }

    private sealed class GeneratorReference(IIncrementalGenerator generator) : AnalyzerReference
    {
        public override string FullPath => string.Empty;

        public override object Id => generator.GetType();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => ImmutableArray<DiagnosticAnalyzer>.Empty;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => ImmutableArray<DiagnosticAnalyzer>.Empty;

        public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => ImmutableArray.Create(generator.AsSourceGenerator());
    }

    [Fact]
    public Task PublicApi_IsLeftAloneByDefault() => VerifyFixAsync(
        """
        public class Limits
        {
            public const int MAX_COUNT = 1;
            protected int _shared;
            internal static int {|BRO1308:Min_Count|};
            private int {|BRO1303:_count|};

            public int Get() => _count + _shared + Min_Count;
        }

        internal class Hidden
        {
            public const int {|BRO1308:MAX_COUNT|} = 1;
        }
        """,
        """
        public class Limits
        {
            public const int MAX_COUNT = 1;
            protected int _shared;
            internal static int MinCount;
            private int count;

            public int Get() => count + _shared + MinCount;
        }

        internal class Hidden
        {
            public const int MaxCount = 1;
        }
        """,
        "stylebro_rename_public_api = false");

    // Ocelot: 'X_RateLimit_Limit = nameof(X_RateLimit_Limit).Replace(...)' is an HTTP header name, and a test read a field
    // with 'GetField(nameof(_tracer))'.
    [Fact]
    public Task NamesInNameof_KeepTheirName() => VerifyNoDiagnosticsAsync(
        """
        internal static class Headers
        {
            public static readonly string X_RateLimit_Limit = nameof(X_RateLimit_Limit).Replace('_', '-');
        }

        class C
        {
            private object _tracer = new object();

            object Read(C c) => typeof(C).GetField(nameof(C._tracer), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(c)!;
        }
        """);

    [Fact]
    public Task NameofInAnotherType_KeepsTheName() => VerifyNotFixedAsync(
        [
            """
            class C
            {
                internal static int {|BRO1308:Max_Count|} = 1;
            }
            """,
            """
            class D
            {
                string Name => nameof(C.Max_Count);
            }
            """,
        ]);
}
