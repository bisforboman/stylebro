# Following StyleCop 1.3.177-beta (issue 338): ?? next to && or || is BRO1407's
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'
    Find = 'IsAnd(token) || IsOr(token) || IsCoalesce(token);'
    Replace = 'IsAnd(token) || IsOr(token);'
    Tests = 'PrecedenceTests.CoalesceMixedWithLogicalOperators_GetsParentheses'
}
