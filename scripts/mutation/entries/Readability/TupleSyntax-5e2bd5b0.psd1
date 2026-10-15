# Trial 22: a pattern's ValueTuple type stays (tuple syntax there is a positional pattern).
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = '            || IsPatternType(node)'
    Replace = ''
    Tests = 'TupleSyntaxTests.PatternTypes'
}
