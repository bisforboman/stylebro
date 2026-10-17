# BRO1405: nested pairs go in one run (trial 27)
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Parentheses.cs'
    Find = 'if (IsReportedByStyleCop(inner, node))'
    Replace = 'if (false)'
    Tests = 'ParenthesesTests.NestedPairs_AreRemovedTogether'
}
