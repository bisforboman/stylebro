# Trial 58 (ExpressionTreeToString): BRO1106 keeps string.Empty in expression trees (literal style)
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = '            && !NullChecks.IsInExpressionTree(access, model, cancellationToken);'
    Replace = '            && true;'
    Tests = 'EmptyStringTests.LiteralStyle_ExpressionTrees_KeepTheirForm'
}
