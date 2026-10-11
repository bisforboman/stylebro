# BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
@{
    File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'
    Find = 'if (!before.Concat(after).All('
    Replace = 'if (false && !before.Concat(after).All('
    Tests = 'WrappingPlacementTests'
}
