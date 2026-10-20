# Trial 37: a word, not a prefix
@{
    File = 'src/StyleBro.CodeFixes/Naming/CamelCaseRenamer.cs'
    Find = '(end == text.Length || !IsWordChar(text[end]))'
    Replace = 'true'
    Tests = 'ContainsWord_NeedsAWordBoundary'
}
