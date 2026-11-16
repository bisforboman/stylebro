# IDE0027 would undo the block
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '&& PrefersExpressionBodies(options, isOn))'
    Replace = '&& false)'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NotReported_WhenExpressionBodiesArePreferred'
}
