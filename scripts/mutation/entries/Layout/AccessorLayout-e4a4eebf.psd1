# No blank line beside an expression body that stays
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '=> !collapse && (accessor.Body is not null || IsSingleLine(accessor, text));'
    Replace = '=> accessor.Body is not null || IsSingleLine(accessor, text);'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToAOneStatementBlock_BlockIsCollapsed'
}
