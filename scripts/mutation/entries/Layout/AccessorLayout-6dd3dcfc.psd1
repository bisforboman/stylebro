# Expression-bodied accessors count (issue 338)
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = 'accessors.Any(a => a.Body is null && a.ExpressionBody is null)'
    Replace = 'accessors.Any(a => a.Body is null)'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToALongerBlock_BecomesABlock'
}
