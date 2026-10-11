# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'if (document is null || generated)'
    Replace = 'if (document is null)'
    Tests = 'NamespaceNamingTests'
}
