# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = '&& fullNameInText.IsMatch(t.ValueText))'
    Replace = '&& false)'
    Tests = 'NamespaceNamingTests'
}
