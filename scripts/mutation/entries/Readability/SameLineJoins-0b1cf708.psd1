# A comment below code (BRO1504, BRO1506, BRO1001; eShop)
@{
    File = 'src/StyleBro.Analyzers/Readability/SameLineJoins.cs'
    Find = 'if (isOn(DiagnosticIds.OpenParenthesisOnNameLine) && ParenthesisPlacement'
    Replace = 'if (ParenthesisPlacement'
    Tests = 'ConstructorInitializerLineTests.SameLine_NotReported_WhenTheRulesThatShortenTheLineAreOff'
}
