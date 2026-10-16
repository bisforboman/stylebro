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

    // Like StyleCop: its field naming rules skip '*NativeMethods' classes (also types nested in one), except SA1311.
    [Fact]
    public Task NativeMethods_KeepFieldNames_ExceptLowerCaseStaticReadonly() => VerifyFixAsync(
        """
        internal static class SafeNativeMethods
        {
            internal const int max_path = 260;
            internal static int dwFlags;
            internal static readonly int {|BRO1306:invalid|} = -1;
            internal static readonly int s_empty = 0;
            private static int Handle;
            private static int m_count;
            private static int a_b;

            internal struct POINT
            {
                public int x;
                private int Y;
                internal int _z;
            }

            internal static int Sum(POINT p) => max_path + dwFlags + invalid + Handle + m_count + a_b;
        }
        """,
        """
        internal static class SafeNativeMethods
        {
            internal const int max_path = 260;
            internal static int dwFlags;
            internal static readonly int Invalid = -1;
            internal static readonly int s_empty = 0;
            private static int Handle;
            private static int m_count;
            private static int a_b;

            internal struct POINT
            {
                public int x;
                private int Y;
                internal int _z;
            }

            internal static int Sum(POINT p) => max_path + dwFlags + Invalid + Handle + m_count + a_b;
        }
        """);

    // Owner's decision (docs/decisions.md): interop names stay. Flow.Launcher got 'S_OK' -> 'SOk', 'IID_IContextMenu' ->
    // '_iidIContextMenu' and 'cbSize' -> 'CbSize'.
    [Fact]
    public Task InteropNames_AreLeftAlone() => VerifyFixAsync(
        """
        using System;
        using System.Runtime.InteropServices;

        public class Menu
        {
            private const int S_OK = 0;
            internal const int MAX_PATH = 260;
            private static readonly Guid IID_IContextMenu = Guid.Empty;
            private static readonly int WINDOW_CLASS_2 = 2;
            private const int {|BRO1308:Max_Count|} = 3;
            private static int {|BRO1308:LOCAL_STATE|};

            public int Sum() => S_OK + MAX_PATH + IID_IContextMenu.GetHashCode() + WINDOW_CLASS_2 + Max_Count + LOCAL_STATE;
        }

        public class Shell
        {
            private int _handle;

            public int Handle => _handle;

            [DllImport("user32.dll")]
            private static extern int GetSubMenu(IntPtr menu, int position);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MENUITEMINFO
        {
            public int cbSize;
            public IntPtr hSubMenu;
        }

        internal class Outer
        {
            [DllImport("user32.dll")]
            private static extern int GetMenu();

            internal struct POINT
            {
                public int x;
            }
        }
        """,
        """
        using System;
        using System.Runtime.InteropServices;

        public class Menu
        {
            private const int S_OK = 0;
            internal const int MAX_PATH = 260;
            private static readonly Guid IID_IContextMenu = Guid.Empty;
            private static readonly int WINDOW_CLASS_2 = 2;
            private const int MaxCount = 3;
            private static int localState;

            public int Sum() => S_OK + MAX_PATH + IID_IContextMenu.GetHashCode() + WINDOW_CLASS_2 + MaxCount + localState;
        }

        public class Shell
        {
            private int _handle;

            public int Handle => _handle;

            [DllImport("user32.dll")]
            private static extern int GetSubMenu(IntPtr menu, int position);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MENUITEMINFO
        {
            public int cbSize;
            public IntPtr hSubMenu;
        }

        internal class Outer
        {
            [DllImport("user32.dll")]
            private static extern int GetMenu();

            internal struct POINT
            {
                public int x;
            }
        }
        """);

    // akka.net: a base's private member with the new name can't clash (the build, which sees the base through a reference
    // assembly without private members, reported it; 'dotnet format', which loads the base's source, didn't). A type nested
    // in the base sees it, so that one keeps its name.
    [Fact]
    public Task PrivateMemberOfABase_IsNoClash() => VerifyFixAsync(
        """
        public class Base
        {
            private readonly object _log = new object();

            public override string ToString() => _log.ToString()!;

            public class Nested : Base
            {
                private readonly object Log = new object();

                public object Get() => Log;
            }
        }

        public class Derived : Base
        {
            private readonly object {|BRO1303:Log|} = new object();

            public object Get() => Log;
        }
        """,
        """
        public class Base
        {
            private readonly object _log = new object();

            public override string ToString() => _log.ToString()!;

            public class Nested : Base
            {
                private readonly object Log = new object();

                public object Get() => Log;
            }
        }

        public class Derived : Base
        {
            private readonly object _log = new object();

            public object Get() => _log;
        }
        """,
        editorConfig: Underscore);

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

    // Fluid: 'this.pet = pet' became 'this._pet = pet'. The 'this.' was only there to step around the parameter; once the
    // field is '_pet', it isn't needed. A 'this.' the code didn't need, or one a parameter still needs, stays.
    [Fact]
    public Task UnderscoreStyle_DropsTheThisTheRenameMadeRedundant() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:pet|};
            private int {|BRO1303:other|};
            private int {|BRO1303:count|};

            public C(int pet, int _count)
            {
                this.pet = pet;
                this.other = this.pet;
                this.count = _count;
                System.Action<int> set = _pet => this.pet = pet + _pet;
            }
        }
        """,
        """
        class C
        {
            private int _pet;
            private int _other;
            private int _count;

            public C(int pet, int _count)
            {
                _pet = pet;
                this._other = _pet;
                this._count = _count;
                System.Action<int> set = _pet => this._pet = pet + _pet;
            }
        }
        """,
        editorConfig: Underscore);

    [Fact]
    public Task UnderscoreStyle_KeepsThis_WhenTheRepoAsksForIt() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:pet|};

            public C(int pet)
            {
                this.pet = pet;
            }
        }
        """,
        """
        class C
        {
            private int _pet;

            public C(int pet)
            {
                this._pet = pet;
            }
        }
        """,
        editorConfig: Underscore + "\ndotnet_style_qualification_for_field = true:warning");

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

    // BRO1307 leaves a prefix a naming rule requires, also on a non-private field (the private ones are left earlier,
    // by the naming rule's style StyleBro doesn't write).
    [Fact]
    public Task InternalStaticField_KeepsThePrefixANamingRuleRequires() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            internal static int s_total;

            public int Next() => ++s_total;
        }
        """,
        """
        dotnet_naming_rule.static_fields.symbols = static_fields
        dotnet_naming_rule.static_fields.style = s_camel
        dotnet_naming_rule.static_fields.severity = warning
        dotnet_naming_symbols.static_fields.applicable_kinds = field
        dotnet_naming_symbols.static_fields.applicable_accessibilities = internal
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
            private const int {|BRO1308:Max_Value|} = 1;
            private static readonly int {|BRO1308:Default_Value|} = 1;
            public int {|BRO1308:Public_Field|};
            protected int {|BRO1308:protected_field|};
            private int m_;
            private static int s_static;
            private static readonly object Int32_0 = 0;

            public int Use() => m_member + s_count + t_depth + m_with_more + m_Upper + with_underscore + x_other
                + RETRY_COUNT + trailing_ + two__underscores + Max_Value + Default_Value + Public_Field + protected_field + m_
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
            private const int {|BRO1308:Max_Value|} = 1;

            public int Use() => m_member + with_underscore + Max_Value;
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
            private const int {|BRO1308:Min_Size|} = 1;
            private const int fine = 4;
            internal static readonly string Shared = "";
            private static readonly string {|BRO1306:Cache|} = "";
            private static readonly string {|BRO1306:empty|} = "";
            private static readonly string _ready = "";
            private int count;

            public int Sum() => MaxCount + Limit + Min_Size + fine + Shared.Length + Cache.Length + empty.Length + _ready.Length + count;
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
    public Task MutableStaticKey_Underscore_OnlyForStaticFields() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:_count|};
            private int done;
            private static int {|BRO1303:instances|};
            private static int {|BRO1307:m_old|};
            private static int _fine;
            private static readonly int Limit = 1;

            public int Sum() => _count + done + instances + m_old + _fine + Limit;
        }
        """,
        """
        class C
        {
            private int count;
            private int done;
            private static int _instances;
            private static int _old;
            private static int _fine;
            private static readonly int Limit = 1;

            public int Sum() => count + done + _instances + _old + _fine + Limit;
        }
        """,
        "stylebro_private_mutable_static_field_naming = _camelCase");

    [Fact]
    public Task MutableStaticKey_CamelCase_WithUnderscoreInstanceFields() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:count|};
            private static int {|BRO1303:_instances|};
            private static int fine;

            public int Sum() => count + _instances + fine;
        }
        """,
        """
        class C
        {
            private int _count;
            private static int instances;
            private static int fine;

            public int Sum() => _count + instances + fine;
        }
        """,
        Underscore + "\nstylebro_private_mutable_static_field_naming = camelCase");

    [Fact]
    public Task NamingRuleRequiringOnlyStatic_SetsTheMutableStaticStyle() => VerifyFixAsync(
        """
        class C
        {
            private int {|BRO1303:count|};
            private static int {|BRO1303:_instances|};
            private static readonly int {|BRO1306:Limit|} = 1;

            public int Sum() => count + _instances + Limit;
        }
        """,
        """
        class C
        {
            private int _count;
            private static int instances;
            private static readonly int limit = 1;

            public int Sum() => _count + instances + limit;
        }
        """,
        Underscore + """

        dotnet_naming_rule.statics.symbols = statics
        dotnet_naming_rule.statics.style = camel
        dotnet_naming_rule.statics.severity = warning
        dotnet_naming_symbols.statics.applicable_kinds = field
        dotnet_naming_symbols.statics.applicable_accessibilities = private
        dotnet_naming_symbols.statics.required_modifiers = static
        dotnet_naming_style.camel.capitalization = camel_case
        """);

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
    public Task AllowUnderscore_KeepsPascalFieldsStartingWithUnderscore_LikeStyleCopWithoutSA1309() => VerifyFixAsync(
        """
        public class C
        {
            private const int _limit = 2;
            private static readonly string _cache = "";
            private static readonly string _my_value = "";
            internal static readonly string _shared = "";
            private static readonly string {|BRO1306:lower|} = "";

            public int Sum() => _limit + _cache.Length + _my_value.Length + _shared.Length + lower.Length;
        }
        """,
        """
        public class C
        {
            private const int _limit = 2;
            private static readonly string _cache = "";
            private static readonly string _my_value = "";
            internal static readonly string _shared = "";
            private static readonly string Lower = "";

            public int Sum() => _limit + _cache.Length + _my_value.Length + _shared.Length + Lower.Length;
        }
        """,
        """
        stylebro_private_static_field_naming = PascalCase
        stylebro_allow_leading_underscore_in_pascal_fields = true
        """);

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

    // A naming rule for statics that asks for a style StyleBro doesn't write ('s_') leaves their names (before, 'cache'
    // became 'Cache', against the rule); constants follow their own rule.
    [Fact]
    public Task NamingRuleForStatics_WithAnotherStyle_LeavesThem() => VerifyFixAsync(
        """
        public class C
        {
            private const int {|BRO1306:limit|} = 2;
            private static readonly string cache = "";
            private static readonly string s_shared = "";

            public int Sum() => limit + cache.Length + s_shared.Length;
        }
        """,
        """
        public class C
        {
            private const int Limit = 2;
            private static readonly string cache = "";
            private static readonly string s_shared = "";

            public int Sum() => Limit + cache.Length + s_shared.Length;
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

    // sourcegit: its rule wants 's_' on private static fields, readonly ones included. StyleBro doesn't write that style,
    // so the names stay (BRO1306 renamed '_culture' to 'Culture', BRO1303 'count' to '_count').
    [Fact]
    public Task NamingRuleWithAStyleStyleBroDoesNotWrite_LeavesStaticFields() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private static readonly string _culture = "";
            private static readonly string s_format = "";
            private static int count;
            private int _value;

            public int Get() => _culture.Length + s_format.Length + count + _value;
        }
        """,
        """
        dotnet_naming_rule.private_static_fields_should_have_prefix.severity = suggestion
        dotnet_naming_rule.private_static_fields_should_have_prefix.symbols = private_static_fields
        dotnet_naming_rule.private_static_fields_should_have_prefix.style = private_static_prefix_style
        dotnet_naming_symbols.private_static_fields.applicable_kinds = field
        dotnet_naming_symbols.private_static_fields.required_modifiers = static
        dotnet_naming_symbols.private_static_fields.applicable_accessibilities = private
        dotnet_naming_style.private_static_prefix_style.required_prefix = s_
        dotnet_naming_style.private_static_prefix_style.capitalization = camel_case
        dotnet_naming_rule.camel_case_for_private_internal_fields.severity = suggestion
        dotnet_naming_rule.camel_case_for_private_internal_fields.symbols = private_internal_fields
        dotnet_naming_rule.camel_case_for_private_internal_fields.style = camel_case_underscore_style
        dotnet_naming_symbols.private_internal_fields.applicable_kinds = field
        dotnet_naming_symbols.private_internal_fields.applicable_accessibilities = private, internal
        dotnet_naming_style.camel_case_underscore_style.required_prefix = _
        dotnet_naming_style.camel_case_underscore_style.capitalization = camel_case
        """);

    // sourcegit: 'Text' would be the name of its own class (CS0542).
    [Fact]
    public Task AFieldNamedLikeItsType_IsNotRenamed() => VerifyNoDiagnosticsAsync(
        """
        public class Engine
        {
            internal class Text
            {
                public string text = string.Empty;
            }
        }
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

    // YamlDotNet's naming rules, exactly: camelCase instance fields of every accessibility but public, PascalCase for the
    // rest. The readonly rule has no severity, so Roslyn ignores it (internal readonly fields are camelCase too).
    private const string YamlDotNetNamingRules = """
        dotnet_naming_rule.non_private_static_fields_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.non_private_static_fields_should_be_pascal_case.symbols = non_private_static_fields
        dotnet_naming_rule.non_private_static_fields_should_be_pascal_case.style = non_private_static_field_style
        dotnet_naming_symbols.non_private_static_fields.applicable_kinds = field
        dotnet_naming_symbols.non_private_static_fields.applicable_accessibilities = public, protected, internal, protected_internal, private_protected
        dotnet_naming_symbols.non_private_static_fields.required_modifiers = static
        dotnet_naming_style.non_private_static_field_style.capitalization = pascal_case
        dotnet_naming_rule.non_private_readonly_fields_should_be_pascal_case.symbols = non_private_readonly_fields
        dotnet_naming_rule.non_private_readonly_fields_should_be_pascal_case.style = non_private_readonly_field_style
        dotnet_naming_symbols.non_private_readonly_fields.applicable_kinds = field
        dotnet_naming_symbols.non_private_readonly_fields.applicable_accessibilities = public, protected, internal, protected_internal, private_protected
        dotnet_naming_symbols.non_private_readonly_fields.required_modifiers = readonly
        dotnet_naming_style.non_private_readonly_field_style.capitalization = pascal_case
        dotnet_naming_rule.constants_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.constants_should_be_pascal_case.symbols = constants
        dotnet_naming_rule.constants_should_be_pascal_case.style = constant_style
        dotnet_naming_symbols.constants.applicable_kinds = field, local
        dotnet_naming_symbols.constants.required_modifiers = const
        dotnet_naming_style.constant_style.capitalization = pascal_case
        dotnet_naming_rule.static_fields_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.static_fields_should_be_camel_case.symbols = static_fields
        dotnet_naming_rule.static_fields_should_be_camel_case.style = static_field_style
        dotnet_naming_symbols.static_fields.applicable_kinds = field
        dotnet_naming_symbols.static_fields.required_modifiers = static
        dotnet_naming_style.static_field_style.capitalization = pascal_case
        dotnet_naming_rule.instance_fields_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.instance_fields_should_be_camel_case.symbols = instance_fields
        dotnet_naming_rule.instance_fields_should_be_camel_case.style = instance_field_style
        dotnet_naming_symbols.instance_fields.applicable_kinds = field
        dotnet_naming_symbols.instance_fields.applicable_accessibilities = protected, internal, protected_internal, private_protected, private
        dotnet_naming_style.instance_field_style.capitalization = camel_case
        dotnet_naming_rule.locals_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.locals_should_be_camel_case.symbols = locals_and_parameters
        dotnet_naming_rule.locals_should_be_camel_case.style = camel_case_style
        dotnet_naming_symbols.locals_and_parameters.applicable_kinds = parameter, local
        dotnet_naming_style.camel_case_style.capitalization = camel_case
        dotnet_naming_rule.members_should_be_pascal_case.severity = suggestion
        dotnet_naming_rule.members_should_be_pascal_case.symbols = all_members
        dotnet_naming_rule.members_should_be_pascal_case.style = pascal_case_style
        dotnet_naming_symbols.all_members.applicable_kinds = *
        dotnet_naming_style.pascal_case_style.capitalization = pascal_case
        """;

    [Fact]
    public Task NamingRules_DecideNonPrivateFields() => VerifyFixAsync(
        """
        public class BuilderSkeleton
        {
            internal int namingConvention;
            internal readonly int overrides;
            internal int {|BRO1303:EnumNamingConvention|};
            internal readonly int {|BRO1303:TypeConverterFactories|};
            protected int {|BRO1303:Depth|};
            protected internal int {|BRO1303:Width|};
            public int {|BRO1306:count|};
            public readonly int {|BRO1306:limit|};
            public static int {|BRO1306:shared|};
            internal static readonly int {|BRO1306:empty|};
            internal const int {|BRO1306:max|} = 1;

            public int Sum() => namingConvention + overrides + EnumNamingConvention + TypeConverterFactories + Depth + Width + count + limit + shared + empty + max;
        }
        """,
        """
        public class BuilderSkeleton
        {
            internal int namingConvention;
            internal readonly int overrides;
            internal int enumNamingConvention;
            internal readonly int typeConverterFactories;
            protected int depth;
            protected internal int width;
            public int Count;
            public readonly int Limit;
            public static int Shared;
            internal static readonly int Empty;
            internal const int Max = 1;

            public int Sum() => namingConvention + overrides + enumNamingConvention + typeConverterFactories + depth + width + Count + Limit + Shared + Empty + Max;
        }
        """,
        YamlDotNetNamingRules);

    // A naming rule requiring the 'm_' prefix of private instance fields keeps it (BRO1307 doesn't remove it).
    [Fact]
    public Task NamingRule_RequiringAPrefix_KeepsItOnPrivateFields() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private int m_count;

            public int Get() => m_count;
        }
        """,
        """
        dotnet_naming_rule.private_fields.severity = warning
        dotnet_naming_rule.private_fields.symbols = private_fields
        dotnet_naming_rule.private_fields.style = m_prefix
        dotnet_naming_symbols.private_fields.applicable_kinds = field
        dotnet_naming_symbols.private_fields.applicable_accessibilities = private
        dotnet_naming_style.m_prefix.capitalization = camel_case
        dotnet_naming_style.m_prefix.required_prefix = m_
        """);

    // A naming rule asking for a style StyleBro doesn't write ('m_' prefix) leaves the field alone; a rule that is off
    // (severity none) leaves StyleCop's default.
    [Fact]
    public Task NamingRules_OtherStylesLeaveTheName() => VerifyFixAsync(
        """
        public class C
        {
            internal int Value;
            protected int m_depth;
            public int {|BRO1306:count|};

            public int Sum() => Value + m_depth + count;
        }
        """,
        """
        public class C
        {
            internal int Value;
            protected int m_depth;
            public int Count;

            public int Sum() => Value + m_depth + Count;
        }
        """,
        """
        dotnet_naming_rule.prefixed.severity = warning
        dotnet_naming_rule.prefixed.symbols = non_public
        dotnet_naming_rule.prefixed.style = m_prefix
        dotnet_naming_symbols.non_public.applicable_kinds = field
        dotnet_naming_symbols.non_public.applicable_accessibilities = internal, protected
        dotnet_naming_style.m_prefix.capitalization = camel_case
        dotnet_naming_style.m_prefix.required_prefix = m_
        dotnet_naming_rule.public_off.severity = none
        dotnet_naming_rule.public_off.symbols = public_fields
        dotnet_naming_rule.public_off.style = camel
        dotnet_naming_symbols.public_fields.applicable_kinds = field
        dotnet_naming_symbols.public_fields.applicable_accessibilities = public
        dotnet_naming_style.camel.capitalization = camel_case
        """);

    // A naming rule nothing enforces doesn't decide non-private fields (owner's decision 2026-10-13): with IDE1006 off,
    // YamlDotNet's camelCase rule for internal and protected fields leaves StyleCop's defaults (PascalCase for internal).
    [Fact]
    public Task NamingRules_NotEnforced_LeaveStyleCopsDefaults() => VerifyFixAsync(
        """
        public class C
        {
            internal int {|BRO1306:namingConvention|};
            protected int depth;

            public int Sum() => namingConvention + depth;
        }
        """,
        """
        public class C
        {
            internal int NamingConvention;
            protected int depth;

            public int Sum() => NamingConvention + depth;
        }
        """,
        YamlDotNetNamingRules + "\ndotnet_diagnostic.IDE1006.severity = none");

    // Stryker (trial 23): Roslyn picks the '_' rule for private static fields over the PascalCase rule for static readonly
    // ones (its comparers don't order those two, so the rule order decides), and 'LockA' got IDE1006. The constant's rule
    // ('const' implies static and readonly) does win, so '_constC' -> 'ConstC' is what IDE1006 asks for too.
    [Fact]
    public Task StaticReadonlyField_FollowsTheRuleRoslynPicks() => VerifyFixAsync(
        """
        public class C
        {
            private static readonly object _lockA = new object();
            private const int {|BRO1306:_constC|} = 1;

            public int Use() => _lockA.GetHashCode() + _constC;
        }
        """,
        """
        public class C
        {
            private static readonly object _lockA = new object();
            private const int ConstC = 1;

            public int Use() => _lockA.GetHashCode() + ConstC;
        }
        """,
        """
        dotnet_diagnostic.IDE1006.severity = warning
        dotnet_naming_rule.async_members_should_be_pascal_case_suffixed_with_async.severity = warning
        dotnet_naming_rule.async_members_should_be_pascal_case_suffixed_with_async.style = pascal_case_suffixed_with_async
        dotnet_naming_rule.async_members_should_be_pascal_case_suffixed_with_async.symbols = async_members
        dotnet_naming_rule.const_declarations_should_be_pascal_case.severity = warning
        dotnet_naming_rule.const_declarations_should_be_pascal_case.style = pascal_case
        dotnet_naming_rule.const_declarations_should_be_pascal_case.symbols = const_declarations
        dotnet_naming_rule.interfaces_should_be_pascal_case_prefixed_with_i.severity = warning
        dotnet_naming_rule.interfaces_should_be_pascal_case_prefixed_with_i.style = pascal_case_prefixed_with_i
        dotnet_naming_rule.interfaces_should_be_pascal_case_prefixed_with_i.symbols = interfaces
        dotnet_naming_rule.parameters_should_be_camel_case.severity = warning
        dotnet_naming_rule.parameters_should_be_camel_case.style = camel_case
        dotnet_naming_rule.parameters_should_be_camel_case.symbols = parameters
        dotnet_naming_rule.private_and_internal_static_fields_should_be_camel_case.severity = warning
        dotnet_naming_rule.private_and_internal_static_fields_should_be_camel_case.style = prefix_underscore
        dotnet_naming_rule.private_and_internal_static_fields_should_be_camel_case.symbols = private_and_internal_static_fields
        dotnet_naming_rule.private_and_internal_fields_should_be_camel_case.severity = warning
        dotnet_naming_rule.private_and_internal_fields_should_be_camel_case.style = prefix_underscore
        dotnet_naming_rule.private_and_internal_fields_should_be_camel_case.symbols = private_and_internal_fields
        dotnet_naming_rule.public_and_protected_declarations_should_be_pascal_case.severity = warning
        dotnet_naming_rule.public_and_protected_declarations_should_be_pascal_case.style = pascal_case
        dotnet_naming_rule.public_and_protected_declarations_should_be_pascal_case.symbols = public_and_protected_declarations
        dotnet_naming_rule.static_readonly_declarations_should_be_pascal_case.severity = warning
        dotnet_naming_rule.static_readonly_declarations_should_be_pascal_case.style = pascal_case
        dotnet_naming_rule.static_readonly_declarations_should_be_pascal_case.symbols = static_readonly_declarations
        dotnet_naming_rule.type_parameters_should_be_pascal_case_prefixed_with_t.severity = warning
        dotnet_naming_rule.type_parameters_should_be_pascal_case_prefixed_with_t.style = pascal_case_prefixed_with_t
        dotnet_naming_rule.type_parameters_should_be_pascal_case_prefixed_with_t.symbols = type_parameters
        dotnet_naming_style.camel_case.capitalization = camel_case
        dotnet_naming_style.pascal_case.capitalization = pascal_case
        dotnet_naming_style.pascal_case_suffixed_with_async.capitalization = pascal_case
        dotnet_naming_style.pascal_case_suffixed_with_async.required_suffix = Async
        dotnet_naming_style.pascal_case_prefixed_with_i.capitalization = pascal_case
        dotnet_naming_style.pascal_case_prefixed_with_i.required_prefix = I
        dotnet_naming_style.pascal_case_prefixed_with_t.capitalization = pascal_case
        dotnet_naming_style.pascal_case_prefixed_with_t.required_prefix = T
        dotnet_naming_symbols.async_members.required_modifiers = async
        dotnet_naming_symbols.const_declarations.required_modifiers = const
        dotnet_naming_symbols.interfaces.applicable_kinds = interface
        dotnet_naming_symbols.parameters.applicable_kinds = parameter
        dotnet_naming_symbols.public_and_protected_declarations.applicable_accessibilities = public, protected
        dotnet_naming_symbols.public_and_protected_declarations.applicable_kinds = method, field, event, property
        dotnet_naming_symbols.private_and_internal_fields.applicable_accessibilities = private, internal
        dotnet_naming_symbols.private_and_internal_fields.applicable_kinds = field
        dotnet_naming_symbols.private_and_internal_static_fields.applicable_accessibilities = private, internal
        dotnet_naming_symbols.private_and_internal_static_fields.applicable_kinds = field
        dotnet_naming_symbols.private_and_internal_static_fields.required_modifiers = static
        dotnet_naming_symbols.static_readonly_declarations.required_modifiers = static, readonly
        dotnet_naming_symbols.type_parameters.applicable_kinds = type_parameter
        dotnet_naming_rule.private_members_with_underscore.symbols = private_members
        dotnet_naming_rule.private_members_with_underscore.style = prefix_underscore
        dotnet_naming_rule.private_members_with_underscore.severity = warning
        dotnet_naming_symbols.private_members.applicable_kinds = field
        dotnet_naming_symbols.private_members.applicable_accessibilities = private
        dotnet_naming_style.prefix_underscore.capitalization = camel_case
        dotnet_naming_style.prefix_underscore.required_prefix = _
        """);

    // IDE1006's own severity enforces a rule whose severity is silent; without it the silent rule doesn't count.
    [Theory]
    [InlineData("dotnet_diagnostic.IDE1006.severity = warning", "{|BRO1303:Value|}", "Value", "value")]
    [InlineData("", "{|BRO1306:value|}", "value", "Value")]
    public Task NamingRules_EnforcedByIde1006(string ide1006, string declared, string before, string after) => VerifyFixAsync(
        $$"""
        public class C
        {
            internal int {{declared}};

            public int Get() => {{before}};
        }
        """,
        $$"""
        public class C
        {
            internal int {{after}};

            public int Get() => {{after}};
        }
        """,
        $"""
        dotnet_naming_rule.internal_fields.severity = silent
        dotnet_naming_rule.internal_fields.symbols = internal_fields
        dotnet_naming_rule.internal_fields.style = camel
        dotnet_naming_symbols.internal_fields.applicable_kinds = field
        dotnet_naming_symbols.internal_fields.applicable_accessibilities = internal
        dotnet_naming_style.camel.capitalization = camel_case
        {ide1006}
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
                    [UnsafeAccessor(Name = "_intValue")]
                    public static void IntValue(Dto dto)
                    {
                    }
                }

                class UnsafeAccessorAttribute : System.Attribute
                {
                    public string Name { get; set; } = string.Empty;
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
            public const int {|BRO1308:Max_Count|} = 1;
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

    // Stateless: 'nameof(args)' of a parameter in another file kept the test class's private field 'args' (matched by name).
    [Fact]
    public Task NameofOfAnotherSymbolWithTheName_DoesNotKeepTheField() => VerifyFixAsync(
        [
            """
            class C
            {
                private readonly object[] {|BRO1303:_args|} = { 5 };

                public int Count => _args.Length;
            }
            """,
            """
            class D
            {
                public static void Unpack(object[] _args)
                {
                    if (_args == null) throw new System.ArgumentNullException(nameof(_args));
                }
            }
            """,
        ],
        [
            """
            class C
            {
                private readonly object[] args = { 5 };

                public int Count => args.Length;
            }
            """,
            """
            class D
            {
                public static void Unpack(object[] _args)
                {
                    if (_args == null) throw new System.ArgumentNullException(nameof(_args));
                }
            }
            """,
        ]);

    // Non-private fields can be used from other files' '#if' code, which no rename sees; private ones only from their
    // type (TypeFacts).
    [Fact]
    public Task NonPrivateFieldsInDisabledCode_AreSkipped() => VerifyNoDiagnosticsAsync(
        ("/0/A.cs", """
            internal class Store
            {
                internal int total;
            }
            """),
        ("/0/B.cs", """
            internal class Uses
            {
            #if NEVER
                int M(Store s) => s.total;
            #endif
            }
            """));

    // Speckle (trial 25): a public field of a private struct can only be used inside the type around the struct, so
    // another file's '#if' code doesn't count; that type's does.
    [Fact]
    public Task FieldsOfAPrivateType_OnlyTheirScopesDisabledCodeCounts() => VerifyFixAsync(
        new[]
        {
            """
            public class Mesh
            {
                private struct Vector
                {
                    public double {|BRO1306:x|};
                    public double y;
                }

            #if NEVER
                int M(Vector v) => v.y;
            #endif
            }
            """,
            """
            internal class Other
            {
            #if NEVER
                int x;
            #endif
            }
            """,
        },
        new[]
        {
            """
            public class Mesh
            {
                private struct Vector
                {
                    public double X;
                    public double y;
                }

            #if NEVER
                int M(Vector v) => v.y;
            #endif
            }
            """,
            """
            internal class Other
            {
            #if NEVER
                int x;
            #endif
            }
            """,
        });

    // MoreLINQ (trial 20): Lookup.cs is copied from dotnet/runtime and starts with the pragma; '_count' stays.
    [Fact]
    public Task FieldsWhereIde1006IsDisabled_KeepTheirNames() => VerifyFixAsync(
        """
        #pragma warning disable IDE1006 // Naming rule violation
        public sealed class Counter
        {
            private int _count;

            public int Next() => _count++;
        }
        #pragma warning restore IDE1006

        public sealed class Other
        {
            private int {|BRO1303:_count|};

            public int Next() => _count++;
        }
        """,
        """
        #pragma warning disable IDE1006 // Naming rule violation
        public sealed class Counter
        {
            private int _count;

            public int Next() => _count++;
        }
        #pragma warning restore IDE1006

        public sealed class Other
        {
            private int count;

            public int Next() => count++;
        }
        """);

    // SSH.NET (trial 21, StyleCop-clean): SA1310 doesn't check a name starting with '_', and in the _camelCase style SA1309
    // never removes it, so '_signature_size' stays. A name without the underscore still gets the whole style in one rename.
    [Fact]
    public Task UnderscoreStyle_LeavesUnderscoresInANameStartingWithOne() => VerifyFixAsync(
        """
        class C
        {
            private readonly int _signature_size;
            private int _a_b;
            private int {|BRO1308:sig_size|};

            public C(int size) => _signature_size = size;

            public int Use() => _signature_size + _a_b + sig_size;
        }
        """,
        """
        class C
        {
            private readonly int _signature_size;
            private int _a_b;
            private int _sigSize;

            public C(int size) => _signature_size = size;

            public int Use() => _signature_size + _a_b + _sigSize;
        }
        """,
        editorConfig: Underscore);

    // In the camelCase style the underscore goes (SA1309), and the rest in the same rename (SA1310).
    [Fact]
    public Task CamelCaseStyle_StillJoinsANameStartingWithAnUnderscore() => VerifyFixAsync(
        "class C\n{\n    private int {|BRO1308:_signature_size|};\n\n    public int Use() => _signature_size;\n}\n",
        "class C\n{\n    private int signatureSize;\n\n    public int Use() => signatureSize;\n}\n");

    // AngleSharp (trial 21): a nested class's 'valid' was kept because the outer class has '_valid'. The renamed field hides
    // it inside the nested class (lookup searches the type before the enclosing types).
    [Fact]
    public Task NestedField_IsRenamedPastAnOuterFieldWithTheNewName() => VerifyFixAsync(
        """
        public sealed class Outer
        {
            private bool _valid = true;

            public bool IsValid => _valid;

            private sealed class Inner
            {
                private bool {|BRO1303:valid|};

                public Inner() => valid = true;

                public bool Check() => valid;
            }
        }
        """,
        """
        public sealed class Outer
        {
            private bool _valid = true;

            public bool IsValid => _valid;

            private sealed class Inner
            {
                private bool _valid;

                public Inner() => _valid = true;

                public bool Check() => _valid;
            }
        }
        """,
        editorConfig: Underscore);

    // ... unless the nested class uses the outer member by that name: the rename would change what that use means.
    [Fact]
    public Task NestedField_IsKeptWhenTheNestedClassUsesTheOuterName() => VerifyNotFixedAsync(
        new[]
        {
            """
            public sealed class Outer
            {
                private static bool _valid = true;

                private sealed class Inner
                {
                    private bool {|BRO1303:valid|};

                    public bool Check() => valid && _valid;
                }
            }
            """,
        },
        Underscore);

    // AzureEventGridSimulator (trial 21): a field named like its own type ('Color Color') is legal, and the type's other
    // uses still bind to the type.
    [Fact]
    public Task FieldNamedLikeItsType_IsRenamed() => VerifyFixAsync(
        """
        public sealed class SimulatorEvent
        {
            public static SimulatorEvent Create() => new();

            public bool Accepts() => true;
        }

        public class FilterTests
        {
            private static readonly SimulatorEvent {|BRO1306:_simulatorEvent|} = SimulatorEvent.Create();

            public bool Check(object o) => _simulatorEvent.Accepts() && SimulatorEvent.Create().Accepts() && o is SimulatorEvent && typeof(SimulatorEvent) != null;
        }
        """,
        """
        public sealed class SimulatorEvent
        {
            public static SimulatorEvent Create() => new();

            public bool Accepts() => true;
        }

        public class FilterTests
        {
            private static readonly SimulatorEvent SimulatorEvent = SimulatorEvent.Create();

            public bool Check(object o) => SimulatorEvent.Accepts() && SimulatorEvent.Create().Accepts() && o is SimulatorEvent && typeof(SimulatorEvent) != null;
        }
        """,
        editorConfig: Underscore);

    // A field of another type with that name stays: 'SimulatorEvent.Create()' would then mean the field.
    [Fact]
    public Task FieldNamedLikeAnotherType_IsKept() => VerifyNotFixedAsync(
        new[]
        {
            """
            public sealed class SimulatorEvent
            {
                public static SimulatorEvent Create() => new();
            }

            public class FilterTests
            {
                private static readonly object {|BRO1306:_simulatorEvent|} = SimulatorEvent.Create();

                public object Check() => _simulatorEvent ?? SimulatorEvent.Create();
            }
            """,
        },
        Underscore);

    private const string RuntimeStatics = """
        stylebro_private_static_field_naming = s_camelCase
        stylebro_private_mutable_static_field_naming = s_camelCase
        """;

    [Theory]
    [InlineData("count", "s_count")]
    [InlineData("_count", "s_count")]
    [InlineData("Count", "s_count")]
    [InlineData("s_Count", "s_count")]
    [InlineData("s_count", null)]
    [InlineData("s_", null)]
    [InlineData("_1", null)]
    [InlineData("max_value", null)]
    [InlineData("s_field", null)]
    [InlineData("field", "s_field")]
    public void NewName_StaticPrefix(string name, string? expected) =>
        Assert.Equal(expected, FieldNames.GetNewName(name, FieldStyle.StaticCamelCase));

    // The .NET runtime's static fields: 's_' on static and static readonly fields, constants stay PascalCase, other one-letter
    // prefixes become 's_' (BRO1307), words after 's_' are joined (BRO1308), instance fields lose 's_'.
    [Fact]
    public Task StaticKeys_SCamelCase_PrefixStaticFields() => VerifyFixAsync(
        """
        class C
        {
            private const int Max = 1;
            private static readonly int {|BRO1306:Limit|} = 1;
            private static readonly int s_fine = 2;
            private static int {|BRO1303:count|};
            private static int {|BRO1303:_total|};
            private static int {|BRO1307:m_old|};
            private static int {|BRO1303:s_Upper|};
            private static int {|BRO1308:s_two_words|};
            private static int s_kept;
            private int {|BRO1307:s_instance|};
            private int _done;

            public int Sum() => Max + Limit + s_fine + count + _total + m_old + s_Upper + s_two_words + s_kept + s_instance + _done;
        }
        """,
        """
        class C
        {
            private const int Max = 1;
            private static readonly int s_limit = 1;
            private static readonly int s_fine = 2;
            private static int s_count;
            private static int s_total;
            private static int s_old;
            private static int s_upper;
            private static int s_twoWords;
            private static int s_kept;
            private int _instance;
            private int _done;

            public int Sum() => Max + s_limit + s_fine + s_count + s_total + s_old + s_upper + s_twoWords + s_kept + _instance + _done;
        }
        """,
        Underscore + "\n" + RuntimeStatics);

    // Two fields that would both become 's_count' keep their names.
    [Fact]
    public Task StaticKeys_SCamelCase_CollisionsKeepTheirNames() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            private static int count;
            private static int _count;

            public int Sum() => count + _count;
        }
        """,
        RuntimeStatics);

    // The .NET runtime's naming rule: internal and private protected fields are named like private ones.
    [Fact]
    public Task InternalLikePrivate_NamesInternalFieldsLikePrivateOnes() => VerifyFixAsync(
        """
        class C
        {
            internal int {|BRO1303:Count|};
            internal int _fine;
            private protected int {|BRO1303:total|};
            internal static int {|BRO1303:Instances|};
            internal static readonly int {|BRO1306:Limit|} = 1;
            internal const int Max = 1;
            public int Open;
            protected int shared;
            protected internal int Both;

            public int Sum() => Count + _fine + total + Instances + Limit + Max + Open + shared + Both;
        }
        """,
        """
        class C
        {
            internal int _count;
            internal int _fine;
            private protected int _total;
            internal static int s_instances;
            internal static readonly int s_limit = 1;
            internal const int Max = 1;
            public int Open;
            protected int shared;
            protected internal int Both;

            public int Sum() => _count + _fine + _total + s_instances + s_limit + Max + Open + shared + Both;
        }
        """,
        Underscore + "\n" + RuntimeStatics + "\nstylebro_internal_field_naming = like_private");

    // The .NET runtime's own setup: its naming rules at suggestion with IDE1006 silent (not enforced, so BRO1306 would make
    // '_count' PascalCase); the key keeps internal fields '_camelCase'.
    [Fact]
    public Task InternalLikePrivate_RuntimesUnenforcedNamingRules() => VerifyFixAsync(
        """
        class C
        {
            internal int _fine;
            internal int {|BRO1303:Count|};

            public int Get() => _fine + Count;
        }
        """,
        """
        class C
        {
            internal int _fine;
            internal int _count;

            public int Get() => _fine + _count;
        }
        """,
        """
        stylebro_private_field_naming = _camelCase
        stylebro_internal_field_naming = like_private
        dotnet_diagnostic.IDE1006.severity = silent
        dotnet_naming_rule.private_internal_fields.severity = suggestion
        dotnet_naming_rule.private_internal_fields.symbols = private_internal_fields
        dotnet_naming_rule.private_internal_fields.style = underscore
        dotnet_naming_symbols.private_internal_fields.applicable_kinds = field
        dotnet_naming_symbols.private_internal_fields.applicable_accessibilities = private, internal
        dotnet_naming_style.underscore.capitalization = camel_case
        dotnet_naming_style.underscore.required_prefix = _
        """);

    // An enforced naming rule that asks for something else wins: no rename IDE1006 would report.
    [Fact]
    public Task InternalLikePrivate_EnforcedNamingRuleWins() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            internal int Count;

            public int Get() => Count;
        }
        """,
        """
        stylebro_internal_field_naming = like_private
        dotnet_naming_rule.internal_fields.severity = warning
        dotnet_naming_rule.internal_fields.symbols = internal_fields
        dotnet_naming_rule.internal_fields.style = pascal
        dotnet_naming_symbols.internal_fields.applicable_kinds = field
        dotnet_naming_symbols.internal_fields.applicable_accessibilities = internal
        dotnet_naming_style.pascal.capitalization = pascal_case
        """);

    // The key decides the name, not an enforced naming rule for internal fields ('count' here): '_count' stays.
    [Fact]
    public Task InternalLikePrivate_KeyDecidesTheName() => VerifyNoDiagnosticsAsync(
        """
        class C
        {
            internal int _count;

            public int Get() => _count;
        }
        """,
        """
        stylebro_private_field_naming = _camelCase
        stylebro_internal_field_naming = like_private
        dotnet_naming_rule.internal_fields.severity = warning
        dotnet_naming_rule.internal_fields.symbols = internal_fields
        dotnet_naming_rule.internal_fields.style = camel
        dotnet_naming_symbols.internal_fields.applicable_kinds = field
        dotnet_naming_symbols.internal_fields.applicable_accessibilities = internal
        dotnet_naming_style.camel.capitalization = camel_case
        """);

    // Without the key (StyleCop's way): internal fields PascalCase, private protected ones camelCase.
    [Fact]
    public Task InternalFields_PascalCaseByDefault() => VerifyFixAsync(
        """
        class C
        {
            internal int {|BRO1306:_count|};
            private protected int {|BRO1303:_total|};

            public int Sum() => _count + _total;
        }
        """,
        """
        class C
        {
            internal int Count;
            private protected int total;

            public int Sum() => Count + total;
        }
        """,
        "stylebro_internal_field_naming = PascalCase");
}
