using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.QualifiedUsingsAnalyzer, StyleBro.CodeFixes.Readability.QualifiedUsingsCodeFixProvider>;

namespace StyleBro.Tests;

public class QualifiedUsingsTests
{
    [Fact]
    public Task RelativeNames_AreQualified() => Verify.VerifyFixAsync("""
        namespace System.Tools
        {
            {|BRO1126:using IO;|}
            {|BRO1126:using Text = Text.StringBuilder;|}
            {|BRO1126:using static Math;|}
            {|BRO1126:using Pairs = Collections.Generic.List<Collections.Generic.KeyValuePair<int, string>>;|}
            using System.Linq;

            class C
            {
            }
        }
        """, """
        namespace System.Tools
        {
            using System.IO;
            using Text = System.Text.StringBuilder;
            using static System.Math;
            using Pairs = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, string>>;
            using System.Linq;

            class C
            {
            }
        }
        """);

    [Fact]
    public Task OutsideNamespacesAliasesGlobalAndOwnNamespace_AreNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        using System;

        namespace Outer
        {
            using global::System.IO;
            using Sys = System;

            public class Helper
            {
            }

            namespace Inner
            {
                using Sys.Text;
            }
        }

        namespace Outer
        {
            using static Helper;
        }
        """);

    [Fact]
    public Task QualifiedNameHiddenByAnEnclosingNamespace_IsNotReported() => Verify.VerifyNoDiagnosticsAsync("""
        namespace Lib
        {
            public class Thing
            {
            }
        }

        namespace Lib.Inner.Lib
        {
        }

        namespace Lib.Inner
        {
            // 'Lib.Thing' would mean Lib.Inner.Lib.Thing here.
            using static Thing;
        }
        """);
}
