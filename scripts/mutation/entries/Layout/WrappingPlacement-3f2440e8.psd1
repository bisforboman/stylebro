# BRO1520-BRO1522 (operator, '=>' and '=' placement when wrapping)
@{
    File = 'src/StyleBro.Analyzers/Layout/WrappingPlacement.cs'
    Find = '|| next.Parent is FromClauseSyntax { Parent: QueryExpressionSyntax }'
    Replace = ''
    Tests = 'WrappingPlacementTests'
}
