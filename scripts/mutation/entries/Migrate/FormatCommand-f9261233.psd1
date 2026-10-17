# Format speed, phase 0 (2026-10-17)
@{
    File = 'src/StyleBro.Migrate/FormatCommand.cs'
    Find = 'var reported = checkedClean && changed.Count == 0 ?'
    Replace = 'var reported = checkedClean ?'
    Tests = 'MigrationTests.Verify_RunsTheChecks_AfterARunThatChangedLineEndingsOnly'
}
