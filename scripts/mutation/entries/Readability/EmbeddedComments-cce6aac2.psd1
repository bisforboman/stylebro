# First-run fixes (2026-10-06): BRO1601 conditional directives and doc generation, BRO1603 spaces before a closing
# tag, BRO1604/BRO1606 text that can't follow the standard words, BRO1134 a comment ending the header's line
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = 'comments = comments.RemoveAll(c => previous.TrailingTrivia.Contains(c));'
    Replace = ''
    Tests = 'DeclarationCommentTests'
}
