# BRO1133 (null check style)
@{
    File = 'src/StyleBro.Analyzers/Readability/NullChecks.cs'
    Find = 'if (constant?.Expression.IsKind(SyntaxKind.NullLiteralExpression) != true'
    Replace = 'if (constant is null'
    Tests = 'NullCheckTests'
}
