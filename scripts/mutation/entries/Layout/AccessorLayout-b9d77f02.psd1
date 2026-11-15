# A comment before the semicolon
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '|| !SingleLineBlocks.Gap(expression.GetLastToken(), accessor.SemicolonToken, string.Empty, text, probe))'
    Replace = ')'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_WithACommentInside_NotReported'
}
