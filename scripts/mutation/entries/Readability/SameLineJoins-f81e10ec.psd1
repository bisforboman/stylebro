# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = 'return isOn(DiagnosticIds.CloseParenthesisOnLastItemLine)'
    Replace = 'return true'
    Tests = 'ConstructorInitializerLineTests.SameLine_NotReported_WhenTheRulesThatShortenTheLineAreOff'
}
