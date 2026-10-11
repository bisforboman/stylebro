# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = '|| Line(text, GetHeaderStart(owner).SpanStart) != Line(text, previous.SpanStart)'
    Replace = ''
    Tests = 'EmbeddedCommentTests'
}
