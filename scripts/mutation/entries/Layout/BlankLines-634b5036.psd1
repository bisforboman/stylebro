# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = 'while (last + 1 < text.Lines.Count && IsComment(last + 1))'
    Replace = 'while (false)'
    Tests = 'BlankLineAfterTests.ACommentBelowCode_FollowedByABlankLine_IsNotReported'
}
