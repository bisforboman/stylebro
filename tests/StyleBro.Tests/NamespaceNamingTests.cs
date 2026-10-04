using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using StyleBro.Analyzers.Naming;
using StyleBro.CodeFixes.Naming;
using static StyleBro.Tests.Verifier<StyleBro.Analyzers.Naming.NamespaceNamingAnalyzer, StyleBro.CodeFixes.Naming.CamelCaseNamingCodeFixProvider>;

namespace StyleBro.Tests;

public class NamespaceNamingTests
{
    private const string On = "dotnet_diagnostic.BRO1312.severity = warning";

    [Fact]
    public Task EveryDeclarationAndReference_IsRenamed() => VerifyFixAsync(
        new[]
        {
            """
            namespace {|BRO1312:myCompany|}.{|BRO1312:data|}
            {
                /// <summary>See <see cref="myCompany.data.Helpers"/>.</summary>
                public class Foo
                {
                }

                public static class Helpers
                {
                    public static int Two() => 2;
                }
            }
            """,
            """
            using myCompany.data;
            using static myCompany.data.Helpers;
            using D = myCompany.data;

            namespace {|BRO1312:myCompany|}.{|BRO1312:web|};

            public class Page
            {
                private Foo a = new D.Foo();
                private global::myCompany.data.Foo b = new data.Foo();
                private int c = Two() + myCompany.data.Helpers.Two();
            }
            """,
            """
            global using myCompany.data;
            """,
        },
        new[]
        {
            """
            namespace MyCompany.Data
            {
                /// <summary>See <see cref="MyCompany.Data.Helpers"/>.</summary>
                public class Foo
                {
                }

                public static class Helpers
                {
                    public static int Two() => 2;
                }
            }
            """,
            """
            using MyCompany.Data;
            using static MyCompany.Data.Helpers;
            using D = MyCompany.Data;

            namespace MyCompany.Web;

            public class Page
            {
                private Foo a = new D.Foo();
                private global::MyCompany.Data.Foo b = new Data.Foo();
                private int c = Two() + MyCompany.Data.Helpers.Two();
            }
            """,
            """
            global using MyCompany.Data;
            """,
        },
        On);

    [Fact]
    public Task NotReported() => VerifyNoDiagnosticsAsync(
        """
        namespace eBay.Api
        {
        }

        namespace Upper.Case
        {
        }

        namespace Taken
        {
        }

        namespace taken.Inner
        {
        }

        namespace _1
        {
        }
        """,
        On + "\nstylebro_allowed_namespace_components = eBay");

    [Fact]
    public Task TheRootNamespace_IsNotReported() => VerifyFixAsync(
        """
        namespace app.core.Models
        {
        }

        namespace app.{|BRO1312:tools|}
        {
        }
        """,
        """
        namespace app.core.Models
        {
        }

        namespace app.Tools
        {
        }
        """,
        On + "\nbuild_property.RootNamespace = app.core");

    [Fact]
    public async Task ANamespaceDeclaredInGeneratedCode_IsNotReported()
    {
        var test = new CSharpCodeFixTest<NamespaceNamingAnalyzer, CamelCaseNamingCodeFixProvider, DefaultVerifier>();
        test.TestState.Sources.Add(("/0/Test0.cs", "namespace gen.Models\n{\n    public class A\n    {\n    }\n}\n"));
        test.TestState.Sources.Add(("/0/Strings.Designer.cs", "namespace gen.Models\n{\n    public class B\n    {\n    }\n}\n"));
        await test.RunAsync();
    }

    [Fact]
    public Task TheNameInAString_KeepsTheNamespace() => VerifyNotFixedAsync(
        new[]
    {
        """
        namespace {|BRO1312:myApp|}.Models
        {
            public class C
            {
                public static System.Type Find() => System.Type.GetType("myApp.Models.C, App");
            }
        }
        """,
    },
        On);

    [Fact]
    public Task TheNameInDisabledCode_KeepsTheNamespace() => VerifyNotFixedAsync(
        new[]
    {
        """
        #if NEVER
        using myApp.Models;
        #endif

        namespace {|BRO1312:myApp|}.Models
        {
        }
        """,
    },
        On);

    [Fact]
    public Task TheNewNameMeaningATypeInside_KeepsTheNamespace() => VerifyNotFixedAsync(
        new[]
    {
        """
        namespace Other
        {
            public class Data
            {
            }
        }

        namespace App.{|BRO1312:data|}
        {
            public class Foo
            {
            }
        }

        namespace App.Web
        {
            using Other;

            public class Page
            {
                public Data Current { get; set; }
            }
        }
        """,
    },
        On);

    [Fact]
    public Task TheNewNameInScopeAtAReference_KeepsTheNamespace() => VerifyNotFixedAsync(
        new[]
    {
        """
        namespace App.{|BRO1312:data|}
        {
            public class Foo
            {
            }
        }

        namespace App.Web
        {
            public class Page
            {
                public int Data { get; set; }

                public data.Foo Current { get; set; }
            }
        }
        """,
    },
        On);

