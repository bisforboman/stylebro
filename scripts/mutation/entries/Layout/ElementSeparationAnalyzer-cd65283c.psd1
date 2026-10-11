# BRO1527 (auto-accessors on one line)
@{
    File = 'src/StyleBro.Analyzers/Layout/ElementSeparationAnalyzer.cs'
    Find = 'var autoAccessorLines = Severities.IsOn(compilationOptions, c.Tree, DiagnosticIds.AutoAccessorsOnOneLine, c.CancellationToken)'
    Replace = 'var autoAccessorLines = true'
    Tests = 'ElementSeparationTests'
}
