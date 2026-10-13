# Trial 18: BRO1104 skips a creation with a comment inside
@{
    File = 'src/StyleBro.Analyzers/Readability/DefaultValueConstructors.cs'
    Find = '|| !Trivia.IsBlank(creation, creation.Span))'
    Replace = ')'
    Tests = 'CommentPreservationTests'
}
