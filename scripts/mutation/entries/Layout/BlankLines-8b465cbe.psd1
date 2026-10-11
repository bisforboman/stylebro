# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '&& !IsCommentBelowCode(comment, text);'
    Replace = ';'
    Tests = 'BlankLineAfterTests.ACommentBelowCode_FollowedByABlankLine_IsNotReported'
}
