# Trial 23 (stryker): fields follow the naming rule Roslyn picks
@{
    File = 'src/StyleBro.Analyzers/Naming/FieldNamingAnalyzer.cs'
    Find = 'field, newName, context.CancellationToken)'
    Replace = 'field, null, context.CancellationToken)'
    Tests = 'FieldNamingTests.StaticReadonlyField_FollowsTheRuleRoslynPicks'
}
