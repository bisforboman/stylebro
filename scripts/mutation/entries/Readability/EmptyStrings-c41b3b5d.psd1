# Trial 58 (ExpressionTreeToString): BRO1106 keeps "" in expression trees
@{
    File = 'src/StyleBro.Analyzers/Readability/EmptyStrings.cs'
    Find = '            && !NullChecks.IsInExpressionTreeOrTwin(literal, model, cancellationToken);'
    Replace = '            && true;'
    Tests = 'EmptyStringTests.ExpressionTrees_KeepTheirForm'
}
