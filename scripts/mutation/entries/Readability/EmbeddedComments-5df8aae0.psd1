# BRO1134 (comments in declaration headers; shares EmbeddedComments.cs with BRO1132)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = '&& line.Start < next.SpanStart && text.ToString(line.Span).Trim().Length == 0'
    Replace = '&& false'
    Tests = 'DeclarationCommentTests'
}
