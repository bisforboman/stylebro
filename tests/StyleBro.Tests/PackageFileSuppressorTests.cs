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
    [InlineData(@"C:\repo\shared-infrastructure\src\Guard.cs", @"C:\repo\shared-infrastructure\|C:\repo\tests\harfbuzz\", true)]
    [InlineData(@"C:\repo\shared-infrastructure-old\Guard.cs", @"C:\repo\shared-infrastructure\", false)]
    [InlineData(@"C:\repo\src\Guard.cs", null, false)]
    [InlineData(@"C:\repo\obj\x\contentFiles\Guard.cs", "", false)]

    // Trial 19: a Compile item '..\..\ext\lib\*.cs' keeps the '..' in the compiler's path.
    [InlineData(@"C:\repo\src\App\..\..\ext\lib\Helper.cs", @"C:\repo\ext\lib", true)]
    [InlineData(@"C:\repo\src\App\.\..\..\ext\lib\Helper.cs", @"C:\repo\src\..\ext\lib\", true)]
    [InlineData(@"C:\repo\ext\lib\..\..\src\App\Messy.cs", @"C:\repo\ext\lib", false)]
    [InlineData("/home/me/repo/src/App/../../ext/lib/Helper.cs", "/home/me/repo/ext/lib", true)]
    public void IsSubmoduleFile(string path, string? folders, bool expected)
    {
        // Fonts compiles shared-infrastructure's code from a git submodule: 88 warnings 'format' can never fix.
        Assert.Equal(expected, PackageFileSuppressor.IsSubmoduleFile(path, folders));
    }

    [Fact]
    public async Task SuppressesStyleBroFindingsInSubmoduleFiles()
    {
        var compilation = CSharpCompilation.Create("Test", new[] { CSharpSyntaxTree.ParseText("class C { }", path: @"C:\repo\shared\Guard.cs") }, Array.Empty<MetadataReference>());
        var global = ImmutableDictionary<string, string>.Empty.Add(PackageFileSuppressor.SubmoduleProperty, @"C:\repo\shared\");
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new Provider(global));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FakeRule("BRO1306"), new PackageFileSuppressor());
        var diagnostics = await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();

        Assert.True(Assert.Single(diagnostics).IsSuppressed);
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

    // Trial 27: '..\..\outside\Shared.cs' linked into a project. Only full paths, and only with the repository's folder.
    [Theory]
    [InlineData(@"C:\work\outside\Shared.cs", @"C:\work\app", true)]
    [InlineData(@"C:\work\app\src\Lib\..\..\..\outside\Shared.cs", @"C:\work\app\", true)]
    [InlineData(@"C:\work\app-old\Shared.cs", @"C:\work\app", true)]
    [InlineData(@"C:\work\app\src\Lib\Program.cs", @"C:\work\app", false)]
    [InlineData("c:/work/APP/src/Lib/Program.cs", @"C:\work\app", false)]
    [InlineData(@"C:\work\outside\Shared.cs", null, false)]
    [InlineData(@"C:\work\outside\Shared.cs", "", false)]
    [InlineData(@"C:\work\outside\Shared.cs", "none", false)]
    [InlineData("Shared.cs", @"C:\work\app", false)]
    [InlineData("/home/me/outside/Shared.cs", "/home/me/app", true)]
    public void IsOutsideRepository(string path, string? root, bool expected)
    {
        Assert.Equal(expected, OutsideRepositorySuppressor.IsOutside(path, root));
    }

    // StyleBro's findings and the built-in rules' its preset and init turn on; another analyzer's stay.
    [Theory]
    [InlineData(@"C:\work\outside\Shared.cs", "BRO1107", true)]
    [InlineData(@"C:\work\outside\Shared.cs", "IDE0055", true)]
    [InlineData(@"C:\work\outside\Shared.cs", "CA1000", false)]
    [InlineData(@"C:\work\app\src\Program.cs", "BRO1107", false)]
    [InlineData(@"C:\work\app\src\Program.cs", "IDE0055", false)]
    public async Task SuppressesFindingsOutsideTheRepository(string path, string id, bool suppressed)
    {
        var compilation = CSharpCompilation.Create("Test", new[] { CSharpSyntaxTree.ParseText("class C { }", path: path) }, Array.Empty<MetadataReference>());
        var global = ImmutableDictionary<string, string>.Empty.Add(OutsideRepositorySuppressor.Property, @"C:\work\app");
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new Provider(global));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FakeRule(id), new OutsideRepositorySuppressor());
        var diagnostics = await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();

        Assert.Equal(suppressed, Assert.Single(diagnostics).IsSuppressed);
    }

#pragma warning disable RS1001 // A stand-in for a rule, never loaded as an analyzer.
    internal sealed class FakeRule : DiagnosticAnalyzer
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
