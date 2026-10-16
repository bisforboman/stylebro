# BRO1504: a comment leading an embedded statement needs no blank line (trial 25)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& !LeadsEmbeddedStatement(comment)'
    Replace = ''
    Tests = 'BlankLineAfterTests.CommentLeadingAnEmbeddedStatement'
}
