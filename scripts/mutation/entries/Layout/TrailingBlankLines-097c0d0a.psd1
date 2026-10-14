# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Layout/TrailingBlankLines.cs'
    Find = '|| BlankLines.IsCommentBelowCode(comment, text)'
    Replace = '|| false'
    Tests = 'TrailingBlankLinesTests.ACommentBelowCode_KeepsTheBlankLineBelowIt'
}
