# Format speed (2026-10-14): settings files are part of the state
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'settings.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase))'
    Replace = 'f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)'
    Tests = 'MigrationTests.Verify_KnowsTheFindingsTheLastCleanRunKept'
}
