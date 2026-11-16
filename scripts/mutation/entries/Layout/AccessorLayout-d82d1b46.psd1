# csharp_new_line_before_open_brace decides where the new brace goes
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = 'SingleLineBlocks.NewLineBeforeAccessorBrace(options) ? lineBreak + indent : " "'
    Replace = 'lineBreak + indent'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_BraceOnTheAccessorsLine_WhenConfigured'
}
