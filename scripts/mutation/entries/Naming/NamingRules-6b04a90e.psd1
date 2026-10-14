# BRO1309: a name an enforced naming rule rejects is not written (Altinn, trial 20)
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = '|| rule.Accepts(newName);'
    Replace = '|| true;'
    Tests = 'PascalCaseNamingTests.NamesAnEnforcedNamingRuleAsksFor'
}
