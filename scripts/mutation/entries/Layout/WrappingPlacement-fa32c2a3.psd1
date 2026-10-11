# BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
@{
    File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'
    Find = 'next.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.OpenBracketToken'
    Replace = 'next.Kind() is SyntaxKind.OpenBracketToken'
    Tests = 'WrappingPlacementTests'
}
