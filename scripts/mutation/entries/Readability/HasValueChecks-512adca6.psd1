# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '|| node.Span.End - node.Expression.Span.End != HasValue.Length)'
    Replace = ')'
    Tests = 'HasValueTests'
}
