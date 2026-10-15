# Trial 23 (stryker): const implies static and readonly in Roslyn's rule order
@{
    File = 'src/StyleBro.Analyzers/Naming/NamingRules.cs'
    Find = ' || (m is "static" or "shared" or "readonly" && x.Modifiers.Contains("const"))'
    Replace = ''
    Tests = 'FieldNamingTests.StaticReadonlyField_FollowsTheRuleRoslynPicks'
}
