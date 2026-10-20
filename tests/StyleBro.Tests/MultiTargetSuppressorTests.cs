using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers.Modernize;

namespace StyleBro.Tests;

public class MultiTargetSuppressorTests
{
    [Theory]
    [InlineData("net48,net8.0", "CA1510", true)]
    [InlineData("netstandard2.0,net8.0", "CA1510", true)]
    [InlineData("net6.0,net8.0", "CA1510", false)]
    [InlineData("net6.0,net8.0", "CA1512", true)]
    [InlineData("net8.0-windows,net10.0", "CA1512", false)]
    [InlineData("netstandard2.1,net8.0", "CA1847", false)]
    [InlineData("netstandard2.1,net8.0", "CA1510", true)]
    [InlineData("netcoreapp3.1,net8.0", "IDE0057", false)]
    [InlineData("netcoreapp2.1,net8.0", "IDE0057", true)]
    [InlineData("net8.0,net10.0", "IDE0330", true)]
    [InlineData("net9.0,net10.0", "IDE0330", false)]
    [InlineData("monoandroid10.0,net8.0", "CA1847", true)]
    [InlineData("net8.0", "CA1510", false)]
    [InlineData("net48", "CA1510", false)]
    [InlineData("", "CA1510", false)]
    [InlineData(null, "CA1510", false)]
    [InlineData("net48,net8.0", "CA1825", false)]
    public async Task Suppresses_WhenAFrameworkOfAMultiTargetedProjectLacksTheApi(string? frameworks, string id, bool suppressed)
    {
        var diagnostic = Assert.Single(await RunAsync(id, frameworks));

        Assert.Equal(suppressed, diagnostic.IsSuppressed);
    }

