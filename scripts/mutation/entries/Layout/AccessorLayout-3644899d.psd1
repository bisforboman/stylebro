# A comment before the arrow
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = 'if (!SingleLineBlocks.Gap(accessor.Keyword, arrow.ArrowToken, " ", text, probe)'
    Replace = 'if (false'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_WithACommentInside_NotReported'
}
