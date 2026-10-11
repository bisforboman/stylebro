# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = '|| IsInExpressionTree(binary, model, cancellationToken)'
    Replace = ''
    Tests = 'NullCheckTests'
}
