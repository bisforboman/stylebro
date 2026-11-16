# get => throw becomes a throw statement
@{
    File = 'src/StyleBro.Analyzers/Layout/AccessorLayout.cs'
    Find = '&& !expression.IsKind(SyntaxKind.ThrowExpression) ? "return "'
    Replace = '? "return "'
    Tests = 'AccessorLayoutTests.ExpressionAccessor_NextToALongerBlock_BecomesABlock'
}
