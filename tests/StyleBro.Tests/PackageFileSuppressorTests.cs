using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers;

namespace StyleBro.Tests;

public class PackageFileSuppressorTests
{
    // Spectre.Console compiles wcwidth.sources' contentFiles from the package folder.
    private const string PackageFile = @"C:\Users\me\.nuget\packages\wcwidth.sources\4.0.1\contentFiles\cs\net10.0\External\UnicodeCalculator.cs";

    [Theory]
    [InlineData(PackageFile, @"C:\Users\me\.nuget\packages\", true)]
    [InlineData(PackageFile, @"C:\Other\|c:/users/me/.nuget/packages", true)]
    [InlineData(PackageFile, null, true)]
    [InlineData(PackageFile, "", true)]
    [InlineData(@"C:\repo\src\App\Program.cs", @"C:\Users\me\.nuget\packages\", false)]
    [InlineData(@"C:\repo\src\Shared\Guard.cs", null, false)]
    [InlineData(@"C:\Users\me\.nuget\packages-old\x\1.0.0\Program.cs", @"C:\Users\me\.nuget\packages\", false)]
    [InlineData(@"C:\repo\contentFiles\cs\any\Source.cs", @"C:\Users\me\.nuget\packages\", false)]
    [InlineData(@"C:\Users\me\.nuget\packages\x\1.0.0\build\Source.cs", @"C:\Users\me\.nuget\packages\", true)]
    public void IsPackageFile(string path, string? folders, bool expected)
    {
        Assert.Equal(expected, PackageFileSuppressor.IsPackageFile(path, folders));
    }

    [Theory]
    [InlineData(PackageFile, "BRO1306", true)]
    [InlineData(@"C:\repo\src\App\Program.cs", "BRO1306", false)]
    [InlineData(PackageFile, "IDE0055", false)]
    public async Task SuppressesStyleBroFindingsInPackageFiles(string path, string id, bool suppressed)
    {
        var compilation = CSharpCompilation.Create("Test", new[] { CSharpSyntaxTree.ParseText("class C { }", path: path) }, Array.Empty<MetadataReference>());
        var global = ImmutableDictionary<string, string>.Empty.Add(PackageFileSuppressor.Property, @"C:\Users\me\.nuget\packages\");
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new Provider(global));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FakeRule(id), new PackageFileSuppressor());
        var diagnostics = await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();

        Assert.Equal(suppressed, Assert.Single(diagnostics).IsSuppressed);
    }

#pragma warning disable RS1001 // A stand-in for a rule, never loaded as an analyzer.
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
            context.RegisterSyntaxNodeAction(c => c.ReportDiagnostic(Diagnostic.Create(descriptor, c.Node.GetLocation())), SyntaxKind.ClassDeclaration);
        }
    }

#pragma warning restore RS1001

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
