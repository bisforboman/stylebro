# Trial 58 (ExpressionTreeToString): BRO1106 keeps "" in expression trees
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = '            && !NullChecks.IsInExpressionTreeOrTwin(expression, model, cancellationToken);'
    Replace = '            && true;'
    Tests = 'EmptyStringTests.ExpressionTrees_KeepTheirForm'
}
