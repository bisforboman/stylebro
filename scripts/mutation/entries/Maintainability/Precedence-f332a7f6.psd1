# Following StyleCop 1.3.177-beta (issue 338): a chain of ?? needs no parentheses
@{
    File = 'src/StyleBro.Analyzers/Maintainability/Precedence.cs'
    Find = ' || (IsCoalesce(a) && IsCoalesce(b));'
    Replace = ';'
    Tests = 'PrecedenceTests.Coalesce_AloneOrWithOtherOperators_NotReported'
}
