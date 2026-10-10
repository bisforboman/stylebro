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
                var natural = (count: 2, other: 3);
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
                var natural = (count: 2, other: 3);
                return t.Count + copy.Count + list[0].Count + last.Count + natural.count + nameof(t.Name).Length
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
                var inferred = (first: pair.First, 2);
                var anonymous = new { first = pair.First };
                return inferred.first + anonymous.first;
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
}
