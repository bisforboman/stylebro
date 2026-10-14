# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'
    Find = '|| declared.Locations.Any(l => l.SourceTree is { } tree && NamespaceNames.IsGenerated(tree))'
    Replace = ''
    Tests = 'NamespaceNamingTests'
}
