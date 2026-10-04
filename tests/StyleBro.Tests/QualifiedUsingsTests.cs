using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Readability.QualifiedUsingsAnalyzer, StyleBro.CodeFixes.Readability.QualifiedUsingsCodeFixProvider>;

namespace StyleBro.Tests;

public class QualifiedUsingsTests
{
    [Fact]
    public Task RelativeNames_AreQualified() => Verify.VerifyFixAsync(
        """
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
        """,
        """
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
    public Task TuplesNullablesAndKeywords_StayNames() => Verify.VerifyFixAsync(
        """
        namespace System.Tools
        {
            {|BRO1126:using Pair = ValueTuple<Threading.Tasks.Task, int>;|}
            {|BRO1126:using Status = Nullable<Threading.Tasks.TaskStatus>;|}
            {|BRO1126:using Number = Int32;|}
            {|BRO1126:using Pairs = Collections.Generic.List<(int, Threading.Tasks.Task)>;|}
            {|BRO1126:using Statuses = Collections.Generic.List<Nullable<Threading.Tasks.TaskStatus>>;|}

            class C
            {
            }
        }
        """,
        """
        namespace System.Tools
        {
            using Pair = System.ValueTuple<System.Threading.Tasks.Task, int>;
            using Status = System.Nullable<System.Threading.Tasks.TaskStatus>;
            using Number = System.Int32;
            using Pairs = System.Collections.Generic.List<(int, System.Threading.Tasks.Task)>;
            using Statuses = System.Collections.Generic.List<System.Threading.Tasks.TaskStatus?>;

            class C
            {
            }
        }
        """);

    [Fact]
    public Task AliasInATypeArgument_IsKeptLikeAnAliasAtTheStart() => Verify.VerifyFixAsync(
        """
        using Tasks = System.Threading.Tasks;

        namespace System.Tools
        {
            using T1 = Tasks.Task;
            using T2 = System.ValueTuple<Tasks.Task, int>;
            using T3 = System.Collections.Generic.List<System.Collections.Generic.List<Tasks.Task>>;
            {|BRO1126:using T4 = ValueTuple<Tasks.Task, IO.Stream>;|}

            class C
            {
            }
        }
        """,
        """
        using Tasks = System.Threading.Tasks;

        namespace System.Tools
        {
            using T1 = Tasks.Task;
            using T2 = System.ValueTuple<Tasks.Task, int>;
            using T3 = System.Collections.Generic.List<System.Collections.Generic.List<Tasks.Task>>;
            using T4 = System.ValueTuple<Tasks.Task, System.IO.Stream>;

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
