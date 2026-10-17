# BRO1405: a nested pair is judged at the outer pair's place (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'var parent = place.Parent;'
    Replace = 'var parent = node.Parent;'
    Tests = 'ParenthesesTests.NestedPairs_AreRemovedTogether'
}
