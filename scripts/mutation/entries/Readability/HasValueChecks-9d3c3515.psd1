# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '|| target.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax'
    Replace = '|| target.Parent is CastExpressionSyntax { Parent.Parent: InvocationExpressionSyntax'
    Tests = 'HasValueTests'
}
