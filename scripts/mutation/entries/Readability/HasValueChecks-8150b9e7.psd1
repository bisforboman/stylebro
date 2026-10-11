# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = ': !IsBuiltIn(model, target, node.Expression, negated, cancellationToken))'
    Replace = ': false)'
    Tests = 'HasValueTests'
}
