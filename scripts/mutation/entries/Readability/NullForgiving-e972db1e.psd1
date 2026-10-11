# BRO1147 (redundant null-forgiving '!')
@{
    File = 'src/StyleBro.Analyzers/Readability/NullForgiving.cs'
    Find = '|| operand is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression }'
    Replace = ''
    Tests = 'NullForgivingTests'
}
