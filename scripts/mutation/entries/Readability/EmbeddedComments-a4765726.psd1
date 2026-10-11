# BRO1132 (embedded comments)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmbeddedComments.cs'
    Find = '|| gap.Any(t => t.IsDirective)'
    Replace = ''
    Tests = 'EmbeddedCommentTests'
}
