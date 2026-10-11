# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.Analyzers/Naming/NamespaceNamingAnalyzer.cs'
    Find = 'if (parent.GetMembers(newName).Any()'
    Replace = 'if (false'
    Tests = 'NamespaceNamingTests'
}
