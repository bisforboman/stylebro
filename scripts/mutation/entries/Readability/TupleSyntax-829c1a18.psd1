# Trial 18: BRO1123 skips a tuple creation with a comment inside
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = 'return !Trivia.IsBlank(expression, expression.Span) || IsInExpressionTree'
    Replace = 'return IsInExpressionTree'
    Tests = 'CommentPreservationTests'
}
