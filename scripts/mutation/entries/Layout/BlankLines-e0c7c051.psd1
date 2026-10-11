# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Layout/BlankLines.cs'
    Find = '(content.StartsWith("#region", StringComparison.Ordinal) || content.StartsWith("#endregion", StringComparison.Ordinal))'
    Replace = 'false'
    Tests = 'FixOrderTests.Regions_ACommentAfterCodeAboveTheRemovedLines'
}
