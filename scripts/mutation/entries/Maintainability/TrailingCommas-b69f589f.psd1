# Trials 22 and 24: stylebro_trailing_comma_switch_expressions = false
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = '|| (node.IsKind(SyntaxKind.SwitchExpression) && IsFalse(getOptions(), SwitchExpressionsKey))'
    Replace = ''
    Tests = 'TrailingCommaTests.SwitchAndWithExpressions_AreLeftAlone_WhenTurnedOff'
}
