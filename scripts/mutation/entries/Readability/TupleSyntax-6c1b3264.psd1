# Issue #304: a ValueTuple type pattern (is not, or, case) stays ValueTuple
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = ' or TypePatternSyntax or '
    Replace = ' or '
    Tests = 'TupleSyntaxTests.PatternTypes'
}
