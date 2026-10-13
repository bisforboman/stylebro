# Trial 18: BRO1123 skips a tuple type with a comment inside
@{
    File = 'src/StyleBro.Analyzers/Readability/TupleSyntax.cs'
    Find = ' || !Trivia.IsBlank(node, node.Span))'
    Replace = ')'
    Tests = 'CommentPreservationTests'
}
