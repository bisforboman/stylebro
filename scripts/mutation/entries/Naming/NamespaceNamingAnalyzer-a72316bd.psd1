# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'
    Find = '|| !NamespaceNames.IsOnlyFrom(ns, a => SymbolEqualityComparer.Default.Equals(a, context.Compilation.Assembly))'
    Replace = ''
    Tests = 'NamespaceNamingTests'
}
