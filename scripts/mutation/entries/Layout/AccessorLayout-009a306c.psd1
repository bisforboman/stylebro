# A multi-line expression body stays an expression body
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '=> !collapse && (accessor.Body is not null || IsSingleLine(accessor, text));'
    Replace = '=> !collapse;'
    Tests = 'AccessorLayoutTests.MultiLineExpressionAccessor_ExpandsTheSingleLineOnes'
}
