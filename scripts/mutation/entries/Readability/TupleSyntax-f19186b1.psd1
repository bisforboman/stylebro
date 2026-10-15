# Trial 22: an inner ValueTuple of a pattern type that stays is converted.
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = '&& !IsPatternType(outerNode))'
    Replace = ')'
    Tests = 'TupleSyntaxTests.PatternTypes'
}
