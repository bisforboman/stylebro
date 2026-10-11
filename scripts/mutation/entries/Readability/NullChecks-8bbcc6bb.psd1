# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '&& !IsSingleSpace(isPattern.IsKeyword, unary.OperatorToken)'
    Replace = '&& false'
    Tests = 'NullCheckTests'
}
