# A StyleCop 1.2 repository gets 1.2's rule list, without 1.3's new rules.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = 'if (version.StartsWith("1.2.", StringComparison.Ordinal))'
    Replace = 'if (version.StartsWith("1.2.x", StringComparison.Ordinal))'
    Tests = 'StyleBro.Tests.MigrationTests.StyleCop13_HasItsNewRules_AndStyleCop12DoesNot'
}
