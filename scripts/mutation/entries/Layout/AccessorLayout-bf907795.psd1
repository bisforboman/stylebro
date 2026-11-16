# when_on_single_line prefers expression bodies too
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = 'is "true" or "when_on_single_line"'
    Replace = 'is "true"'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NotReported_WhenExpressionBodiesArePreferred'
}
