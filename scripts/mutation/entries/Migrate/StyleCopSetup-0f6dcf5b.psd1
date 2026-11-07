# A StyleCop 1.2 repository gets 1.2's rule list, without 1.3's new rules.
@{
    File = 'src/StyleBro.Migrate/StyleCopSetup.cs'
    Find = ': version.StartsWith("1.2.", StringComparison.Ordinal) ? LoadIds("StyleCopRules-1.2.0-beta.556.csv")'
    Replace = ': version.StartsWith("1.2.x", StringComparison.Ordinal) ? LoadIds("StyleCopRules-1.2.0-beta.556.csv")'
    Tests = 'StyleBro.Tests.MigrationTests.StyleCop13_HasItsNewRules_AndStyleCop12DoesNot'
}
