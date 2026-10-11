# Hardening: BRO1112's sort (BRO1001) adds BRO1505's blank lines first, like BRO1001's own fix
@{
    File = 'src/StyleBro.Analyzers/Readability/ParenthesisPlacement.cs'
    Find = 'SyntaxKind.CloseParenToken when closing.Parent is ArgumentListSyntax or TupleExpressionSyntax'
    Replace = 'SyntaxKind.CloseParenToken when false'
    Tests = 'ParenthesisPlacementTests'
}