    [Theory]
    [InlineData("net48", false)]
    [InlineData("net4.8", false)]
    [InlineData("net5.0", false)]
    [InlineData("net6.0", true)]
    [InlineData("NET6.0", true)]
    [InlineData("net10.0-windows10.0.19041", true)]
    [InlineData("netcoreapp3.1", false)]
    [InlineData("netstandard2.1", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("net", false)]
    [InlineData("xamarinios", false)]
    public void Has_ParsesFrameworkNames(string framework, bool has)
    {
        Assert.Equal(has, MultiTargetSuppressor.Has(framework, MultiTargetSuppressor.Minimums["CA1510"]));
    }

    [Fact]
    public void Has_ComparesNetStandardAndCoreVersions()
    {
        var minimum = MultiTargetSuppressor.Minimums["IDE0057"];

        Assert.True(MultiTargetSuppressor.Has("netstandard2.1", minimum));
        Assert.False(MultiTargetSuppressor.Has("netstandard2.0", minimum));
        Assert.True(MultiTargetSuppressor.Has("netcoreapp3.0", minimum));
        Assert.False(MultiTargetSuppressor.Has("netcoreapp2.2", minimum));
        Assert.True(MultiTargetSuppressor.Has("net5.0", minimum));
        Assert.False(MultiTargetSuppressor.Has("net4.8", minimum));
    }

    [Theory]
    [InlineData("netstandard2.0,net8.0", null, "IntPtr", true)]
    [InlineData("netstandard2.0,net8.0", null, "System.UIntPtr", true)]
    [InlineData("netstandard2.0,net8.0", null, "Int32", false)]
    [InlineData("netstandard2.0,net8.0", null, "alias", true)]
    [InlineData("net6.0,net8.0", null, "IntPtr", false)]
    [InlineData("net6.0,net8.0", null, "alias", true)]
    [InlineData("net8.0,net10.0", null, "alias", false)]
    [InlineData("netstandard2.0,net8.0", "12.0", "alias", false)]
    [InlineData("netstandard2.0,net8.0", "7.3", "IntPtr", true)]
    [InlineData("netstandard2.0", null, "IntPtr", false)]
    [InlineData(null, null, "alias", false)]
    public async Task SuppressesTypeNames_WhereAFrameworksDefaultCSharpIsTooOld(string? frameworks, string? langVersion, string name, bool suppressed)
    {
        // sqlite-net: 'using Sqlite3DatabaseHandle = System.IntPtr;' became '= nint;' in the net8.0 run (C# 12), and
        // SQLite-net-static's netstandard2.0 build (C# 7.3) failed with CS8370. 7.3 = the SDK's default there (MaxSupportedLangVersion).
        var code = name == "alias" ? "using H = System.Int32;\nclass C { }" : $"class C {{ {name} f; }}";
        var global = ImmutableDictionary<string, string>.Empty;
        if (frameworks is not null)
        {
            global = global.Add(MultiTargetSuppressor.Property, frameworks).Add("build_property.MaxSupportedLangVersion", "7.3");
        }

        if (langVersion is not null)
        {
            global = global.Add("build_property.LangVersion", langVersion);
        }

        var diagnostic = Assert.Single(await RunAsync(new FakeTypeNames(), code, global));

        Assert.Equal(suppressed, diagnostic.IsSuppressed);
    }

    [Theory]
    [InlineData("7.3", "IntPtr", true)]
    [InlineData("7.3", "alias", true)]
    [InlineData("7.3", "Int32", false)]
    [InlineData("9.0", "IntPtr", false)]
    [InlineData("9.0", "alias", true)]
    [InlineData("12.0", "alias", false)]
    [InlineData(null, "alias", false)]
    public async Task SuppressesTypeNames_InFilesFormatListsForAnOlderBuild(string? lowest, string name, bool suppressed)
    {
        // sqlite-net: SQLite.Tests (net8.0 only) compiles src/SQLite.cs, which SQLite-net-static also builds for
        // netstandard2.0; its own suppressor sees one framework, so 'bro format' passes the list.
        var code = name == "alias" ? "using H = System.Int32;\nclass C { }" : $"class C {{ {name} f; }}";
        var list = lowest is null ? ImmutableArray<AdditionalText>.Empty : ImmutableArray.Create<AdditionalText>(new Text("/tmp/stylebro-1.oldcsharp", $"{lowest}\t/src/Other.cs\n{lowest}\t\\src\\Test0.cs\n"));
        var global = ImmutableDictionary<string, string>.Empty.Add(MultiTargetSuppressor.Property, "net8.0");

        var diagnostic = Assert.Single(await RunAsync(new FakeTypeNames(), code, global, list, "/src/Test0.cs"));

        Assert.Equal(suppressed, diagnostic.IsSuppressed);
    }

    /// <summary>A stand-in for the SDK rule: reports <paramref name="id"/> on the class; returns all its diagnostics.</summary>
    private static Task<ImmutableArray<Diagnostic>> RunAsync(string id, string? frameworks) =>
        RunAsync(new FakeRule(id), "class C { }", frameworks is null ? ImmutableDictionary<string, string>.Empty : ImmutableDictionary<string, string>.Empty.Add(MultiTargetSuppressor.Property, frameworks));

    private static async Task<ImmutableArray<Diagnostic>> RunAsync(DiagnosticAnalyzer rule, string code, ImmutableDictionary<string, string> global, ImmutableArray<AdditionalText> additional = default, string path = "")
    {
        var compilation = CSharpCompilation.Create("Test", new[] { CSharpSyntaxTree.ParseText(code, path: path) }, Array.Empty<MetadataReference>());
        var options = new AnalyzerOptions(additional.IsDefault ? ImmutableArray<AdditionalText>.Empty : additional, new Provider(global));
        var analyzers = ImmutableArray.Create(rule, new MultiTargetSuppressor());
        return await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();
    }

#pragma warning disable RS1001, RS2008 // Stand-ins for SDK rules, never loaded as analyzers.
    private sealed class FakeRule : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor descriptor;

        public FakeRule(string id)
        {
            descriptor = new DiagnosticDescriptor(id, id, id, "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(c => c.ReportDiagnostic(Diagnostic.Create(descriptor, c.Node.GetLocation())), Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);
        }
    }

    /// <summary>A stand-in for IDE0049: reports the framework names of built-in types ('Int32', 'System.IntPtr').</summary>
    private sealed class FakeTypeNames : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor descriptor = new("IDE0049", "IDE0049", "IDE0049", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                c =>
                {
                    var name = (Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax)c.Node;
                    if (name.Identifier.ValueText is "Int32" or "IntPtr" or "UIntPtr")
                    {
                        var node = name.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.QualifiedNameSyntax qualified && qualified.Right == name ? qualified : (SyntaxNode)name;
                        c.ReportDiagnostic(Diagnostic.Create(descriptor, node.GetLocation()));
                    }
                },
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierName);
        }
    }

#pragma warning restore RS1001, RS2008

    private sealed class Text : AdditionalText
    {
        private readonly string text;

        public Text(string path, string text)
        {
            Path = path;
            this.text = text;
        }

        public override string Path { get; }

        public override Microsoft.CodeAnalysis.Text.SourceText GetText(CancellationToken cancellationToken = default) => Microsoft.CodeAnalysis.Text.SourceText.From(text);
    }

    private sealed class Provider : AnalyzerConfigOptionsProvider
    {
        public Provider(ImmutableDictionary<string, string> global)
        {
            GlobalOptions = new Options(global);
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Options(ImmutableDictionary<string, string>.Empty);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Options(ImmutableDictionary<string, string>.Empty);
    }

    private sealed class Options : AnalyzerConfigOptions
    {
        private readonly ImmutableDictionary<string, string> values;

        public Options(ImmutableDictionary<string, string> values)
        {
            this.values = values;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }
}
