# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = '|| comments.Any(c => Line(text, c.Span.Start) != Line(text, c.Span.End))'
    Replace = ''
    Tests = 'EmbeddedCommentTests'
}
