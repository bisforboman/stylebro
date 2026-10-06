using StyleBro.Analyzers.Naming;
using Fields = StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.FieldNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;
using Locals = StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.CamelCaseNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class HungarianNamingTests
{
    private const string On = "dotnet_diagnostic.BRO1310.severity = warning";

    [Theory]
    [InlineData("iCount", "count")]
    [InlineData("szName", "name")]
    [InlineData("pURL", "url")]
    [InlineData("isOpen", null)]
    [InlineData("onClick", null)]
    [InlineData("strName", null)]
    [InlineData("count", null)]
    [InlineData("x", null)]
    [InlineData("xY", "y")]
    public void NewName(string name, string? expected) =>
        Assert.Equal(expected, HungarianNames.Read(new TestOptions()).GetNewName(name));

    [Fact]
    public void Configuration_AllowsPrefixes_AndTurnsOffTheCommonWords()
    {
        var names = HungarianNames.Read(new TestOptions(
            (HungarianNames.AllowedKey, "db, ui"),
            (HungarianNames.AllowCommonKey, "false")));
        Assert.Null(names.GetNewName("dbContext"));
        Assert.Null(names.GetNewName("uiState"));
        Assert.Equal("open", names.GetNewName("isOpen"));
    }

    [Fact]
    public Task VariablesAndParameters_LoseThePrefix_InOneRename() => Locals.VerifyFixAsync(
        """
        public class C
        {
            public int M(int {|BRO1310:iValue|}, bool isSet, int {|BRO1310:_nCount|})
            {
                var {|BRO1310:szName|} = iValue.ToString();
                var {|BRO1301:Total|} = _nCount;
                return szName.Length + Total + (isSet ? 1 : 0);
            }
        }
        """,
        """
        public class C
        {
            public int M(int value, bool isSet, int count)
            {
                var name = value.ToString();
                var total = count;
                return name.Length + total + (isSet ? 1 : 0);
            }
        }
        """,
        editorConfig: On);

    [Fact]
    public Task Fields_LoseThePrefix_InTheirStyle() => Fields.VerifyFixAsync(
        """
        public class C
        {
            private int {|BRO1310:iCount|};
            private string {|BRO1310:_szName|} = "";
            private int {|BRO1310:m_nTotal|};
            private const int MaxValue = 1;
            public int {|BRO1306:xPos|};
            protected int {|BRO1310:bFlag|};

            public int M() => iCount + _szName.Length + m_nTotal + xPos + bFlag;
        }
        """,
        """
        public class C
        {
            private int _count;
            private string _name = "";
            private int _total;
            private const int MaxValue = 1;
            public int XPos;
            protected int flag;

            public int M() => _count + _name.Length + _total + XPos + flag;
        }
        """,
        editorConfig: On + "\nstylebro_private_field_naming = _camelCase");

    [Fact]
    public Task ParametersOfNativeMethods_KeepTheirNames() => Locals.VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Runtime.InteropServices;

        public class C
        {
            [DllImport("user32.dll")]
            private static extern int GetWindowText(IntPtr hWnd, IntPtr lpString, int nMaxCount);

            [System.Runtime.InteropServices.DllImportAttribute("kernel32.dll")]
            private static extern void SetFlags(int dwFlags);

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
            private static extern void Native(int dwFlags);

            public void M()
            {
                [DllImport("kernel32.dll")]
                static extern void Local(int dwFlags);
            }
        }
        """,
        editorConfig: On);

    [Fact]
    public void OffByDefault()
    {
        // The test framework turns on every supported diagnostic, so this checks what a build without configuration sees.
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class C { }");
        var options = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary);
        Assert.False(StyleBro.Analyzers.Descriptors.HungarianNotation.IsEnabledByDefault);
        Assert.False(StyleBro.Analyzers.Severities.IsOn(options, tree, "BRO1310", default, enabledByDefault: false));

        // Without the argument, the rule's own default (its descriptor) counts: off, like the build sees it.
        Assert.False(StyleBro.Analyzers.Severities.IsOn(options, tree, "BRO1310", default));
        Assert.True(StyleBro.Analyzers.Severities.IsOn(options, tree, "BRO1504", default));
    }

    [Fact]
    public Task MemberNames_DoNotBlockARename() => Locals.VerifyFixAsync(
        """
        public class C
        {
            public int M(System.Exception error)
            {
                var {|BRO1310:rResult|} = error.HResult;
                return rResult;
            }
        }
        """,
        """
        public class C
        {
            public int M(System.Exception error)
            {
                var result = error.HResult;
                return result;
            }
        }
        """,
        On);

    [Fact]
    public Task NamesThatWouldCollide_AreNotReported() => Locals.VerifyNoDiagnosticsAsync(
        """
        using System.Linq;

        public class C
        {
            public int M(int[] items)
            {
                var q = from xItem in items let yItem = xItem select yItem;
                return q.Count();
            }
        }
        """,
        On);

    [Fact]
    public Task FieldsThatWouldCollide_AreNotReported() => Fields.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private int nCount;
            private int iCount;

            public int M() => nCount + iCount;
        }
        """,
        On);

    [Fact]
    public Task NativeMethods_KeepTheirPrefixes() => Locals.VerifyNoDiagnosticsAsync(
        """
        internal static class SafeNativeMethods
        {
            public static int M(int lpBuffer)
            {
                var dwSize = lpBuffer;
                return dwSize;
            }
        }
        """,
        On);

    [Fact]
    public Task NativeMethods_FieldsKeepTheirPrefixes() => Fields.VerifyNoDiagnosticsAsync(
        """
        internal class NativeMethods
        {
            private int dwFlags;

            public int M() => dwFlags;
        }
        """,
        On);

    [Fact]
    public Task AllowedPrefixes_AreNotReported() => Locals.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int M(int dbValue, bool isSet, int xY) => dbValue + (isSet ? xY : 0);
        }
        """,
        On + "\nstylebro_allowed_hungarian_prefixes = db, x");

    [Fact]
    public Task NothingIsRenamedToField_ACSharp14KeywordInAccessors() => Locals.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            public int P
            {
                get
                {
                    var iField = 2;
                    return iField;
                }
            }
        }
        """,
        On);

    [Fact]
    public Task FieldsAreNotRenamedToField_ACSharp14KeywordInAccessors() => Fields.VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            private int iField;

            public int P => iField;
        }
        """,
        On);

    private sealed class TestOptions(params (string Key, string Value)[] values) : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = values.FirstOrDefault(v => v.Key == key).Value!;
            return value is not null;
        }
    }
}

public class HungarianAbstractTests
{
    [Fact]
    public Task AbstractParameters_AreRenamedWithTheirOverrides() => Verifier<StyleBro.Analyzers.Naming.CamelCaseNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>.VerifyFixAsync(
        """
        public abstract class Base
        {
            public abstract int Grow(int {|BRO1310:nSize|});
        }

        public class Derived : Base
        {
            public override int Grow(int nSize) => nSize;
        }
        """,
        """
        public abstract class Base
        {
            public abstract int Grow(int size);
        }

        public class Derived : Base
        {
            public override int Grow(int size) => size;
        }
        """,
        "dotnet_diagnostic.BRO1310.severity = warning");
}
