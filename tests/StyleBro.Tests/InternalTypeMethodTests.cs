using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Maintainability;
using StyleBro.CodeFixes.Maintainability;
using Verify = StyleBro.Tests.Verifier<StyleBro.Analyzers.Maintainability.InternalTypeMethodAnalyzer, StyleBro.CodeFixes.Maintainability.InternalTypeMethodCodeFixProvider>;

namespace StyleBro.Tests;

public class InternalTypeMethodTests
{
    private const string On = "dotnet_diagnostic.BRO1409.severity = warning\ndotnet_diagnostic.BRO1001.severity = none\n";

    [Fact]
    public Task OffByDefault() => Verify.VerifyNoDiagnosticsAsync(
        """
        internal class C
        {
            public void Run()
            {
            }
        }
        """);

    [Fact]
    public Task PublicMethodsOfHiddenTypes_BecomeInternal() => Verify.VerifyFixAsync(
        """
        using System.Collections.Generic;

        class Implicit
        {
            {|BRO1409:public|} void Run()
            {
            }

            static {|BRO1409:public|} int Twice(int x) => x * 2;

            internal void AlreadyInternal()
            {
            }

            private void Private()
            {
            }

            public int Property { get; set; }

            public Implicit()
            {
            }

            public static Implicit operator +(Implicit a, Implicit b) => a;
        }

        public class Outer
        {
            private class Nested
            {
                {|BRO1409:public|} void Run()
                {
                }
            }

            private protected class Guarded
            {
                {|BRO1409:public|} void Run()
                {
                }
            }

            public class Visible
            {
                public void Run()
                {
                }
            }

            protected internal class Inherited
            {
                public void Run()
                {
                }
            }
        }

        internal static class Extensions
        {
            {|BRO1409:public|} static int Count<T>(this List<T> list) => list.Count;
        }

        file class Local
        {
            {|BRO1409:public|} void Run()
            {
            }
        }

        internal struct S
        {
            {|BRO1409:public|} readonly int Get() => 1;
        }

        internal record R
        {
            internal int x;

            {|BRO1409:public|} int Double() => this.x * 2;

            public override string ToString() => "R";
        }

        internal class Generic<T>
        {
            {|BRO1409:public|} T Echo(T value) => value;
        }
        """,
        """
        using System.Collections.Generic;

        class Implicit
        {
            internal void Run()
            {
            }

            static internal int Twice(int x) => x * 2;

            internal void AlreadyInternal()
            {
            }

            private void Private()
            {
            }

            public int Property { get; set; }

            public Implicit()
            {
            }

            public static Implicit operator +(Implicit a, Implicit b) => a;
        }

        public class Outer
        {
            private class Nested
            {
                internal void Run()
                {
                }
            }

            private protected class Guarded
            {
                internal void Run()
                {
                }
            }

            public class Visible
            {
                public void Run()
                {
                }
            }

            protected internal class Inherited
            {
                public void Run()
                {
                }
            }
        }

        internal static class Extensions
        {
            internal static int Count<T>(this List<T> list) => list.Count;
        }

        file class Local
        {
            internal void Run()
            {
            }
        }

        internal struct S
        {
            internal readonly int Get() => 1;
        }

        internal record R
        {
            internal int x;

            internal int Double() => this.x * 2;

            public override string ToString() => "R";
        }

        internal class Generic<T>
        {
            internal T Echo(T value) => value;
        }
        """,
        On);

    [Fact]
    public Task WithBro1001On_TheTypeIsSortedInTheSameFix() => Verify.VerifyFixAsync(
        """
        internal class C
        {
            {|BRO1409:public|} void Run()
            {
            }

            public void Dispose()
            {
            }
        }

        internal class Untouched
        {
            private void Wait()
            {
            }

            internal void Stop()
            {
            }
        }
        """,
        """
        internal class C
        {
            public void Dispose()
            {
            }

            internal void Run()
            {
            }
        }

        internal class Untouched
        {
            private void Wait()
            {
            }

            internal void Stop()
            {
            }
        }
        """,
        "dotnet_diagnostic.BRO1409.severity = warning\n");

    [Fact]
    public Task RegionsAndPragmas_DontCount() => Verify.VerifyFixAsync(
        """
        internal class C
        {
            #region Running
            {|BRO1409:public|} void Run()
            {
                #region Body
        #pragma warning disable CS0168
                int unused;
        #pragma warning restore CS0168
                #endregion
            }
            #endregion
        }
        """,
        """
        internal class C
        {
            #region Running
            internal void Run()
            {
                #region Body
        #pragma warning disable CS0168
                int unused;
        #pragma warning restore CS0168
                #endregion
            }
            #endregion
        }
        """,
        On);

    [Fact]
    public Task APartialTypeIsJudgedByAllItsParts() => Verify.VerifyFixAsync(
        """
        public partial class Shown
        {
        }

        partial class Shown
        {
            public void Run()
            {
            }
        }

        internal partial class Hidden
        {
        }

        partial class Hidden
        {
            {|BRO1409:public|} void Run()
            {
            }
        }
        """,
        """
        public partial class Shown
        {
        }

        partial class Shown
        {
            public void Run()
            {
            }
        }

        internal partial class Hidden
        {
        }

        partial class Hidden
        {
            internal void Run()
            {
            }
        }
        """,
        On);

    [Fact]
    public Task MembersOthersRelyOn_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        using System;
        using System.Collections;
        using System.Collections.Generic;

