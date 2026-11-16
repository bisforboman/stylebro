# IDE0027 on with its option unset: the default is true
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = ': isOn(ExpressionBodiedAccessorsId);'
    Replace = ': false;'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NotReported_WhenExpressionBodiesArePreferred'
}
