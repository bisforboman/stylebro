# Only a getter returns its expression
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = 'accessor.IsKind(SyntaxKind.GetAccessorDeclaration) && !expression'
    Replace = '!expression'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToALongerBlock_BecomesABlock'
}