    [Fact]
    public async Task AProjectReference_IsRenamedWithIt()
    {
        // The namespace spans two projects: the library reports it (only its own source declares it there; the test
        // framework analyzes only this project), the app wouldn't (the library's part comes from another assembly), and
        // the rename covers both.
        const string App = """
            using shared.Lib;

            namespace shared.App
            {
                public class Page
                {
                    public Widget Widget { get; } = new Widget();
                }
            }
            """;
        var test = new CSharpCodeFixTest<NamespaceNamingAnalyzer, CamelCaseNamingCodeFixProvider, DefaultVerifier>
        {
            TestCode = "namespace {|BRO1312:shared|}.Lib\n{\n    public class Widget\n    {\n    }\n}\n",
            FixedCode = "namespace Shared.Lib\n{\n    public class Widget\n    {\n    }\n}\n",
        };
        test.TestState.AdditionalProjects["App"].Sources.Add(App);
        test.TestState.AdditionalProjects["App"].AdditionalProjectReferences.Add("TestProject");
        test.FixedState.AdditionalProjects["App"].Sources.Add(App.Replace("shared.", "Shared.", StringComparison.Ordinal));
        test.FixedState.AdditionalProjects["App"].AdditionalProjectReferences.Add("TestProject");
        await test.RunAsync();
    }

    [Fact]
    public async Task ANamespaceALibraryAlsoDeclares_IsNotReported()
    {
        var library = CSharpCompilation.Create(
            "Vendor",
            new[] { CSharpSyntaxTree.ParseText("namespace vendor.Tools { public class Tool { } }") },
            await ReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, CancellationToken.None),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        Assert.True(library.Emit(image).Success);

        var test = new CSharpCodeFixTest<NamespaceNamingAnalyzer, CamelCaseNamingCodeFixProvider, DefaultVerifier>
        {
            TestCode = "namespace vendor.Mine\n{\n}\n",
        };
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(image.ToArray()));
        await test.RunAsync();
    }

    [Fact]
    public Task TheNameInAnAdditionalFile_KeepsTheNamespace() => RunNotFixedAsync(state =>
    {
        state.Sources.Add("namespace {|BRO1312:myApp|}.Models\n{\n}\n");
        state.AdditionalFiles.Add(("/0/Page.razor", "@using myApp.Models\n"));
    });

    [Fact]
    public async Task TheAssemblyNameInGeneratedAssemblyInfo_DoesNotKeepTheNamespace()
    {
        // The SDK's generated AssemblyInfo names the assembly, which usually is the root namespace.
        const string AssemblyInfo = "// <auto-generated/>\n[assembly: System.Reflection.AssemblyTitle(\"myApp.Models\")]\n";
        var test = new CSharpCodeFixTest<NamespaceNamingAnalyzer, CamelCaseNamingCodeFixProvider, DefaultVerifier>();
        test.TestState.Sources.Add(("/0/Test0.cs", "namespace {|BRO1312:myApp|}.Models\n{\n}\n"));
        test.TestState.Sources.Add(("/0/App.AssemblyInfo.cs", AssemblyInfo));
        test.FixedState.Sources.Add(("/0/Test0.cs", "namespace MyApp.Models\n{\n}\n"));
        test.FixedState.Sources.Add(("/0/App.AssemblyInfo.cs", AssemblyInfo));
        await test.RunAsync();
    }

    [Fact]
    public Task AReferenceInGeneratedCode_KeepsTheNamespace() => RunNotFixedAsync(state =>
    {
        state.Sources.Add(("/0/Test0.cs", "namespace {|BRO1312:myApp|}.Models\n{\n    public class A\n    {\n    }\n}\n"));
        state.Sources.Add(("/0/Page.g.cs", "using myApp.Models;\n\npublic class Page\n{\n    public A Model { get; set; }\n}\n"));
    });

    [Fact]
    public async Task AnotherProjectsPartFromALibrary_KeepsTheNamespace()
    {
        // The app also gets 'shared.Vendor' from a library outside the solution: its 'using shared.Vendor;' must stay.
        var library = CSharpCompilation.Create(
            "Vendor",
            new[] { CSharpSyntaxTree.ParseText("namespace shared.Vendor { public class Tool { } }") },
            await ReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, CancellationToken.None),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        Assert.True(library.Emit(image).Success);
        await RunNotFixedAsync(state =>
        {
            state.Sources.Add("namespace {|BRO1312:shared|}.Lib\n{\n}\n");
            state.AdditionalProjects["App"].Sources.Add("using shared.Vendor;\n\npublic class Page\n{\n    public Tool Tool { get; set; }\n}\n");
            state.AdditionalProjects["App"].AdditionalProjectReferences.Add("TestProject");
            state.AdditionalProjects["App"].AdditionalReferences.Add(MetadataReference.CreateFromImage(image.ToArray()));
        });
    }

    [Fact]
    public Task TheNewNameTakenInAnotherProject_KeepsTheNamespace() => RunNotFixedAsync(state =>
    {
        state.Sources.Add("namespace {|BRO1312:shared|}.Lib\n{\n}\n");
        state.AdditionalProjects["App"].Sources.Add("namespace Shared.App\n{\n}\n");
        state.AdditionalProjects["App"].AdditionalProjectReferences.Add("TestProject");
    });

    /// <summary>The diagnostics are reported, but the rename is skipped: the same sources before and after.</summary>
    private static Task RunNotFixedAsync(Action<SolutionState> fill)
    {
        var test = new CSharpCodeFixTest<NamespaceNamingAnalyzer, CamelCaseNamingCodeFixProvider, DefaultVerifier>
        {
            NumberOfIncrementalIterations = 1,
            NumberOfFixAllIterations = 1,
            CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck,
        };
        fill(test.TestState);
        fill(test.FixedState);
        return test.RunAsync();
    }
}
