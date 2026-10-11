# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '? NullChecks.IsInExpressionTree(target, model, cancellationToken)'
    Replace = '? false'
    Tests = 'HasValueTests'
}
