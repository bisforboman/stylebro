# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'
    Find = '|| IsInRootNamespace(fullName, rootNamespace)'
    Replace = ''
    Tests = 'NamespaceNamingTests'
}
