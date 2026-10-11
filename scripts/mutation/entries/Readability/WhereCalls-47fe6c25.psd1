# BRO1150: List/array Exists and Find where Sonar S6605/S6602 are on
@{
    File = 'src/StyleBro.Analyzers/Readability/WhereCalls.cs'
    Find = '|| !SonarRules.IsOn(model, options, name == "Any" ? "S6605" : "S6602", cancellationToken))'
    Replace = ')'
    Tests = 'WhereBeforeTerminalTests.SonarCollectionRules'
}
