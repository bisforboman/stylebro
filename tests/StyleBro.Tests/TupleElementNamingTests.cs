using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.TupleElementNamingAnalyzer, StyleBro.CodeFixes.Naming.TupleElementNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class TupleElementNamingTests
{
    [Theory]
    [InlineData("count", false, "Count")]
    [InlineData("Count", false, null)]
    [InlineData("_", false, null)]
    [InlineData("_count", false, null)]
    [InlineData("Count", true, "count")]
    [InlineData("count", true, null)]
    [InlineData("Int", true, null)]
    [InlineData("Var", true, null)]
    public void NewName(string name, bool camelCase, string? expected) =>
        Assert.Equal(expected, TupleElementNames.GetNewName(name, camelCase));

    [Fact]
    public Task Elements_AreRenamedWithEveryUse() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        public abstract class Base
        {
            public abstract (int {|BRO1311:count|}, string {|BRO1311:name|}) Get();
        }

        public class Derived : Base
        {
            private (int {|BRO1311:count|}, string Label) last;

            public override (int {|BRO1311:count|}, string {|BRO1311:name|}) Get() => (count: 1, name: "x");

            public int Use()
            {
                var t = Get();
                (int {|BRO1311:count|}, string {|BRO1311:name|}) copy = t;
                var list = new List<(int {|BRO1311:count|}, string {|BRO1311:name|})> { t };
                var (c, n) = t;
                last = (count: c, Label: n);
                var natural = ({|BRO1311:count|}: 2, {|BRO1311:other|}: 3);
                return t.count + copy.count + list[0].count + last.count + natural.count + nameof(t.name).Length
                    + (t is (count: 1, _) ? 1 : 0) + (list.Count > 0 ? list[0].name.Length : 0);
            }
        }
        """,
        """
        using System.Collections.Generic;

        public abstract class Base
        {
            public abstract (int Count, string Name) Get();
        }

        public class Derived : Base
        {
            private (int Count, string Label) last;

            public override (int Count, string Name) Get() => (Count: 1, Name: "x");

            public int Use()
            {
                var t = Get();
                (int Count, string Name) copy = t;
                var list = new List<(int Count, string Name)> { t };
                var (c, n) = t;
                last = (Count: c, Label: n);
                var natural = (Count: 2, Other: 3);
                return t.Count + copy.Count + list[0].Count + last.Count + natural.Count + nameof(t.Name).Length
                    + (t is (Count: 1, _) ? 1 : 0) + (list.Count > 0 ? list[0].Name.Length : 0);
            }
        }
        """);

    [Fact]
    public Task InferredNames_KeepTheirUses() => VerifyFixAsync(
        """
        public class C
        {
            private (int {|BRO1311:count|}, int Total) stats;

            public int M(int count)
            {
                var inferred = (count, 1);
                stats = (count: count, Total: 2);
                return inferred.count + stats.count;
            }
        }
        """,
        """
        public class C
        {
            private (int Count, int Total) stats;

            public int M(int count)
            {
                var inferred = (count, 1);
                stats = (Count: count, Total: 2);
                return inferred.count + stats.Count;
            }
        }
        """);

    [Fact]
    public Task CamelCase_FromEditorConfig() => VerifyFixAsync(
        """
        public class C
        {
            public (int {|BRO1311:Count|}, string name) M() => (Count: 1, name: "a");
        }
        """,
        """
        public class C
        {
            public (int count, string name) M() => (count: 1, name: "a");
        }
        """,
        editorConfig: "stylebro_tuple_element_name_casing = camelCase");

    // splat (2026-10-14): Splat.Core's private '(Type ServiceType, string Contract)' keys were renamed when 'format' ran
    // Splat.Core apart from Splat.SimpleInjector, whose own '(Type ServiceType, ...)' can't be renamed; loaded together,
    // that one kept them all. Each project's elements are judged on their own once the whole name can't be renamed.
    [Fact]
    public async Task ElementsInAnotherProject_ThatCantBeRenamed_DontKeepTheseNames()
    {
        const string App = """
            internal class Clash
            {
                private (int {|BRO1311:count|}, int Count) M() => (1, 2);

                private int N()
                {
                    var t = ({|BRO1311:count|}: 1, 2);
                    return t.count;
                }
            }
            """;
        var test = new CSharpCodeFixTest<TupleElementNamingAnalyzer, StyleBro.CodeFixes.Naming.TupleElementNamingCodeFixProvider, DefaultVerifier>
        {
            TestCode = """
                internal class Totals
                {
                    private (int {|BRO1311:count|}, int {|BRO1311:total|}) last;

                    public int Sum() => last.count + last.total;
                }
                """,
            FixedCode = """
                internal class Totals
                {
                    private (int Count, int Total) last;

                    public int Sum() => last.Count + last.Total;
                }
                """,
        };
        test.TestState.AdditionalProjects["App"].Sources.Add(App);
        test.FixedState.AdditionalProjects["App"].Sources.Add(App.Replace("{|BRO1311:count|}", "count", StringComparison.Ordinal));
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(StyleBro.Analyzers.DiagnosticIds.TupleElementCasing, DiagnosticSeverity.Warning).WithSpan("/App/Test0.cs", 3, 18, 3, 23).WithArguments("count", "Count"));
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(StyleBro.Analyzers.DiagnosticIds.TupleElementCasing, DiagnosticSeverity.Warning).WithSpan("/App/Test0.cs", 7, 18, 7, 23).WithArguments("count", "Count"));
        await test.RunAsync();
    }

    [Fact]
    public Task RenamesThatWouldNotCompile_AreLeftWithTheirWarning() => VerifyNotFixedAsync(
        new[]
        {
            """
            public class C
            {
                public (int {|BRO1311:count|}, int Count) M() => (1, 2);
            }
            """,
        });

    // sharpcompress (trial 18): the local 'pointer' kept IDE0042 (an error there) quiet; 'Pointer' would end the clash.
    [Theory]
    [InlineData("dotnet_diagnostic.IDE0042.severity = error")]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = warning")]
    [InlineData("dotnet_analyzer_diagnostic.severity = warning")]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = error\ndotnet_analyzer_diagnostic.severity = suggestion")]
    public Task RenamesThatLetIde0042Report_AreLeftWhereItIsEnforced(string editorConfig) => VerifyNotFixedAsync(
        new[]
        {
            """
            public class C
            {
                private static (int Code, string {|BRO1311:pointer|}) Decode() => (0, "x");

                public string Run()
                {
                    string pointer;
                    var result = Decode();
                    pointer = result.pointer;
                    return pointer + result.Code;
                }
            }
            """,
        },
        editorConfig);

    // The id wins over a category, a category over the bulk setting.
    [Theory]
    [InlineData("dotnet_diagnostic.IDE0042.severity = suggestion")]
    [InlineData("csharp_style_deconstructed_variable_declaration = false:error")]
    [InlineData("dotnet_diagnostic.IDE0042.severity = suggestion\ndotnet_analyzer_diagnostic.category-Style.severity = error")]
    [InlineData("dotnet_analyzer_diagnostic.category-Style.severity = suggestion\ndotnet_analyzer_diagnostic.severity = error")]
    public Task RenamesThatLetIde0042Report_AreMadeWhereItIsNotEnforced(string editorConfig) => VerifyFixAsync(
        """
        public class C
        {
            private static (int {|BRO1311:code|}, string {|BRO1311:pointer|}) Decode() => (0, "x");

            public string Run()
            {
                string pointer;
                var result = Decode();
                pointer = result.pointer;
                return pointer + result.code;
            }
        }
        """,
        """
        public class C
        {
            private static (int Code, string Pointer) Decode() => (0, "x");

            public string Run()
            {
                string pointer;
                var result = Decode();
                pointer = result.Pointer;
                return pointer + result.Code;
            }
        }
        """,
        editorConfig);

    // IDE0042's own conditions: a tuple used as a whole or by 'Item2' can't be deconstructed, a foreach variable can.
    [Theory]
    [InlineData("var result = Decode(); return pointer + result.pointer;", true)]
    [InlineData("foreach (var item in items) { pointer = item.pointer; } return pointer;", true)]
    [InlineData("var result = Decode(); return pointer + result;", false)]
    [InlineData("var result = Decode(); return pointer + result.Item2;", false)]
    public Task RenamesThatLetIde0042Report_FollowItsConditions(string body, bool kept)
    {
        var source = $$"""
            using System.Collections.Generic;

            public class C
            {
                private static (int Code, string {|BRO1311:pointer|}) Decode() => (0, "x");

                public string Run(List<(int Code, string {|BRO1311:pointer|})> items)
                {
                    string pointer = "";
                    {{body}}
                }
            }
            """;
        const string EditorConfig = "dotnet_diagnostic.IDE0042.severity = error";
        return kept
            ? VerifyNotFixedAsync(new[] { source }, EditorConfig)
            : VerifyFixAsync(source, source.Replace("{|BRO1311:pointer|}", "Pointer").Replace(".pointer", ".Pointer"), EditorConfig);
    }

    // Trial 20 (MeasureLower): 'pointer' is kept for IDE0042, so 'code' in the same tuple type keeps its name too, not '(int Code, string pointer)'.
    [Fact]
    public Task ElementsOfOneTupleType_AreRenamedAllOrNone() => VerifyNotFixedAsync(
        new[]
        {
            """
            public class C
            {
                private static (int {|BRO1311:code|}, string {|BRO1311:pointer|}) Decode() => (0, "x");

                public string Run()
                {
                    string pointer;
                    var result = Decode();
                    pointer = result.pointer;
                    return pointer + result.code;
                }
            }
            """,
        },
        "dotnet_diagnostic.IDE0042.severity = error");

    // 'Max' is left by the analyzer (disabled code mentions it), so 'total' next to it stays too.
    [Fact]
    public Task ElementsNextToANameInDisabledCode_KeepTheirNames() => VerifyNotFixedAsync(
        new[]
        {
            """
            public class C
            {
                private (int {|BRO1311:total|}, int max) M() => (1, 2);
            #if NEVER
                private int max;
            #endif
            }
            """,
        });

    // A partner with nothing to rename (only in a public signature) doesn't hold the group back.
    [Fact]
    public Task PartnersOnlyInPublicSignatures_DoNotKeepTheGroup() => VerifyFixAsync(
        """
        public class C
        {
            public (int total, int max) A() => (1, 2);

            private (int {|BRO1311:total|}, string {|BRO1311:name|}) B() => (1, "");
        }
        """,
        """
        public class C
        {
            public (int total, int max) A() => (1, 2);

            private (int Total, string Name) B() => (1, "");
        }
        """,
        "stylebro_rename_public_api = false");

    // Coverlet (trial 20): a use inside a tuple literal converted to another tuple type ('return (false, x.documentName)')
    // names nothing there, so writing the old name out ('documentName: ...') only added CS8123 and the rename was dropped.
    [Fact]
    public Task UsesInConvertedTupleLiterals_AreRenamedWithoutWritingTheNameOut() => VerifyFixAsync(
        """
        using System.Collections.Generic;
        using System.Linq;

        internal class C
        {
            private IEnumerable<(string {|BRO1311:documentName|}, bool DocumentExists)> Map(string[] names) => names.Select(n => (n, n.Length > 0));

            public (bool AllMatch, string NotFound) Check(string[] names)
            {
                var map = Map(names).ToList();
                var natural = (true, map[0].documentName);
                return (natural.documentName == Map(names).First().documentName, map.FirstOrDefault(x => !x.DocumentExists).documentName);
            }
        }
        """,
        """
        using System.Collections.Generic;
        using System.Linq;

        internal class C
        {
            private IEnumerable<(string DocumentName, bool DocumentExists)> Map(string[] names) => names.Select(n => (n, n.Length > 0));

            public (bool AllMatch, string NotFound) Check(string[] names)
            {
                var map = Map(names).ToList();
                var natural = (true, map[0].DocumentName);
                return (natural.DocumentName == Map(names).First().DocumentName, map.FirstOrDefault(x => !x.DocumentExists).DocumentName);
            }
        }
        """);

    // Owner's choice 2026-10-14: code that turns IDE1006 off keeps its names, tuple elements too.
    [Fact]
    public Task WhereIde1006IsSuppressed_ElementsKeepTheirNames() => VerifyNoDiagnosticsAsync(
        """
        public class C
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006")]
            private (int count, int total) M() => (1, 2);

        #pragma warning disable IDE1006
            private (int count, int total) N() => (1, 2);
        }
        """);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync("""
        using System;
        using System.Collections.Generic;

        public interface IShape
        {
            (int Width, int Height) Size();
        }

        public class Box : IShape
        {
            public (int Width, int Height) Size() => (1, 2);

            public (int _, string Name) Discard() => (1, "x");

            public (int, string) Unnamed() => (1, "x");
        }
        """);

    [Fact]
    public Task TypeArgumentsOfLibraryInterfaces_AreRenamedWithTheImplementation() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        public class ByFirst : IComparer<(int {|BRO1311:a|}, int {|BRO1311:b|})>
        {
            public int Compare((int {|BRO1311:a|}, int {|BRO1311:b|}) x, (int {|BRO1311:a|}, int {|BRO1311:b|}) y) => x.a - y.a;
        }
        """,
        """
        using System.Collections.Generic;

        public class ByFirst : IComparer<(int A, int B)>
        {
            public int Compare((int A, int B) x, (int A, int B) y) => x.A - y.A;
        }
        """);

    [Fact]
    public Task Renames_ReachOtherFiles_ArgumentsAndAsyncReturns() => VerifyFixAsync(
        new[]
        {
            """
            using System.Threading.Tasks;

            public static class Api
            {
                public static async Task<(int {|BRO1311:id|}, string Text)> LoadAsync() { await Task.Yield(); return (id: 1, Text: "a"); }

                public static int Save((int {|BRO1311:id|}, string Text) item) => item.id;
            }
            """,
            """
            public static class Caller
            {
                public static async System.Threading.Tasks.Task<int> Run()
                {
                    var item = await Api.LoadAsync();
                    return item.id + Api.Save((id: 2, Text: "b"));
                }
            }
            """,
        },
        new[]
        {
            """
            using System.Threading.Tasks;

            public static class Api
            {
                public static async Task<(int Id, string Text)> LoadAsync() { await Task.Yield(); return (Id: 1, Text: "a"); }

                public static int Save((int Id, string Text) item) => item.Id;
            }
            """,
            """
            public static class Caller
            {
                public static async System.Threading.Tasks.Task<int> Run()
                {
                    var item = await Api.LoadAsync();
                    return item.Id + Api.Save((Id: 2, Text: "b"));
                }
            }
            """,
        });

    // A tuple literal's element named by a renamed use is renamed with it, and so are its uses (written out with the old
    // name it would be an explicit name in the wrong case, reported in the next run). An anonymous type's property isn't
    // a tuple element: its name is written out and stays.
    [Fact]
    public Task NamesInferredFromAUse_AreWrittenOut() => VerifyFixAsync(
        """
        public class C
        {
            public int M((int {|BRO1311:first|}, int Second) pair)
            {
                var inferred = (pair.first, 2);
                var anonymous = new { pair.first };
                return inferred.first + anonymous.first;
            }
        }
        """,
        """
        public class C
        {
            public int M((int First, int Second) pair)
            {
                var inferred = (pair.First, 2);
                var anonymous = new { first = pair.First };
                return inferred.First + anonymous.first;
            }
        }
        """);

    [Fact]
    public Task OverridesOfSolutionMembers_AreReportedAndRenamedTogether() => VerifyFixAsync(
        """
        public interface IShape
        {
            (int {|BRO1311:width|}, int {|BRO1311:height|}) Size();
        }

        public class Box : IShape
        {
            public (int {|BRO1311:width|}, int {|BRO1311:height|}) Size() => (width: 1, height: 2);
        }
        """,
        """
        public interface IShape
        {
            (int Width, int Height) Size();
        }

        public class Box : IShape
        {
            public (int Width, int Height) Size() => (Width: 1, Height: 2);
        }
        """);

    [Fact]
    public async Task NamesForcedByALibraryMember_AreNotReported()
    {
        // A referenced assembly whose interface names the tuple elements: the implementation has to use the same names.
        var library = CSharpCompilation.Create(
            "Library",
            new[] { CSharpSyntaxTree.ParseText("public interface IScores { (int count, int total) Get(); }") },
            await ReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, CancellationToken.None),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        Assert.True(library.Emit(image).Success);

        var test = new CSharpCodeFixTest<TupleElementNamingAnalyzer, StyleBro.CodeFixes.Naming.TupleElementNamingCodeFixProvider, DefaultVerifier>
        {
            TestCode = """
                public class Scores : IScores
                {
                    public (int count, int total) Get() => (1, 2);
                }
                """,
        };
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(image.ToArray()));
        test.TestState.AnalyzerConfigFiles.Add(PublicApi.RenameEverything);
        await test.RunAsync();
    }

    [Fact]
    public Task PublicSignatures_AreLeftAloneByDefault() => VerifyFixAsync(
        """
        public class Scores
        {
            public (int count, int total) Get() => (1, 2);

            private (int {|BRO1311:count|}, int {|BRO1311:total|}) Sum() => (1, 2);

            public int Total() => Get().count + Sum().count;
        }
        """,
        """
        public class Scores
        {
            public (int count, int total) Get() => (1, 2);

            private (int Count, int Total) Sum() => (1, 2);

            public int Total() => Get().count + Sum().Count;
        }
        """,
        "stylebro_rename_public_api = false");

    // Trial 26 (Serde): a public interface keeps its names by default, so an internal implementer has to keep them too;
    // it was reported, and the fix then kept it every time ("would add compile errors").
    [Fact]
    public Task NamesForcedByAPublicInterface_AreNotReportedByDefault() => VerifyNoDiagnosticsAsync(
        """
        public interface IReader
        {
            (int, string? errorName) TryRead();
        }

        internal sealed class Reader : IReader
        {
            public (int, string? errorName) TryRead() => (0, null);
        }
        """,
        "stylebro_rename_public_api = false");

    // Trial 25 (Flow Launcher): a public conversion operator's tuple was renamed.
    [Fact]
    public Task PublicOperatorSignatures_AreLeftAloneByDefault() => VerifyNoDiagnosticsAsync(
        """
        public class Pair
        {
            public static implicit operator (string key, string value)(Pair pair) => ("a", "b");

            public static explicit operator Pair((int key, int value) pair) => new Pair();

            public static (int low, int high) operator +(Pair a, Pair b) => (1, 2);

            public static bool operator ==(Pair a, (int low, int high) b) => true;

            public static bool operator !=(Pair a, (int low, int high) b) => false;

            public override bool Equals(object obj) => false;

            public override int GetHashCode() => 0;
        }

        public record Entry((int key, int value) Pair);

        public class Holder((int key, int value) pair) : System.IEquatable<(int key, int value)>
        {
            public bool Equals((int key, int value) other) => other.key == pair.key;
        }

        public class Store<T>
            where T : System.IEquatable<(int key, int value)>
        {
        }

        namespace System.Runtime.CompilerServices
        {
            internal static class IsExternalInit
            {
            }
        }
        """,
        "stylebro_rename_public_api = false");

    // A use in code excluded by '#if' isn't bound, so the rename can't follow it.
    [Fact]
    public Task NamesInDisabledCode_AreSkipped() => VerifyNoDiagnosticsAsync(
        """
        internal class Scores
        {
            private (int count, int Total) Sum() => (1, 2);

        #if NEVER
            private int Total() => Sum().count;
        #endif
        }
        """);

    // StyleCop 1.3's SA1316 (issue 338): explicit names in tuple literals are always checked. A literal that keeps its
    // own type declares the element: the name is renamed with every use bound to it.
    [Fact]
    public Task NamesInTupleLiterals_AreRenamedWithTheirUses() => VerifyFixAsync(
        """
        using System.Collections.Generic;
        using System.Linq;

        internal class C
        {
            public int M(int[] xs, bool flag)
            {
                var t = ({|BRO1311:lower|}: 1, 2);
                var copy = t;
                var pairs = xs.Select(x => ({|BRO1311:lower|}: x, {|BRO1311:upper|}: x + 1)).Where(p => p.lower > 0).ToList();
                var array = new[] { ({|BRO1311:lower|}: 1, Size: 2), ({|BRO1311:lower|}: 3, Size: 4) };
                var either = flag ? ({|BRO1311:lower|}: 1, 2) : ({|BRO1311:lower|}: 3, 4);
                var picked = flag switch { true => ({|BRO1311:lower|}: 1, 2), false => ({|BRO1311:lower|}: 3, 4) };
                object boxed = ({|BRO1311:lower|}: 1, 2);
                var name = nameof(t.lower);
                return t.lower + copy.lower + pairs[0].lower + pairs[0].upper + array[0].lower + either.lower + picked.lower
                    + (t is (lower: 1, _) ? 1 : 0) + Twice(({|BRO1311:lower|}: 1, 2)).lower + name.Length + boxed.GetHashCode()
                    + (({|BRO1311:lower|}: 1, 2) == ({|BRO1311:lower|}: 3, 4) ? 1 : 0);
            }

            private static T Twice<T>(T value) => value;
        }
        """,
        """
        using System.Collections.Generic;
        using System.Linq;

        internal class C
        {
            public int M(int[] xs, bool flag)
            {
                var t = (Lower: 1, 2);
                var copy = t;
                var pairs = xs.Select(x => (Lower: x, Upper: x + 1)).Where(p => p.Lower > 0).ToList();
                var array = new[] { (Lower: 1, Size: 2), (Lower: 3, Size: 4) };
                var either = flag ? (Lower: 1, 2) : (Lower: 3, 4);
                var picked = flag switch { true => (Lower: 1, 2), false => (Lower: 3, 4) };
                object boxed = (Lower: 1, 2);
                var name = nameof(t.Lower);
                return t.Lower + copy.Lower + pairs[0].Lower + pairs[0].Upper + array[0].Lower + either.Lower + picked.Lower
                    + (t is (Lower: 1, _) ? 1 : 0) + Twice((Lower: 1, 2)).Lower + name.Length + boxed.GetHashCode()
                    + ((Lower: 1, 2) == (Lower: 3, 4) ? 1 : 0);
            }

            private static T Twice<T>(T value) => value;
        }
        """);

    // A name inferred from a renamed use is the element's new name: its uses follow, through as many literals as it takes.
    [Fact]
    public Task NamesInTupleLiterals_RenameTheElementsInferredFromTheirUses() => VerifyFixAsync(
        """
        internal class C
        {
            public int M()
            {
                var t = ({|BRO1311:lower|}: 1, 2);
                var second = (t.lower, 5);
                var third = (6, second.lower);
                var named = (Kept: third.lower, 7);
                var anonymous = new { third.lower };
                return second.lower + third.lower + named.Kept + anonymous.lower;
            }
        }
        """,
        """
        internal class C
        {
            public int M()
            {
                var t = (Lower: 1, 2);
                var second = (t.Lower, 5);
                var third = (6, second.Lower);
                var named = (Kept: third.Lower, 7);
                var anonymous = new { lower = third.Lower };
                return second.Lower + third.Lower + named.Kept + anonymous.lower;
            }
        }
        """);

    // The compiler ignores a name the target type names differently or not at all (CS8123): nothing binds to it, only
    // the name changes.
    [Fact]
    public Task NamesInTupleLiterals_TheTargetTypeIgnores_AreRenamed() => VerifyFixAsync(
        """
        internal class C
        {
            private (int A, int B) field = ({|BRO1311:lower|}: 1, 2);

            public (int, int) M()
            {
                (int First, int Second) local = ({|BRO1311:lower|}: 1, {|BRO1311:upper|}: 2);
                System.ValueTuple<int, int> plain = ({|BRO1311:lower|}: 3, 4);
                Take(({|BRO1311:lower|}: 5, 6));
                return ({|BRO1311:lower|}: local.First + plain.Item1 + field.A, 8);
            }

            private static void Take((long Wide, long Other) value)
            {
            }
        }
        """,
        """
        internal class C
        {
            private (int A, int B) field = (Lower: 1, 2);

            public (int, int) M()
            {
                (int First, int Second) local = (Lower: 1, Upper: 2);
                System.ValueTuple<int, int> plain = (Lower: 3, 4);
                Take((Lower: 5, 6));
                return (Lower: local.First + plain.Item1 + field.A, 8);
            }

            private static void Take((long Wide, long Other) value)
            {
            }
        }
        """);

    // A name that follows a declared tuple type's is that declaration's: reported there, and renamed with it.
    [Fact]
    public Task NamesInTupleLiterals_ThatFollowADeclaredTupleType_AreRenamedWithIt() => VerifyFixAsync(
        """
        using System.Collections.Generic;

        internal class C
        {
            private (int {|BRO1311:lower|}, int B)? maybe = (lower: 1, 2);
            private (int {|BRO1311:lower|}, int B)[] array = { (lower: 1, 2) };
            private System.Func<int, (int {|BRO1311:lower|}, int B)> make = x => (lower: x, 2);

            public IEnumerable<(int {|BRO1311:lower|}, int B)> Items()
            {
                yield return (lower: 1, 2);
            }

            public int M(bool flag)
            {
                (int {|BRO1311:lower|}, long B) wide = (lower: 1, 2);
                (int {|BRO1311:lower|}, int B) either = flag ? ({|BRO1311:lower|}: 1, 2) : ({|BRO1311:lower|}: 3, 4);
                return wide.lower + either.lower + Take((lower: 5, 6)) + maybe.Value.lower + array[0].lower + make(1).lower
                    + (wide == (lower: 1, 2) ? 1 : 0) + ((lower: 1, 2L) != wide ? 1 : 0);
            }

            private static int Take((int {|BRO1311:lower|}, int B) value) => value.lower;
        }
        """,
        """
        using System.Collections.Generic;

        internal class C
        {
            private (int Lower, int B)? maybe = (Lower: 1, 2);
            private (int Lower, int B)[] array = { (Lower: 1, 2) };
            private System.Func<int, (int Lower, int B)> make = x => (Lower: x, 2);

            public IEnumerable<(int Lower, int B)> Items()
            {
                yield return (Lower: 1, 2);
            }

            public int M(bool flag)
            {
                (int Lower, long B) wide = (Lower: 1, 2);
                (int Lower, int B) either = flag ? (Lower: 1, 2) : (Lower: 3, 4);
                return wide.Lower + either.Lower + Take((Lower: 5, 6)) + maybe.Value.Lower + array[0].Lower + make(1).Lower
                    + (wide == (Lower: 1, 2) ? 1 : 0) + ((Lower: 1, 2L) != wide ? 1 : 0);
            }

            private static int Take((int Lower, int B) value) => value.Lower;
        }
        """);

    // The declared type keeps its names (public API, IDE1006 suppressed, a library's): a literal's name that follows it
    // has to stay too (CS8123 otherwise).
    [Fact]
    public Task NamesInTupleLiterals_ThatFollowADeclaredTupleTypeThatKeepsItsNames_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        using System.Collections.Generic;

        public class C
        {
            public (int lower, int B) Field = (lower: 1, 2);

            public (int lower, int B) Get() => (lower: 1, 2);

            public IEnumerable<(int lower, int B)> Items()
            {
                yield return (lower: 1, 2);
            }

            public void Set((int lower, int B)? value = null) => Set((lower: 1, 2));

            public bool Same((int lower, int B) value) => value == (lower: 1, 2) || (lower: 1, 2) != value;

            public KeyValuePair<string, (int lower, int B)> Pair() => new("a", (lower: 1, 2));
        }
        """,
        "stylebro_rename_public_api = false");

    // A literal follows its own target type: renaming one declaration's element leaves the literal of another one,
    // which keeps its name (public API here), alone.
    [Fact]
    public Task NamesInTupleLiterals_ThatFollowAnotherDeclaration_KeepTheirNames() => VerifyFixAsync(
        """
        public class C
        {
            public (int lower, int B) Kept() => (lower: 1, 2);

            private (int {|BRO1311:lower|}, int B) Mine() => (lower: 1, 2);
        }
        """,
        """
        public class C
        {
            public (int lower, int B) Kept() => (lower: 1, 2);

            private (int Lower, int B) Mine() => (Lower: 1, 2);
        }
        """,
        "stylebro_rename_public_api = false");

    // Another element has the new name already (a second one is an error, or takes an inferred name away so that
    // 't.Lower' would mean the other element), or the new name is one of the tuple's own members.
    [Fact]
    public Task NamesInTupleLiterals_WhoseNewNameIsTaken_AreNotReported() => VerifyNoDiagnosticsAsync(
        """
        internal class C
        {
            public int M((int Lower, int Other) other)
            {
                var twice = (lower: 1, Lower: 2);
                var inferred = (lower: 1, other.Lower);
                var local = 3;
                var Lower = 4;
                var simple = (lower: local, Lower);
                var members = (rest: 1, item1: 2, toString: 3, 4);
                var discard = (_: 1, _2: 2, Fine: 3);
                return twice.lower + inferred.lower + inferred.Lower + simple.lower + simple.Lower + members.rest + members.item1 + discard.Fine;
            }
        }
        """);

    // 'item1' is the first element's own name in the right case, and fine for it.
    [Fact]
    public Task NamesInTupleLiterals_ItemNamesAtTheirOwnPosition_AreRenamed() => VerifyFixAsync(
        """
        internal class C
        {
            public int M()
            {
                var t = ({|BRO1311:item1|}: 1, Other: 2);
                return t.item1;
            }
        }
        """,
        """
        internal class C
        {
            public int M()
            {
                var t = (Item1: 1, Other: 2);
                return t.Item1;
            }
        }
        """);

    // A use that names an element next to one with the new name: the old name is written out (the inferred 'First' would
    // be lost, and 'pair.First' would mean the other element), and the rule leaves it (its new name is taken).
    [Fact]
    public Task NamesInferredFromAUse_NextToTheNewName_AreWrittenOut() => VerifyFixAsync(
        """
        internal class C
        {
            public int M((int {|BRO1311:first|}, int Second) pair)
            {
                var inferred = (pair.first, First: 2);
                var unnamed = (1 + pair.first, First: 3);
                (int {|BRO1311:first|}, int Other) declared = (pair.first, First: 4);
                return inferred.first + inferred.First + unnamed.First + declared.first;
            }
        }
        """,
        """
        internal class C
        {
            public int M((int First, int Second) pair)
            {
                var inferred = (first: pair.First, First: 2);
                var unnamed = (1 + pair.First, First: 3);
                (int First, int Other) declared = (pair.First, First: 4);
                return inferred.first + inferred.First + unnamed.First + declared.First;
            }
        }
        """);

    // The name is renamed where it can be; a literal where the new name is taken keeps it, with its uses.
    [Fact]
    public Task NamesInTupleLiterals_WhoseNewNameIsTaken_StayWhenTheNameIsRenamedElsewhere() => VerifyFixAsync(
        """
        internal class C
        {
            public int M()
            {
                var free = ({|BRO1311:lower|}: 1, 2);
                var taken = (lower: 3, Lower: 4);
                return free.lower + taken.lower + taken.Lower;
            }
        }
        """,
        """
        internal class C
        {
            public int M()
            {
                var free = (Lower: 1, 2);
                var taken = (lower: 3, Lower: 4);
                return free.Lower + taken.lower + taken.Lower;
            }
        }
        """);

    // A literal compared with one that keeps the old name: the rename would add CS8383 (the name is ignored), so it's left.
    [Fact]
    public Task NamesInTupleLiterals_WhoseRenameAddsAnIgnoredNameWarning_AreLeftWithTheirWarning() => VerifyNotFixedAsync(
        new[]
        {
            """
            internal class C
            {
                public bool M() => ({|BRO1311:lower|}: 1, 2) == (lower: 3, Lower: 4);
            }
            """,
        });

    [Fact]
    public Task NamesInTupleLiterals_CamelCase_FromEditorConfig() => VerifyFixAsync(
        """
        internal class C
        {
            public int M()
            {
                var t = ({|BRO1311:Lower|}: 1, upper: 2);
                return t.Lower + t.upper;
            }
        }
        """,
        """
        internal class C
        {
            public int M()
            {
                var t = (lower: 1, upper: 2);
                return t.lower + t.upper;
            }
        }
        """,
        editorConfig: "stylebro_tuple_element_name_casing = camelCase");

    // The same guards as for tuple types: IDE1006 suppressed, and a name in code excluded by '#if'.
    [Fact]
    public Task NamesInTupleLiterals_WhereIde1006IsSuppressed_OrInDisabledCode_KeepTheirNames() => VerifyNoDiagnosticsAsync(
        """
        internal class C
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006")]
            private int M()
            {
                var t = (lower: 1, 2);
                return t.lower;
            }

            private int N()
            {
                var t = (hidden: 1, 2);
        #if NEVER
                return t.hidden + 1;
        #else
                return t.hidden;
        #endif
            }
        }
        """);
}