        internal abstract class Base
        {
            public abstract void Abstract();

            public virtual void Virtual()
            {
            }

            public override string ToString() => "B";
        }

        internal sealed class Derived : Base
        {
            public override void Abstract()
            {
            }

            public sealed override void Virtual()
            {
            }

            public new int GetHashCode() => 1;
        }

        internal interface IRunner
        {
            void Run();

            public static void Helper()
            {
            }
        }

        internal class Runner : IRunner, IComparable<Runner>
        {
            public void Run()
            {
            }

            public int CompareTo(Runner other) => 0;

            void Explicit()
            {
            }
        }

        internal class InheritedImplementation
        {
            public void Run()
            {
            }
        }

        internal class Implementer : InheritedImplementation, IRunner
        {
        }

        public class Host
        {
            private class Nested : IRunner
            {
                public void Run()
                {
                }
            }
        }

        internal class Conventions : IEnumerable
        {
            public static void Main()
            {
            }

            public void Dispose()
            {
            }

            public IEnumerator GetEnumerator() => null!;

            public void Add(int x)
            {
            }

            public void Deconstruct(out int a, out int b) => (a, b) = (1, 2);

            public void Configure()
            {
            }

            public void Invoke()
            {
            }
        }

        internal static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32")]
            public static extern int GetTickCount();

            public static extern int Undecorated();
        }

        internal partial class Generated
        {
            public partial void Hook();

            public partial void Hook()
            {
            }
        }

        internal class Attributed
        {
            [Obsolete]
            public void Old()
            {
            }

            [return: System.Diagnostics.CodeAnalysis.NotNull]
            public object Value() => new object();
        }

        [Serializable]
        internal class Marked
        {
            public void Run()
            {
            }

            private class Inner
            {
                public void Run()
                {
                }
            }
        }

        internal class Failure : Exception
        {
            public void Describe()
            {
            }
        }

        internal class List : List<int>
        {
            public void Describe()
            {
            }
        }
        """,
        On);

    [Fact]
    public Task SourceBaseTypes_AreReported() => Verify.VerifyFixAsync(
        """
        internal class Base
        {
        }

        internal class Derived : Base
        {
            {|BRO1409:public|} void Run()
            {
            }
        }
        """,
        """
        internal class Base
        {
        }

        internal class Derived : Base
        {
            internal void Run()
            {
            }
        }
        """,
        On);

    [Fact]
    public Task InheritedImplementations_InNamespacesAndNestedTypes_AreNotReported() => Verify.VerifyNoDiagnosticsAsync(
        """
        namespace App.Inner
        {
            internal interface IRunner
            {
                void Run();
            }

            internal class Base
            {
                public void Run()
                {
                }
            }
        }

        namespace App.Other
        {
            internal class Outer
            {
                internal class Implementer : App.Inner.Base, App.Inner.IRunner
                {
                }
            }
        }
        """,
        On);

    [Fact]
    public Task MethodsWithDirectives_AreSkipped() => Verify.VerifyNoDiagnosticsAsync(
        """
        internal class D
        {
        #if DEBUG
            public void Run()
            {
            }
        #endif

            public void Other()
            {
        #if DEBUG
                System.Console.WriteLine();
        #endif
            }
        }
        """,
        On);

    [Fact]
    public Task BaseListWithADirective_IsSkipped() => Verify.VerifyNoDiagnosticsAsync(
        """
        internal class B : System.IComparable,
        #if DEBUG
            System.ICloneable,
        #endif
            System.IFormattable
        {
            public int CompareTo(object? other) => 0;

            public object Clone() => this;

            public string ToString(string? format, System.IFormatProvider? provider) => "";

            public void Run()
            {
            }
        }

        internal class C
        #if DEBUG
            : System.ICloneable
        #endif
        {
            public object Clone() => this;
        }
        """,
        On);

    [Fact]
    public Task NamesInStringsOrNameof_AreReportedButNotFixed() => Verify.VerifyNotFixedAsync(
        new[]
        {
            """
            internal class C
            {
                {|BRO1409:public|} void Run()
                {
                }

                {|BRO1409:public|} void Stop()
                {
                }
            }
            """,
            """
            internal static class Reflect
            {
                internal static object? Find() => typeof(C).GetMethod("Run") ?? typeof(C).GetMethod(nameof(C.Stop));
            }
            """,
        },
        On);

    [Fact]
    public async Task ATypeAFriendAssemblyDerivesFrom_IsReportedButNotFixed()
    {
        // The derived class in the friend assembly implements IRunner with the inherited Run: 'internal' would break it.
        const string Library = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]

            public interface IRunner
            {
                void Run();
            }

            internal class Base
            {
                {|BRO1409:public|} void Run()
                {
                }
            }
            """;
        var test = new CSharpCodeFixTest<InternalTypeMethodAnalyzer, InternalTypeMethodCodeFixProvider, DefaultVerifier>
        {
            TestCode = Library,
            FixedCode = Library,
            NumberOfIncrementalIterations = 0,
            NumberOfFixAllIterations = 0,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck,
        };
        foreach (var state in new[] { test.TestState, test.FixedState })
        {
            state.AdditionalProjects["App"].Sources.Add("internal class Derived : Base, IRunner\n{\n}\n");
            state.AdditionalProjects["App"].AdditionalProjectReferences.Add("TestProject");
        }

        await test.RunAsync();
    }
}
