# BRO1504: comments inside an expression (trial 23, Stryker)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '            && !IsInsideExpression(comment)'
    Replace = ''
    Tests = 'BlankLineAfterTests.CommentInsideAnExpression_NoDiagnostic'
}
