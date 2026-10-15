# Trial 23 (stryker): rules in the order Roslyn lists their titles
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = 'k.Trim().StartsWith("dotnet_naming_rule.", System.StringComparison.Ordinal) && k.Split(''.'').Length == 3'
    Replace = 'k.Trim().StartsWith("dotnet_naming_rule.", System.StringComparison.Ordinal) && k.EndsWith(".symbols", System.StringComparison.Ordinal)'
    Tests = 'FieldNamingTests.StaticReadonlyField_FollowsTheRuleRoslynPicks'
}
