# A comment after the arrow
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '|| !SingleLineBlocks.Gap(arrow.ArrowToken, expression.GetFirstToken(), " ", text, probe)'
    Replace = ''
    Tests = 'AccessorLayoutTests.ExpressionAccessor_WithACommentInside_NotReported'
}
