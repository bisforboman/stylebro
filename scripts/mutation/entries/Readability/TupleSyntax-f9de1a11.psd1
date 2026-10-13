# Trial 18: BRO1123 skips a tuple creation with a comment inside
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = ' || !Trivia.IsBlank(expression, expression.Span))'
    Replace = ')'
    Tests = 'CommentPreservationTests'
}
