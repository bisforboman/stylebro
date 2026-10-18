# BRO1132 (trial 32, Pinta): '{' goes up to the header's line when csharp_new_line_before_open_brace leaves the block out
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = 'if (braceOnHeaderLine && Line(text, previous.SpanStart) != Line(text, openBrace.SpanStart))'
    Replace = 'if (false)'
    Tests = 'EmbeddedCommentTests.BraceOnTheHeaderLine'
}
