# BRO1148 (HasValue -> null check)
@{
    File = 'src/StyleBro.Analyzers/Readability/HasValueChecks.cs'
    Find = '&& Severities.IsOn(model.Compilation.Options, node.SyntaxTree, DiagnosticIds.UnnecessaryParentheses, cancellationToken)'
    Replace = '&& false'
    Tests = 'HasValueTests'
}
