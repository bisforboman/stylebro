# Trial 24 (Refit): stylebro_trailing_comma_with_expressions = false
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = '|| (node.IsKind(SyntaxKind.WithInitializerExpression) && IsFalse(getOptions(), WithExpressionsKey)))'
    Replace = ')'
    Tests = 'TrailingCommaTests.SwitchAndWithExpressions_AreLeftAlone_WhenTurnedOff'
}
