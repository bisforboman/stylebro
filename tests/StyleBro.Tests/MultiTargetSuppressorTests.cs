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

    /// <summary>A stand-in for the SDK rule: reports <paramref name="id"/> on the class; returns all its diagnostics.</summary>
    private static async Task<ImmutableArray<Diagnostic>> RunAsync(string id, string? frameworks)
    {
        var compilation = CSharpCompilation.Create("Test", new[] { CSharpSyntaxTree.ParseText("class C { }") }, Array.Empty<MetadataReference>());
        var global = frameworks is null ? ImmutableDictionary<string, string>.Empty : ImmutableDictionary<string, string>.Empty.Add(MultiTargetSuppressor.Property, frameworks);
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new Provider(global));
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new FakeRule(id), new MultiTargetSuppressor());
        return await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();
    }

#pragma warning disable RS1001 // A stand-in for the SDK rule, never loaded as an analyzer.
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
