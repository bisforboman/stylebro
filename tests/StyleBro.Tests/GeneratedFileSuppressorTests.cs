using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using StyleBro.Analyzers;
using StyleBro.Analyzers.Baseline;

namespace StyleBro.Tests;

public class GeneratedFileSuppressorTests
{
    // Where the compiler puts a generator's file: <intermediate output>/<generator assembly>/<generator type>/<hint name>,
    // under obj/.../generated/ with EmitCompilerGeneratedFiles (probed with a generator and a real build).
    private const string Generated = @"C:\repo\src\RestSharp\obj\Debug\net8.0\SourceGenerator\SourceGenerator.ImmutableGenerator\ReadOnlyRestClientOptions.cs";
    private const string Emitted = @"C:\repo\src\RestSharp\obj\Debug\net8.0\generated\SourceGenerator\SourceGenerator.ImmutableGenerator\ReadOnlyRestClientOptions.cs";
    private const string Folders = @"C:\repo\src\RestSharp\obj\";

    // A generated file the way generators write them: no header, no layout, every kind of finding.
    private const string Code = """
        using System;
        namespace RestSharp {
        /// <summary>
        ///Options</summary>
        public partial class ReadOnlyRestClientOptions {
        public ReadOnlyRestClientOptions(RestClientOptions options) { _options = options; BaseUrl = options.BaseUrl; }
        private readonly RestClientOptions _options;
        /// <summary>
        /// The base URL</summary>
        public Uri BaseUrl { get; private set; }
        public string Name = "";
        // field
        public int count;
        }
        public class RestClientOptions { public Uri BaseUrl { get; set; } }
        }
        """;

    [Theory]
    [InlineData(Generated, Folders, true)]
    [InlineData(Emitted, Folders, true)]
    [InlineData(Generated, null, true)]
    [InlineData(@"C:\repo\Generated\SourceGenerator\G\Hint.cs", @"C:\repo\src\RestSharp\obj\|C:\repo\Generated", true)]
    [InlineData(@"C:\repo\src\RestSharp\RestClient.cs", Folders, false)]
    [InlineData(@"C:\repo\src\RestSharp\RestClient.cs", null, false)]
    [InlineData(@"C:\repo\src\RestSharp\object\Rest.cs", null, false)]
    [InlineData(@"C:\repo\src\Other\obj\Debug\net8.0\G\T\Hint.cs", Folders, false)]
    public void IsGeneratedFile(string path, string? folders, bool expected)
    {
        Assert.Equal(expected, GeneratedFileSuppressor.IsGeneratedFile(path, folders));
    }

    // Every analyzer, every rule on: nothing StyleBro reports in generator output gets through, the same code in a
    // source file is reported.
    [Theory]
    [InlineData(Generated, Folders)]
    [InlineData(Emitted, Folders)]
    [InlineData(Generated, null)]
    public async Task EveryAnalyzer_IsQuietInGeneratedFiles(string path, string? folders)
    {
        var generated = await RunAsync(Code, path, folders);
        var source = await RunAsync(Code, @"C:\repo\src\RestSharp\RestClient.cs", folders);

        Assert.NotEmpty(generated);
        Assert.All(generated, d => Assert.True(d.IsSuppressed, d.ToString()));
        Assert.Contains(source, d => !d.IsSuppressed);
    }

    /// <summary>Every StyleBro analyzer with every BRO rule on, over <paramref name="code"/>: its BRO diagnostics.</summary>
    internal static async Task<ImmutableArray<Diagnostic>> RunAsync(string code, string path, string? folders = null)
    {
        var parse = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose);
        var tree = CSharpSyntaxTree.ParseText(code, parse, path);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(p => Path.GetFileName(p) is "System.Runtime.dll" or "System.Private.CoreLib.dll")
            .Select(p => MetadataReference.CreateFromFile(p));
        var everyRule = BaselineSuppressor.BroIds().ToDictionary(id => id, _ => ReportDiagnostic.Warn);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, specificDiagnosticOptions: everyRule);
        var compilation = CSharpCompilation.Create("Test", new[] { tree }, references, compilationOptions);
        var global = ImmutableDictionary<string, string>.Empty;
        if (folders is not null)
        {
            global = global.Add(GeneratedFileSuppressor.Property, folders);
        }

        var analyzers = typeof(DiagnosticIds).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && t != typeof(BaselineSuppressor))
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t)!)
            .ToImmutableArray();
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new Provider(global));
        var diagnostics = await compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(options, null, true, false, reportSuppressedDiagnostics: true))
            .GetAnalyzerDiagnosticsAsync();
        Assert.DoesNotContain(diagnostics, d => d.Id == "AD0001");
        return diagnostics.Where(d => d.Id.StartsWith("BRO", StringComparison.Ordinal)).ToImmutableArray();
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

        public override IEnumerable<string> Keys => values.Keys;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }
}
