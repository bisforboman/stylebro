# BRO1405: the fix removes the nested pairs (trial 27)
@{
    File = 'src/StyleBro.CodeFixes/Maintainability/ParenthesesCodeFixProvider.cs'
    Find = 'nodes.AddRange(Parentheses.GetNested(outer));'
    Replace = ''
    Tests = 'ParenthesesTests.NestedPairs_AreRemovedTogether'
}
