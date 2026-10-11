# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& !comment.Token.IsKind(SyntaxKind.CloseBraceToken);'
    Replace = ';'
    Tests = 'TrailingBlankLinesTests.ACommentBelowCode_KeepsTheBlankLineBelowIt'
}
