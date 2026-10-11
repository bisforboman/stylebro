# BRO1312 (namespace names): the analyzer's skips, then the rename's
@{
    File = 'src/StyleBro.CodeFixes/Naming/NamespaceRenamer.cs'
    Find = 'is { } text && fullNameInText.Match(text.ToString()) is { Success: true } match)'
    Replace = 'is { } text && fullNameInText.Match(text.ToString()) is { Success: false } match)'
    Tests = 'NamespaceNamingTests'
}
