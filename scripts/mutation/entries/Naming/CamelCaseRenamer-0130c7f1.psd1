# Trial 37: a word, not a suffix
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '(at == 0 || !IsWordChar(text[at - 1]))'
    Replace = 'true'
    Tests = 'ContainsWord_NeedsAWordBoundary'
}
