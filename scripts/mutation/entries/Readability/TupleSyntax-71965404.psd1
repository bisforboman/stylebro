# Issue #304: a ValueTuple declaration pattern (switch arm) stays ValueTuple
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = 'node.Parent is DeclarationPatternSyntax or '
    Replace = 'node.Parent is '
    Tests = 'TupleSyntaxTests.PatternTypes'
}
