# First-run fixes (2026-10-06): BRO1601 conditional directives and doc generation, BRO1603 spaces before a closing
# tag, BRO1604/BRO1606 text that can't follow the standard words, BRO1134 a comment ending the header's line
@{
    File = 'src/StyleBro.Analyzers/Documentation/DocumentationComments.cs'
    Find = 'var end = member.AttributeLists.Count > 0 ? member.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart : member.SpanStart;'
    Replace = 'var end = member.SpanStart;'
    Tests = 'DocumentationTests'
}
