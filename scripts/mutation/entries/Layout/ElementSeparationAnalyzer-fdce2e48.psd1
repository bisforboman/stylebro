# Trial 62: the analyzer asks BRO1506's severity.
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparationAnalyzer.cs'
    Find = 'var commentBlankLines = Severities.IsOn(compilationOptions, c.Tree, DiagnosticIds.BlankLineAfterComment, c.CancellationToken) ? options : null;'
    Replace = 'AnalyzerConfigOptions? commentBlankLines = null;'
    Tests = 'StyleBro.Tests.ElementSeparationTests.BlankLineBelowAComment_BRO1506Removes_DoesNotCount'
}
