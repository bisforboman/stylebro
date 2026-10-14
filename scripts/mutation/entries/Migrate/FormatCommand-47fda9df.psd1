# Format speed (2026-10-14): recorded kept findings count only on the same state
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'lines.Length > 0 && lines[0] == StateKey(root, args)'
    Replace = 'lines.Length > 0'
    Tests = 'MigrationTests.Verify_KnowsTheFindingsTheLastCleanRunKept'
}
