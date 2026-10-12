# BRO1122: the parentheses go only when BRO1405 is on
@{
    File = 'src/StyleBro.Analyzers/Readability/LiteralSuffixes.cs'
    Find = '|| !Severities.IsOn(model.Compilation.Options, cast.SyntaxTree, DiagnosticIds.UnnecessaryParentheses, cancellationToken))'
    Replace = ')'
    Tests = 'LiteralSuffixTests'
}
