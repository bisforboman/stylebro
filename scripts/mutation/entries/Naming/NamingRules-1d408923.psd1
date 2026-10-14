# BRO1309: a naming rule nothing enforces does not count
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = '|| !IsEnforced(rule.Severity,'
    Replace = '|| false && !IsEnforced(rule.Severity,'
    Tests = 'PascalCaseNamingTests.NamingRulesThatDoNotCoverTheRename'
}
