# BRO1504: comments inside a pattern
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = 'is ExpressionSyntax or PatternSyntax;'
    Replace = 'is ExpressionSyntax;'
    Tests = 'BlankLineAfterTests.CommentInsideAnExpression_NoDiagnostic'
}
