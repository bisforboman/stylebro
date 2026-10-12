# BRO1123: nothing in crefs (no tuple syntax there).
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = ' || node.IsPartOfStructuredTrivia() ||'
    Replace = ' ||'
    Tests = 'CrefTests'
}
