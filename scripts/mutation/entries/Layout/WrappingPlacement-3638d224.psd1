# BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
@{
    File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'
    Find = 'if (token.IsMissing || previous.IsMissing || next.IsMissing'
    Replace = 'if (false'
    Tests = 'WrappingPlacementTests'
}
