# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = '|| Line(text, openBrace.GetNextToken().SpanStart) == Line(text, openBrace.SpanStart)'
    Replace = ''
    Tests = 'EmbeddedCommentTests'
}
