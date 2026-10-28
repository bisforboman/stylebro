# Trial 58 (ExpressionTreeToString): BRO1103 leaves expression trees alone
@{
    File = 'src/StyleBro.Analyzers/Readability/ConstantComparisons.cs'
    Find = '            && !NullChecks.IsInExpressionTree(binary, model, cancellationToken);'
    Replace = '            && true;'
    Tests = 'ConstantOnLeftTests.ExpressionTrees_KeepTheirOperands'
}
