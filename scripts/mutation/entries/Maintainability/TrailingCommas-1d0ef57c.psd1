# Trials 22 and 24: the keys value
@{
    File = 'src/StyleBro.Analyzers/Maintainability/TrailingCommas.cs'
    Find = '.Equals("false", StringComparison.OrdinalIgnoreCase);'
    Replace = '.Equals("true", StringComparison.OrdinalIgnoreCase);'
    Tests = 'TrailingCommaTests.SwitchAndWithExpressions_AreLeftAlone_WhenTurnedOff'
}
