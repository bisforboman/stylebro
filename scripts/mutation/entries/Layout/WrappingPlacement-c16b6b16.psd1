# BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
@{
    File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'
    Find = ' when !arrow.ArrowToken.GetPreviousToken().Parent!.AncestorsAndSelf().Any(a => a is TypeParameterConstraintClauseSyntax)'
    Replace = ''
    Tests = 'WrappingPlacementTests'
}
