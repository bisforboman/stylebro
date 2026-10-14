# AngleSharp: IDE0049 in a using alias, no fix changes it: verify said Clean.
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'if (unfixed > 0)'
    Replace = 'if (false)'
    Tests = 'StyleBro.Tests.MigrationTests.Verify_IsNotClean_WhenAFindingNoFixChangesIsLeft'
}
